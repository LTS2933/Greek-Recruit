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
using GreekRecruit.Services;


namespace GreekRecruit.Controllers
{
    [Authorize(AuthenticationSchemes = "MyCookieAuth")]
    public class ProfileController : Controller
    {
        private readonly SqlDataContext _context;
        private readonly IConfiguration _configuration;
        private readonly StripeService _stripeService;

        public ProfileController(SqlDataContext context, IConfiguration configuration, StripeService stripeService)
        {
            _context = context;
            _configuration = configuration;
            _stripeService = stripeService;
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

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(full_name))
            {
                TempData["ErrorMessage"] = "Email and full name are required!";
                return RedirectToAction("AddUsers");
            }

            if (await _context.Users.AnyAsync(u => u.email == email))
            {
                TempData["ErrorMessage"] = $"Email {email} already exists!";
                return RedirectToAction("AddUsers");
            }

            try
            {
                var organization = await _context.Organizations.FirstOrDefaultAsync(o => o.organization_id == curr_user.organization_id);
                if (organization == null)
                {
                    TempData["ErrorMessage"] = "Organization settings not configured.";
                    return RedirectToAction("AddUsers");
                }

                string smtpServer = organization.smtp_server;
                string smtpUsername = organization.smtp_username;
                string smtpPasswordDecrypted;

                try
                {
                    smtpPasswordDecrypted = AesEncryptionHelper.Decrypt(organization.smtp_password);
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Failed to decrypt SMTP password: {ex.Message}";
                    return RedirectToAction("AddUsers");
                }

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPasswordDecrypted))
                {
                    TempData["ErrorMessage"] = "Organization SMTP settings are incomplete.";
                    return RedirectToAction("AddUsers");
                }

                var mail = new MailMessage();
                mail.From = new MailAddress(smtpUsername);
                mail.To.Add(email);

                if (!MailAddress.TryCreate(email, out _))
                {
                    TempData["ErrorMessage"] = "Invalid email address! Please input a valid email address.";
                    return RedirectToAction("AddUsers");
                }

                // Create the new user
                var user = new User
                {
                    username = email.Substring(0, email.IndexOf("@")),
                    email = email,
                    full_name = full_name,
                    role = "User",
                    password = GenerateRandomPassword(),
                    is_hashed_passowrd = "N",
                    organization_id = curr_user.organization_id
                };

                mail.Subject = "Join GreekRecruit!";
                mail.Body = $"You've been invited to join GreekRecruit by your admin, {curr_user.full_name}.\nYou can now log in using these credentials.\nUsername: {user.username}\nPassword: {user.password}" +
                            "\nFor security reasons, please reset your password. You can do so by clicking the Settings button in your profile dropdown.";

