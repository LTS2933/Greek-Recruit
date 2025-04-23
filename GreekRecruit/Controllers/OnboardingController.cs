using GreekRecruit.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GreekRecruit.Controllers
{
    public class OnboardingController : Controller
    {
        private readonly StripeService _stripeService;

        public OnboardingController(StripeService stripeService)
        {
            _stripeService = stripeService;
        }

        [HttpGet]
        public IActionResult Start()
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
        public async Task<IActionResult> CreateSubscription(string orgName, string adminEmail, string paymentMethodId)
        {
            try
            {
                var subscriptionId = await _stripeService.CreateSubscriptionAsync(orgName, adminEmail, paymentMethodId);

                // Log subscriptionId, orgName, and adminEmail to database if needed

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