using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DatingManagementSystem.Data;
using DatingManagementSystem.Models;
using System.Collections.Concurrent;
using System.Collections;
using System.Security.Claims;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace DatingManagementSystem.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UsersController(ILogger<UsersController> logger, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // IAction for Login
        public IActionResult Login()
        {
            return View();
        }

        // IAction for create
        public IActionResult Create()
        {
            return View();
        }
        public IActionResult CompatibilityScores()
        {
            return View();
        }


        // IAction for Index
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        // IAction for details

        public async Task<IActionResult> GetUserDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserID == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }


        // IAction for Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserID == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // IAction for delete function
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserID == id);
        }

        // IAction for Compatibility score to test on Postman

        [HttpGet]
        public IActionResult GetCompatibilityScores()
        {
            var scores = _context.CompatibilityScores.ToList();
            return Json(scores);
        }

        // IAction to get profile pic
        public IActionResult GetProfilePicture(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null || user.ProfilePicture == null || user.ProfilePicture.Length == 0)
            {
                return NotFound(); // Or return a default image
            }

            return File(user.ProfilePicture, "image/*"); // Assuming the images are JPGs
        }







        // POST: Users/Create


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Age,Gender,Email,Password,Interests,Bio,CreatedAt")] User user, IFormFile? ProfilePictureFile)
        {
            try
            {
                Console.WriteLine("Processing Create request...");

                if (ProfilePictureFile != null && ProfilePictureFile.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await ProfilePictureFile.CopyToAsync(memoryStream);
                    user.ProfilePicture = memoryStream.ToArray();
                }
                else
                {
                    user.ProfilePicture = new byte[0]; // Default empty image
                }

                if (!ModelState.IsValid)
                {
                    Console.WriteLine("ModelState is invalid:");
                    foreach (var error in ModelState)
                    {
                        Console.WriteLine($"Key: {error.Key}, Error: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                    }
                    return View(user);
                }

                Console.WriteLine("Adding user to DB...");
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _context.Entry(user).Reload();
                _logger.LogInformation($"User saved successfully with ID: {user.UserID}");


                // Compute compatibility score
                ComputeCompatibility(user);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while saving: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while saving. Check the logs.");
                return View(user);
            }
        }




        //Login Functionality
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);
                if (user != null)
                {
                    // Authentication logic
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FirstName + " " + user.LastName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("UserID", user.UserID.ToString()) // Store User ID
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties { IsPersistent = true };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                                  new ClaimsPrincipal(claimsIdentity),
                                                  authProperties);

                    // Store user details in session
                    _httpContextAccessor.HttpContext?.Session.SetString("UserID", user.UserID.ToString());
                    HttpContext.Session.SetString("UserName", user.FirstName + " " + user.LastName);
                    HttpContext.Session.SetString("UserEmail", user.Email);

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                }
            }
            return View(model);
        }

        //Logout Functionality
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Users");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        public static class CompatibilityStore
        {
            public static ConcurrentDictionary<int, Dictionary<int, double>> CompatibilityScores = new();
        }

        //Calculating Compatibility Score
        private void ComputeCompatibility(User newUser)
        {
            var users = _context.Users.AsNoTracking().ToList();
            _logger.LogInformation($"Computing compatibility for User {newUser.UserID} with {users.Count} existing users.");

            if (!users.Any()) return;

            var computedScores = new ConcurrentBag<CompatibilityScore>();

            Parallel.ForEach(users, existingUser =>
            {
                if (existingUser.UserID == newUser.UserID) return;

                double similarity = CalculateJaccardSimilarity(newUser.Interests, existingUser.Interests);
                double agePenalty = 1 / (1 + Math.Abs(newUser.Age - existingUser.Age) / 10.0);
                double compatibilityScore = similarity * agePenalty;

                _logger.LogInformation($"User {newUser.UserID} ↔ User {existingUser.UserID}: Score = {compatibilityScore}");

                computedScores.Add(new CompatibilityScore
                {
                    User1Id = newUser.UserID,
                    User2Id = existingUser.UserID,
                    Score = compatibilityScore
                });

                computedScores.Add(new CompatibilityScore
                {
                    User1Id = existingUser.UserID,
                    User2Id = newUser.UserID,
                    Score = compatibilityScore
                });
            });

            _context.CompatibilityScores.AddRange(computedScores);
            _context.SaveChanges();
        }


        //Calculating Jaccard Similarity

        private double CalculateJaccardSimilarity(string interests1, string interests2)
        {
            if (string.IsNullOrWhiteSpace(interests1) || string.IsNullOrWhiteSpace(interests2))
            {
                return 0; // No similarity if either is empty
            }

            // Normalize by trimming and converting to lowercase
            var set1 = new HashSet<string>(interests1.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                       .Select(i => i.Trim().ToLower()));
            var set2 = new HashSet<string>(interests2.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                       .Select(i => i.Trim().ToLower()));

            int intersection = set1.Intersect(set2).Count(); // Common interests
            int union = set1.Union(set2).Count(); // All unique interests

            // Return Jaccard similarity: intersection / union
            return union == 0 ? 0 : (double)intersection / union;
        }




        // Endpoint to test if the hashtable is working correctly, Sort the Compatibility Scores

        [HttpGet]

        public async Task<IActionResult> GetSortedCompatibilityScoresForLoggedInUser()
        {
            int loggedInUserId = int.Parse(HttpContext.Session.GetString("UserID"));

            var compatibilityScores = await _context.CompatibilityScores
                .Where(cs => cs.User1Id == loggedInUserId || cs.User2Id == loggedInUserId)
                .ToListAsync();

            Hashtable compatibilityScoresHashtable = new Hashtable();
            foreach (var score in compatibilityScores)
            {
                int pairedUserId = score.User1Id == loggedInUserId ? score.User2Id : score.User1Id;
                compatibilityScoresHashtable[pairedUserId] = score.Score;
            }

            PriorityQueue<int, double> maxHeap = new PriorityQueue<int, double>(Comparer<double>.Create((a, b) => b.CompareTo(a)));

            foreach (DictionaryEntry entry in compatibilityScoresHashtable)
            {
                int userId = (int)entry.Key;
                double score = entry.Value as double? ?? 0.0;
                if (score > 0)
                {
                    maxHeap.Enqueue(userId, score);
                }
            }

            List<(int UserId, double Score)> sortedUsers = new List<(int UserId, double Score)>();
            while (maxHeap.Count > 0)
            {
                maxHeap.TryDequeue(out int userId, out double score);
                sortedUsers.Add((userId, score));
            }

            var userIds = sortedUsers.Select(u => u.UserId).ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserID))
                .Select(u => new
                {
                    u.UserID,
                    u.FirstName,
                    u.LastName,
                    u.Age,
                    u.Gender,
                    u.Email,
                    u.Interests,
                    u.Bio,
                    u.CreatedAt,
                    ProfilePictureUrl = Url.Action("GetProfilePicture", "Users", new { id = u.UserID })
                })
                .ToListAsync();

            var sortedResults = sortedUsers.Select(sortedUser =>
            {
                var user = users.FirstOrDefault(u => u.UserID == sortedUser.UserId);
                return new
                {
                    UserId = user?.UserID,
                    CompatibilityScore = sortedUser.Score,
                    User = user
                };
            }).ToList();

            return Json(sortedResults);
        }

    }

}