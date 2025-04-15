using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using DatingManagementSystem.Controllers;
using DatingManagementSystem.Data;
using DatingManagementSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Text.Json;


[TestClass]
public class SkipUserTests
{
    private Mock<ApplicationDbContext> _mockContext;
    private Mock<ILogger<UsersController>> _mockLogger;
    private UsersController _controller;
    private DefaultHttpContext _httpContext;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);

        // Seed sample users
        context.Users.AddRange(
         new User { UserID = 1, FirstName = "Alice", LastName = "Scott", Age = 25, Gender = "Female", Bio = "Hello there, I'm Alice", Email = "Alicescott@example.com", Interests = "music,travel,coding", Password = "qwerty" },
                new User { UserID = 2, FirstName = "Bob", LastName = "Vance", Age = 28, Gender = "Male", Bio = "Hello there, I'm Bob", Email = "Bobvance@example.com", Interests = "coding,reading,travel", Password = "qwerty" }
             );
        context.SaveChanges();

        // Mock ILogger
        var mockLogger = new Mock<ILogger<UsersController>>();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.Session = new TestSession(); // Custom session class defined below
        httpContext.Session.SetString("UserID", "1");

        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Initialize controller
        _controller = new UsersController(mockLogger.Object, context, mockHttpContextAccessor.Object);
    }

    [TestMethod]
    public async Task SkipUser_InvalidUserId_ReturnsError()
    {
        var result = await _controller.SkipUser(new SkipUserModel { SkippedUserId = -1 }) as JsonResult;

        var jsonString = JsonSerializer.Serialize(result.Value);
        var jsonData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);

        Assert.AreEqual(false, bool.Parse(jsonData["success"].ToString()));
        Assert.AreEqual("Invalid user ID provided", jsonData["message"].ToString());
    }

    [TestMethod]
    public async Task SkipUser_UserNotFound_ReturnsError()
    {
        var model = new SkipUserModel { SkippedUserId = 0 };
        var result = await _controller.SkipUser(model) as JsonResult;

        var jsonString = JsonSerializer.Serialize(result.Value);
        var json = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);

        Assert.AreEqual(false, bool.Parse(json["success"].ToString()));
        Assert.AreEqual("Invalid user ID provided", json["message"].ToString());
    }


    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _sessionStorage = new Dictionary<string, byte[]>();

        public IEnumerable<string> Keys => _sessionStorage.Keys;

        public string Id => Guid.NewGuid().ToString();
        public bool IsAvailable => true;

        public void Clear() => _sessionStorage.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _sessionStorage.Remove(key);

        public void Set(string key, byte[] value) => _sessionStorage[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value);
    }

}

