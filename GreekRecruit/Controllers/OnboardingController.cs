using GreekRecruit.Models;
using GreekRecruit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace GreekRecruit.Controllers
{
    public class OnboardingController : Controller
    {
        private readonly StripeService _stripeService;
        private readonly SqlDataContext _context;

        public OnboardingController(StripeService stripeService, SqlDataContext context)
        {
            _stripeService = stripeService;
            _context = context;
        }

        [HttpGet]
        public IActionResult Start()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Landing()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Start(string orgName, string adminEmail)
        {
            if (string.IsNullOrWhiteSpace(orgName) || string.IsNullOrWhiteSpace(adminEmail))
            {
                TempData["ErrorMessage"] = "Organization name and email are required.";
                return RedirectToAction("Start");
            }

            ViewData["OrgName"] = orgName;
            ViewData["AdminEmail"] = adminEmail;
            ViewData["PublishableKey"] = Environment.GetEnvironmentVariable("Stripe__PublishableKey");
            var stripeKey = Environment.GetEnvironmentVariable("Stripe__PublishableKey");
            Console.WriteLine($"Stripe Publishable Key: {stripeKey}");

            return View("Payment");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubscription(
             string orgName,
             string adminEmail,
             string paymentMethodId,
             string billingName,
             string billingAddress,
             string billingPostalCode,
             string billingPhone)
        {
            try
            {
                var subscriptionId = await _stripeService.CreateSubscriptionAsync(
                    orgName,
                    adminEmail,
                    paymentMethodId,
                    billingName,
                    billingAddress,
                    billingPostalCode,
                    billingPhone);

                var user = await _context.Users.FirstOrDefaultAsync(u => u.email == adminEmail);
                if (user != null)
                {
                    user.SubscriptionId = subscriptionId;
                }
                else
                {
                    _context.Users.Add(new User
                    {
                        username = adminEmail.Split('@')[0],
                        email = adminEmail,
                        SubscriptionId = subscriptionId,
                        role = "Admin",
                    });
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thanks! We’ll be in touch shortly to get your organization set up.";
                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Start");
            }
        }


        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }
    }
}