                var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = organization.smtp_port ?? 587,
                    Credentials = new NetworkCredential(smtpUsername, smtpPasswordDecrypted),
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
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error sending email: {ex.Message}";
            }

            return RedirectToAction("AddUsers");
        }

        //The View for batch adding new users to the Organization is rendered
        [Authorize]
        public async Task<IActionResult> BatchAddUsers()
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

        //Submits the form handling batch adding new users
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchAddUserData(List<string> fullNames, List<string> emails)
        {
            var curr_user_uname = User.Identity?.Name;
            var curr_user = await _context.Users.FirstOrDefaultAsync(u => u.username == curr_user_uname);
            if (curr_user == null) return Unauthorized();
            if (curr_user.role != "Admin") return Forbid();

            if (fullNames == null || emails == null || fullNames.Count != emails.Count)
            {
                TempData["ErrorMessage"] = "Mismatch or missing user data.";
                return RedirectToAction("BatchAddUsers");
            }

            var organization = await _context.Organizations.FirstOrDefaultAsync(o => o.organization_id == curr_user.organization_id);
            if (organization == null)
            {
                TempData["ErrorMessage"] = "Organization settings not configured.";
                return RedirectToAction("BatchAddUsers");
            }

            string smtpServer = organization.smtp_server;
            string smtpUsername = organization.smtp_username;
            string smtpPasswordDecrypted = AesEncryptionHelper.Decrypt(organization.smtp_password);

            var smtpClient = new SmtpClient(smtpServer)
            {
                Port = organization.smtp_port ?? 587,
                Credentials = new NetworkCredential(smtpUsername, smtpPasswordDecrypted),
                EnableSsl = true,
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int usersAdded = 0;

                for (int i = 0; i < emails.Count; i++)
                {
                    var email = emails[i];
                    var fullName = fullNames[i];

                    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(fullName))
                        continue;

                    if (await _context.Users.AnyAsync(u => u.email == email))
                        continue;

                    var newUser = new User
                    {
                        username = email.Substring(0, email.IndexOf("@")),
                        email = email,
                        full_name = fullName,
                        role = "User",
                        password = GenerateRandomPassword(),
                        is_hashed_passowrd = "N",
                        organization_id = curr_user.organization_id
                    };

                    var mail = new MailMessage
                    {
                        From = new MailAddress(smtpUsername),
                        Subject = "Join GreekRecruit!",
                        Body = $"You've been invited to join GreekRecruit by your admin, {curr_user.full_name}.\nYou can now log in using these credentials.\nUsername: {newUser.username}\nPassword: {newUser.password}" +
                               "\nFor security reasons, please reset your password.",
                        IsBodyHtml = false
                    };
                    mail.To.Add(email);

                    await smtpClient.SendMailAsync(mail);

                    _context.Users.Add(newUser);
                    usersAdded++;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"{usersAdded} users added and invited successfully!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error adding users: {ex.Message}";
            }

            return RedirectToAction("BatchAddUsers");
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

        //Handles the form for updating the user's password
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
            user.is_hashed_passowrd = "Y";

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

        //Handles the form for canceling the user's subscription with Stripe
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CancelSubscription()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);

            if (user == null) return Unauthorized();
            if (string.IsNullOrEmpty(user.SubscriptionId))
            {
                TempData["ErrorMessage"] = "Unable to cancel subscription.";
                return RedirectToAction("Index");
            }
            if (user.role != "Admin") return Forbid();

            try
            {
                await _stripeService.CancelSubscriptionAsync(user.SubscriptionId);

                // Optionally update DB to reflect cancellation
                user.SubscriptionId = null;
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Your subscription has been successfully cancelled.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Cancellation failed: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        //Shows the Points Categories codetable view for the Organization
        [Authorize]
        public async Task<IActionResult> PointsCategories()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
            if (user == null) return Unauthorized();

            var categories = await _context.PointsCategories
                .Where(c => c.organization_id == user.organization_id)
                .ToListAsync();

            return View(categories);
        }

        //Handles the form submission for Points Categories
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePointsCategories(List<PointsCategory> categories)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
            if (user == null) return Unauthorized();
            if (user.role != "Admin") return Forbid();

            foreach (var updatedCategory in categories)
            {
                var category = await _context.PointsCategories
                    .FirstOrDefaultAsync(c => c.PointsCategoryID == updatedCategory.PointsCategoryID
                                           && c.organization_id == user.organization_id);

                if (category != null)
                {
                    category.PointsValue = updatedCategory.PointsValue;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Points categories updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating points categories: {ex.Message}";
            }

            return RedirectToAction("PointsCategories");
        }



        //Encryption tool for me only, will be used to encrypt user's SMTP app password from Google
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EncryptPasswordTool()
        {
            var curr_user_uname = User.Identity?.Name;
            var curr_user = await _context.Users.FirstOrDefaultAsync(u => u.username == curr_user_uname);

            if (curr_user.username != "LiamSmith12")
            {
                return Forbid();
            }

            return View();
        }

        //Handles the form for encrypting a password
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EncryptPasswordTool(string plainPassword)
        {
            var curr_user_uname = User.Identity?.Name;
            var curr_user = await _context.Users.FirstOrDefaultAsync(u => u.username == curr_user_uname);

            if (curr_user?.username != "LiamSmith12")
            {
                return Forbid();
            }

            if (string.IsNullOrEmpty(plainPassword))
            {
                TempData["ErrorMessage"] = "Password cannot be empty.";
                return RedirectToAction("EncryptPasswordTool");
            }

            try
            {
                var encryptedPassword = AesEncryptionHelper.Encrypt(plainPassword);
                TempData["SuccessMessage"] = $"Encrypted password: {encryptedPassword}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Encryption error: {ex.Message}";
            }

            return RedirectToAction("EncryptPasswordTool");
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
