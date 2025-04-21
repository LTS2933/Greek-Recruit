using GreekRecruit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace GreekRecruit.Controllers
{
    public class EventController : Controller
    {
        private readonly SqlDataContext _context;

        public EventController(SqlDataContext context)
        {
            _context = context;
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);

            if (user == null)
            {
                return Unauthorized();
            }

            var events = await _context.Events
                .Where(e => e.organization_id == user.organization_id)
                .OrderBy(e => e.event_datetime)
                .ToListAsync();

            return View(events);
        }

    }
}
