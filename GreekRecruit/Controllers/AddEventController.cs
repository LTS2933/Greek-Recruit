using GreekRecruit.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreekRecruit.Controllers
{
    public class AddEventController : Controller
    {
        private readonly SqlDataContext _context;

        public AddEventController(SqlDataContext context)
        {
            _context = context;
        }

        //Returns the view for a user to add an event after filling out the provided fields on the pag
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);

            if (user == null) return Unauthorized();
            if (user.role != "Admin") return Forbid();
            
            var model = new Event
            {
                organization_id = user.organization_id
            };

            return View("Index", model);
        }

        //Handles the form submission for adding an event
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitEventData(Event model)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
            if (user == null) return Unauthorized();
            if (user.role != "Admin") return Forbid();

            if (ModelState.IsValid)
            {
                _context.Events.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Event added successfully!";
                return RedirectToAction("Index", "AddEvent");
            }

            return View("Index", model);



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
