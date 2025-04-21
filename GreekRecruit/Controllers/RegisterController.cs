using GreekRecruit.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GreekRecruit.Controllers
{
    public class RegisterController : Controller
    {
        private readonly SqlDataContext _context;

        public RegisterController(SqlDataContext context)
        {
            _context = context;
        }
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public IActionResult SubmitRegister(User model) 
        {
            String? uname = model.username;
            String? password1 = model.password;
            String? password2 = model.confirmPassword;

            if (uname == null || password1 == null || password2 == null)
            {
                TempData["FlashMessage"] = "One or more fields are empty!";
                return View("Register");
            }

            if (uname.Length < 7 || uname.Length > 15)
            {
                TempData["FlashMessage"] = "Username must be between 8 and 15 characters!";
                return View("Register");
            }

            if (password1.Length < 7 || password1.Length > 20)
            {
                TempData["FlashMessage"] = "Password must be between 8 and 20 characters!";
                return View("Register");
            }
            if (password1 != password2)
            {
                TempData["FlashMessage"] = "Passwords must match!";
                return View("Register");
            }
            String pattern = @"^(?=.*[^a-zA-Z0-9]).+$";
            Regex regex = new Regex(pattern);

            if (regex.IsMatch(password1)) {
                _context.Add<User>(model);
                _context.SaveChanges();

                TempData["FlashMessage"] = "You can now log in with your username and password!";
                return View("~/Views/Login/Login.cshtml");
            }
            else
            {
                TempData["FlashMessage"] = "Passwords does not contain at least 1 special character!";
                return View("Register");
            }
        }
    }
}
