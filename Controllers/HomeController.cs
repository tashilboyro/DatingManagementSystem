using System.Diagnostics;
using System.Security.Claims;
using DatingManagementSystem.Data;
using DatingManagementSystem.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http; // Required for session management

namespace DatingManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }
        public IActionResult Home()
        {

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

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
                Console.WriteLine("User saved successfully!");

                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while saving: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while saving. Check the logs.");
                return View(user);
            }
        }
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

                    return RedirectToAction(nameof(Home));
                }
                else
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                }
            }
            return View(model);
        }
        public IActionResult TestSession()
        {
            HttpContext.Session.SetString("TestKey", "Hello, Session!");
            var sessionValue = HttpContext.Session.GetString("TestKey");

            if (string.IsNullOrEmpty(sessionValue))
            {
                return Content("Session is NOT working!");
            }

            return Content($"Session Value: {sessionValue}");
        }

    }
}
