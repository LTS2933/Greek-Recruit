using GreekRecruit.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreekRecruit.Controllers
{
    public class EventAttendanceController : Controller
    {
        private readonly SqlDataContext _context;
        public EventAttendanceController (SqlDataContext context)
        {
            _context = context;
        }

        //Returns the view for ANYONE to say they attended a given event
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(int event_id)
        {
            var r_event = await _context.Events.FirstOrDefaultAsync(e => e.event_id == event_id);
            if (r_event == null)
            {
                TempData["FlashMessage"] = "Event ID not found.";
                return RedirectToAction("Index", "Home");
            }

            return View(r_event);
        }

        //Handles the form submission for a user to say they attended an event
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAttendance(EventAttendance attendance)
        {
            if (!ModelState.IsValid)
            {
                TempData["FlashMessage"] = "Please fill out all required fields.";
                return RedirectToAction("Index", new { event_id = attendance.event_id });
            }

            if (string.IsNullOrWhiteSpace(attendance.pnm_fname) || string.IsNullOrWhiteSpace(attendance.pnm_lname))
            {
                TempData["FlashMessage"] = "Name is required.";
                return RedirectToAction("Index", new { event_id = attendance.event_id });
            }

            attendance.checked_in_at = DateTime.Now;

            _context.EventsAttendance.Add(attendance);
            await _context.SaveChangesAsync();

            TempData["FlashMessage"] = "Thanks! Your attendance has been recorded.";
            return RedirectToAction("ThankYou");
        }

        //Returns the view of the site just saying user is good after filling out form
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ThankYou()
        {
            return View();
        }

        //Returns the view of PNMs who attended a given event
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ViewAttendees(int event_id)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);

            if (user == null) return Unauthorized();

            var r_event = await _context.Events.FirstOrDefaultAsync(e => e.event_id == event_id && e.organization_id == user.organization_id);
            if (r_event == null) return NotFound();

            var attendees = await _context.EventsAttendance
                .Where(a => a.event_id == event_id && a.organization_id == user.organization_id)
                .OrderByDescending(a => a.checked_in_at)
                .ToListAsync();

            ViewData["EventName"] = r_event.event_name;
            return View(attendees);
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
