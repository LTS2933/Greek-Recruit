using GreekRecruit.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;


namespace GreekRecruit.Controllers
{
    [Authorize(AuthenticationSchemes = "MyCookieAuth")]
    public class ProfileController : Controller
    {
        private readonly SqlDataContext _context;
        private readonly IConfiguration _configuration;

        public ProfileController(SqlDataContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        //Profile view of a user, showing basic credentials
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);

            if (user == null) return Unauthorized();

            return View(user);
        }

        //The View for Adding a new User to the Organization is rendered
        [Authorize]
        public async Task<IActionResult> AddUsers()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
            if (user == null) return Unauthorized();

            if (user.role == "Admin")
            {
                return View();
            }
            else
            {
                return Forbid();
            }
        }


        //Handles form data, emailing, and adding new User to DB
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUserData(string email, string full_name)
        {
            var curr_user_uname = User.Identity?.Name;
            var curr_user = await _context.Users.FirstOrDefaultAsync(u => u.username == curr_user_uname);
            if (curr_user == null) return Unauthorized();
            if (curr_user.role != "Admin") return Forbid();

            if (await _context.Users.AnyAsync(u => u.email == email))
            {
                TempData["ErrorMessage"] = $"Email {email} already exists!";
                return RedirectToAction("AddUsers");
            }
            else
            {
                try
                {

                    var emailSettings = _configuration.GetSection("EmailSettings");
                    var smtpServer = emailSettings["SmtpServer"];
                    var port = int.Parse(emailSettings["Port"]);
                    var username = Environment.GetEnvironmentVariable("SMTP_USER");
                    var password = Environment.GetEnvironmentVariable("SMTP_KEY");

                    var mail = new MailMessage();
                    mail.From = new MailAddress(username);
                    mail.To.Add(email);


                    int ampersand_index = email.IndexOf("@");
                    if (MailAddress.TryCreate(email, out _))
                    {
                        User user = new User();
                        user.username = email.Substring(0, ampersand_index);
                        user.email = email;
                        user.full_name = full_name;
                        user.role = "User";
                        user.password = GenerateRandomPassword();
                        
                        //Will want to implement a password hashing algorithm here
                        //var hasher = new PasswordHasher<User>();
                        //string hashedPassword = hasher.HashPassword(user, user.password);
                        //user.password = hashedPassword;
                        user.is_hashed_passowrd = "N";

                        var current_user_username = User.Identity?.Name;
                        var current_user = await _context.Users.FirstOrDefaultAsync(u => u.username == current_user_username);

                        //if (current_user == null)
                        //{
                        //    ViewData["ErrorMessage"] = "User not found. Please log in again.";
                        //    return RedirectToAction("Login", "Login");
                        //}

                        user.organization_id = current_user.organization_id;

                        //_context.Add<User>(user);
                        //_context.SaveChanges();

                        mail.Subject = "Join GreekRecruit!";
                        mail.Body = $"You've been invited to join GreekRecruit by your admin, {current_user.full_name}.\nYou can now log in using this email.\nUsername: {user.username}\nPassword: {user.password}" +
                        "\nFor security reasons, please reset your password. You can do so by clicking the Settings button in your profile dropdown.";

                        var smtpClient = new SmtpClient(smtpServer)
                        {
                            Port = port,
                            Credentials = new NetworkCredential(username, password),
                            EnableSsl = true,
                        };

                        using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            await smtpClient.SendMailAsync(mail);

                            _context.Users.Add(user);
                            await _context.SaveChangesAsync();

                            await transaction.CommitAsync();

                            TempData["SuccessMessage"] = $"User added and email sent to {email}";
                        }
                        catch (Exception innerEx)
                        {
                            await transaction.RollbackAsync();
                            TempData["ErrorMessage"] = $"Error adding user or sending email: {innerEx.Message}";
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Invalid email address! Please input a valid email address.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error sending email: {ex.Message}";
                }

                return RedirectToAction("AddUsers");
            }
        }

        //Helper method for AddUserData. Generates a random, valid password
        private string GenerateRandomPassword()
        {
            const string alphanumerics = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            const string specialChars = "!@#$%^&*()-_=+<>?";

            var random = new Random();
            var passwordChars = new char[8];

            for (int i = 0; i < 6; i++)
                passwordChars[i] = alphanumerics[random.Next(alphanumerics.Length)];

            for (int i = 6; i < 8; i++)
                passwordChars[i] = specialChars[random.Next(specialChars.Length)];

            return new string(passwordChars.OrderBy(c => Guid.NewGuid()).ToArray());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(string newPassword)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
            if (user == null) return Unauthorized();

            if (newPassword.Length < 7 || newPassword.Length > 20)
            {
                TempData["ErrorMessage"] = "Password must be between 8 and 20 characters and contain at least one special character!";
                return RedirectToAction("Index");
            }

            String pattern = @"^(?=.*[^a-zA-Z0-9]).+$";
            Regex regex = new Regex(pattern);

            if (!regex.IsMatch(newPassword))
            {
                TempData["ErrorMessage"] = "Password must be between 8 and 20 characters and contain at least one special character!";
                return RedirectToAction("Index");
            }

            // Hash the new password
            user.password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.is_hashed_passowrd = "Y"; // You can add this bool column to track who has upgraded

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Password updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating password: {ex.Message}";
            }

            return RedirectToAction("Index");
        }



        //Logout
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Login", "Login");
        }
    }
}
