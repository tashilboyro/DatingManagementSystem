using CsvHelper;
using CsvHelper.Configuration;
using DatingManagementSystem.Data;
using DatingManagementSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;

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

        // IAction to load Users.csv
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadUsers(IFormFile csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                ModelState.AddModelError("", "CSV file is required.");
                return View("Index");
            }

            var usersToAdd = new List<User>();

            using var reader = new StreamReader(csvFile.OpenReadStream());
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                Quote = '"',
            });

            try
            {
                csv.Context.RegisterClassMap<UserMap>();
                var records = csv.GetRecords<User>();

                // Keep track of names seen in the current CSV
                var csvNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Also fetch existing names from the DB to avoid inserting duplicates
                var existingNamesInDb = await _context.Users
                    .Select(u => u.FirstName.ToLower() + " " + u.LastName.ToLower())
                    .ToListAsync();
                var dbNameSet = new HashSet<string>(existingNamesInDb);

                foreach (var record in records)
                {
                    string fullName = (record.FirstName + " " + record.LastName).ToLower();

                    // Skip if it's a duplicate in CSV or already in the database
                    if (csvNameSet.Contains(fullName) || dbNameSet.Contains(fullName))
                    {
                        _logger.LogInformation($"Duplicate skipped: {record.FirstName} {record.LastName}");
                        continue;
                    }

                    usersToAdd.Add(record);
                    csvNameSet.Add(fullName);
                }
            }
            catch (CsvHelperException ex)
            {
                _logger.LogWarning($"CSV parsing failed: {ex.Message}");
                return View("Create");
            }

            // Add valid users to the database
            _context.Users.AddRange(usersToAdd);
            await _context.SaveChangesAsync();

            // Compute compatibility for all newly added users
            foreach (var user in usersToAdd)
            {
                ComputeCompatibility(user);
            }

            return RedirectToAction(nameof(Login));
        }


        // POST: Users/Create

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create([Bind("FirstName,LastName,Age,Gender,Email,Password,Interests,Bio,CreatedAt")] User user, IFormFile? ProfilePictureFile)
        {
            try
            {
                Console.WriteLine("Processing Create request...");

                // Check if the FirstName already exists in the database
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.FirstName.ToLower() == user.FirstName.ToLower() &&
                        u.LastName.ToLower() == user.LastName.ToLower());

                if (existingUser != null)
                {
                    TempData["DuplicateNameError"] = "true";

                    // Clear only the FirstName and LastName so user can input new ones
                    user.FirstName = string.Empty;
                    user.LastName = string.Empty;

                    return View(user);  // SweetAlert will be triggered from the view
                }


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

                return RedirectToAction(nameof(Login));
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
                new Claim("UserID", user.UserID.ToString())
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties { IsPersistent = true };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                                  new ClaimsPrincipal(claimsIdentity),
                                                  authProperties);

                    // Session storage
                    // Use _httpContextAccessor for all session access


                    _httpContextAccessor.HttpContext?.Session.SetString("UserID", user.UserID.ToString());
                    _httpContextAccessor.HttpContext?.Session.SetString("UserName", user.FirstName + " " + user.LastName);
                    _httpContextAccessor.HttpContext?.Session.SetString("UserEmail", user.Email);


                    // Set TempData for successful login
                    TempData["LoginSuccess"] = "true";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["LoginError"] = "true";
                    return RedirectToAction(nameof(Login));
                }
            }

            return View(model);
        }


        //Logout Functionality
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Clear session storage
            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Set TempData for successful logout
            TempData["LogoutSuccess"] = "true";
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


            // Get the list of users the logged-in user has skipped
            var skippedUserIds = await _context.SkippedUsers
                .Where(su => su.UserId == loggedInUserId)
                .Select(su => su.SkippedUserId)
                .ToListAsync();
            // Filter out skipped users
            var filteredCompatibilityScores = compatibilityScores
                .Where(cs => !skippedUserIds.Contains(cs.User1Id == loggedInUserId ? cs.User2Id : cs.User1Id))
                .ToList();

            // Ensure skippedUserIds contains only valid integers
            skippedUserIds = skippedUserIds.Where(id => id > 0).ToList();



            Hashtable compatibilityScoresHashtable = new Hashtable();
            HashSet<(int, int)> processedPairs = new(); // Track unique matchups

            foreach (var score in compatibilityScores)
            {
                int uid1 = score.User1Id;
                int uid2 = score.User2Id;

                // Normalize the pair to ensure (A,B) and (B,A) are treated the same
                var pairKey = uid1 < uid2 ? (uid1, uid2) : (uid2, uid1);

                if (!processedPairs.Contains(pairKey))
                {
                    int pairedUserId = uid1 == loggedInUserId ? uid2 : uid1;

                    // Skip users that the logged-in user has already skipped
                    if (!skippedUserIds.Contains(pairedUserId))
                    {
                        // Only add if it's the first time this unique pair shows up
                        compatibilityScoresHashtable[pairedUserId] = score.Score;
                    }
                    processedPairs.Add(pairKey);
                }
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

            // compatibility threshold to filter users
            double compatibilityThreshold = 0.02; // threshold here
            var userIds = sortedUsers
                .Where(u => u.Score >= compatibilityThreshold) // Only include users above threshold
                .Select(u => u.UserId)
                .ToList();

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

            var sortedResults = sortedUsers
                .Where(sortedUser => userIds.Contains(sortedUser.UserId)) // Only include the filtered users
                .Select(sortedUser =>
                {
                    var user = users.FirstOrDefault(u => u.UserID == sortedUser.UserId);
                    return new
                    {
                        UserId = user?.UserID,
                        CompatibilityScore = sortedUser.Score,
                        User = user
                    };
                })
                .ToList();

            // Return results with debug message
            return Json(new
            {
                message = $"Duplicate compatibility score pairs handled. Unique pairs processed: {processedPairs.Count}",
                results = sortedResults
            });
        }


        // Improved SkipUser method
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SkipUser([FromBody] SkipUserModel model)
        {
            try
            {
                if (model == null || model.SkippedUserId <= 0)
                {
                    return Json(new { success = false, message = "Invalid user ID provided" });
                }

                int loggedInUserId = int.Parse(HttpContext.Session.GetString("UserID"));

                // Validate the skipped user exists
                var skippedUser = await _context.Users.FindAsync(model.SkippedUserId);
                if (skippedUser == null)
                {
                    return Json(new { success = false, message = "Selected user not found" });
                }

                // Use the execution strategy to handle the transaction
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Find and remove compatibility scores between these users
                        var compatibilityScores = await _context.CompatibilityScores
                            .Where(cs => (cs.User1Id == loggedInUserId && cs.User2Id == model.SkippedUserId) ||
                                         (cs.User1Id == model.SkippedUserId && cs.User2Id == loggedInUserId))
                            .ToListAsync();

                        // Create a record of the skip to prevent future matching
                        var skippedUserRecord = new SkippedUser
                        {
                            UserId = loggedInUserId,
                            SkippedUserId = model.SkippedUserId,
                        };

                        // Check if this user was already skipped to avoid duplicate entries
                        var existingSkipRecord = await _context.SkippedUsers
                            .FirstOrDefaultAsync(su => su.UserId == loggedInUserId && su.SkippedUserId == model.SkippedUserId);

                        // Remove compatibility scores if they exist
                        if (compatibilityScores.Any())
                        {
                            _context.CompatibilityScores.RemoveRange(compatibilityScores);
                        }

                        // Add skip record if it doesn't exist
                        if (existingSkipRecord == null)
                        {
                            _context.SkippedUsers.Add(skippedUserRecord);
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                _logger.LogInformation($"User {loggedInUserId} skipped user {model.SkippedUserId}.");
                return Json(new { success = true, message = "User skipped successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error skipping user: {ex.Message}");
                return Json(new { success = false, message = $"Error skipping user: {ex.Message}" });
            }
        }

    }

    // Define model for SkipUser to properly bind from JSON request
    public class SkipUserModel
    {
        public int SkippedUserId { get; set; }
    }

    // Model to track skipped users
    public class SkippedUser
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SkippedUserId { get; set; }
    }
}