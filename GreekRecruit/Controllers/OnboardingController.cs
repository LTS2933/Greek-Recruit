using GreekRecruit.Models;
using Microsoft.AspNetCore.Mvc;

namespace GreekRecruit.Controllers
{
    public class OnboardingController : Controller
    {
        [HttpGet]
        public IActionResult Start()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Payment()
        {
            return View(); // Payment form or Stripe integration
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmPayment(string orgName, string adminEmail)
        {
            // TODO: Add payment confirmation logic here (e.g., via Stripe webhook or client call)

            // Optionally save interest to DB or email
            // Could also log this to a table or send a notification

            TempData["SuccessMessage"] = "Thanks! We'll be in touch shortly to get your organization set up.";
            return RedirectToAction("Success");
        }

        [HttpGet]
        public IActionResult Success()
        {
            return View(); // Success message view
        }
    }
}
