using GreekRecruit.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BCrypt.Net;




namespace GreekRecruit.Controllers
{
    //[Authorize(AuthenticationSchemes = "MyCookieAuth")]
    public class LoginController : Controller
    {

        private readonly SqlDataContext _context;

        public LoginController(SqlDataContext context)
        {
            _context = context;
        }

        //Login page
        public IActionResult Login()
        {
            return View();
        }

        //Submit user credentials to try to log in
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDataAsync(User model)
        {
            var uname = model.username;
            var email = model.email;
            var enteredPassword = model.password;

            // Try to find a user by username and email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == uname && u.email == email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid credentials!";
                return View("Login");
            }

            bool isPasswordValid;

            if (user.is_hashed_passowrd == "Y") // Note: consider renaming to is_hashed_password in the DB later
            {
                // Password is hashed
                isPasswordValid = BCrypt.Net.BCrypt.Verify(enteredPassword, user.password);
            }
            else
            {
                // Password is still plaintext (legacy support)
                isPasswordValid = (user.password == enteredPassword);

                if (isPasswordValid)
                {
                    // Upgrade this user to hashed password
                    user.password = BCrypt.Net.BCrypt.HashPassword(enteredPassword);
                    user.is_hashed_passowrd = "Y";
                    await _context.SaveChangesAsync();
                }
            }

            if (!isPasswordValid)
            {
                TempData["ErrorMessage"] = "Invalid credentials!";
                return View("Login");
            }

            // Sign in
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, uname)
    };

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("MyCookieAuth", principal);

            return RedirectToAction("Index", "Home");
        }

    }
}
