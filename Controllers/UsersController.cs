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

namespace Lovebirds.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        public IActionResult GetProfilePicture(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null || user.ProfilePicture == null || user.ProfilePicture.Length == 0)
            {
                return NotFound(); // Or return a default image
            }

            return File(user.ProfilePicture, "image/*"); // Assuming the images are JPGs
        }




        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
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

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        public static class CompatibilityStore
        {
            public static ConcurrentDictionary<int, Dictionary<int, double>> CompatibilityScores = new();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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

                // Compute compatibility score
                ComputeCompatibility(user);


                Console.WriteLine("User saved successfully!");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while saving: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while saving. Check the logs.");
                return View(user);
            }
        }

        private void ComputeCompatibility(User newUser)
        {
            var users = _context.Users.ToList();
            Dictionary<int, double> scores = new();

            foreach (var existingUser in users)
            {
                if (existingUser.UserID == newUser.UserID) continue;

                double similarity = CalculateJaccardSimilarity(newUser.Interests, existingUser.Interests);
                double agePenalty = 1 / (1 + Math.Abs(newUser.Age - existingUser.Age));
                double compatibilityScore = similarity * agePenalty;

                scores[existingUser.UserID] = compatibilityScore;

                // Store in existing user entry as well
                if (CompatibilityStore.CompatibilityScores.TryGetValue(existingUser.UserID, out var existingScores))
                {
                    existingScores[newUser.UserID] = compatibilityScore;
                }
                else
                {
                    CompatibilityStore.CompatibilityScores[existingUser.UserID] = new Dictionary<int, double> { { newUser.UserID, compatibilityScore } };
                }
            }

            CompatibilityStore.CompatibilityScores[newUser.UserID] = scores;

            // LOG COMPATIBILITY SCORES FOR DEBUGGING
            Console.WriteLine($"Stored Compatibility Scores for User {newUser.UserID}:");
            foreach (var kvp in scores)
            {
                Console.WriteLine($" - With User {kvp.Key}: {kvp.Value:F4}");
            }
        }

        


        private double CalculateJaccardSimilarity(string interests1, string interests2)
        {
            var set1 = new HashSet<string>(interests1.Split(',', StringSplitOptions.RemoveEmptyEntries));
            var set2 = new HashSet<string>(interests2.Split(',', StringSplitOptions.RemoveEmptyEntries));

            int intersection = set1.Intersect(set2).Count();
            int union = set1.Union(set2).Count();

            return union == 0 ? 0 : (double)intersection / union;
        }




























        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserID,FirstName,LastName,Age,Gender,Email,Password,Interests,ProfilePicture,Bio,CreatedAt")] User user)
        {
            if (id != user.UserID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Delete/5
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

        // POST: Users/Delete/5
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
    }
}