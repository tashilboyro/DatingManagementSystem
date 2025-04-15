using Microsoft.VisualStudio.TestTools.UnitTesting;
using DatingManagementSystem.Controllers;
using DatingManagementSystem.Data;
using DatingManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace DatingManagementSystem.Tests
{
    [TestClass]
    public class CompatibilityTests
    {
        private ApplicationDbContext _context;
        private UsersController _controller;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDB")
                .Options;

            _context = new ApplicationDbContext(options);

            // Seed initial users
            _context.Users.AddRange(
                new User { UserID = 1, FirstName = "Alice", LastName = "Scott", Age = 25, Gender = "Female", Bio = "Hello there, I'm Alice", Email = "Alicescott@example.com", Interests = "music,travel,coding",Password = "qwerty" },
                new User {UserID = 2, FirstName = "Bob", LastName = "Vance", Age = 28, Gender = "Male", Bio = "Hello there, I'm Bob", Email = "Bobvance@example.com", Interests = "coding,reading,travel", Password = "qwerty" },
                new User {UserID = 3, FirstName = "Charlie", LastName = "Sheen", Age = 40, Gender = "Male", Bio = "Hello there, I'm Charlie", Email = "CharlieSheen@example.com", Interests = "gaming,travel", Password = "qwerty" }
            );
            _context.SaveChanges();

            var logger = new Mock<ILogger<UsersController>>().Object;
            var accessor = new Mock<IHttpContextAccessor>().Object;
            _controller = new UsersController(logger, _context, accessor);
        }

        [TestMethod]
        public void ComputeCompatibility_AddsCorrectScores()
        {
            // Arrange: Add new user to compute against others
            var newUser = new User {UserID = 4, FirstName = "Diana", LastName = "Johnson", Age = 26, Gender = "Female", Bio = "Hello there, I'm Diana", Email = "Dianajohnson@example.com", Interests = "coding,travel", Password = "qwerty" };
            _context.Users.Add(newUser);
            _context.SaveChanges();

            // Act: Call the private ComputeCompatibility method
            var method = typeof(UsersController).GetMethod("ComputeCompatibility",
                         System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method!.Invoke(_controller, new object[] { newUser });

            // Assert
            var scores = _context.CompatibilityScores
                .Where(s => s.User1Id == 4 || s.User2Id == 4)
                .ToList();

            Assert.AreEqual(6, scores.Count); // 3 other users x 2 (bidirectional)

            var bobScore = scores.FirstOrDefault(s => s.User1Id == 4 && s.User2Id == 2)?.Score;
            Assert.IsNotNull(bobScore);
            Assert.IsTrue(bobScore > 0.1); // Some similarity and close age

            var charlieScore = scores.FirstOrDefault(s => s.User1Id == 4 && s.User2Id == 3)?.Score;
            Assert.IsNotNull(charlieScore);
            Assert.IsTrue(charlieScore < bobScore); // Bigger age difference
        }
    }
}
