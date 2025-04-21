using Microsoft.AspNetCore.Mvc;
using GreekRecruit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;

namespace GreekRecruit.Controllers;

public class InterviewController : Controller
{
    private readonly SqlDataContext _context;

    public InterviewController(SqlDataContext context)
    {
        _context = context;
    }

    //Returns the view for the interview schedule
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var username = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
        if (user == null) return Unauthorized();
       

        //var interviews = await _context.Interviews
        //    .Where(i => i.organization_id == user.organization_id)
        //    .Include(i => i.pnm_id)
        //    .OrderBy(i => i.interview_datetime)
        //    .ToListAsync();

        return View();
    }

    //Returns the view for scheduling a new interview (admin only)
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Schedule()
    {
        var username = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
        if (user == null) return Unauthorized();
        if (user.role != "Admin") return Forbid();

        var pnms = await _context.PNMs
            .Where(p => p.organization_id == user.organization_id)
            .ToListAsync();

        ViewData["PNMs"] = pnms;
        return View();
    }

    //HTTP POST method that submits new interview data
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Schedule(Interview interview)
    {
        var username = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
        if (user == null) return Unauthorized();
        if (user.role != "Admin") return Forbid();

        interview.organization_id = user.organization_id;
        interview.interviewer_user_id = user.user_id;

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please check all required fields.";
            return RedirectToAction("Schedule");
        }

        _context.Interviews.Add(interview);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Interview scheduled successfully.";
        return RedirectToAction("Index");
    }

    //Method for for canceling an interview (admin only)
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int interview_id)
    {
        var interview = await _context.Interviews.FindAsync(interview_id);
        if (interview == null) return NotFound();

        var username = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
        if (user == null) return Unauthorized();
        if (user.role != "Admin") return Forbid();

        _context.Interviews.Remove(interview);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Interview canceled successfully.";
        return RedirectToAction("Index");
    }

    //Returns a JSON object of all interviews for the logged-in user, which the view uses to populate calendar
    [HttpGet]
    public async Task<IActionResult> GetInterviews()
    {
        var username = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
        if (user == null) return Unauthorized();

        var interviews = await _context.Interviews
            .Where(i => i.organization_id == user.organization_id)
            .Join(_context.PNMs,
                  interview => interview.pnm_id,
                  pnm => pnm.pnm_id,
                  (interview, pnm) => new
                  {
                      interview.interview_id,
                      interview.interview_datetime,
                      pnm.pnm_fname,
                      pnm.pnm_lname
                  })
            .ToListAsync();

        //Console.WriteLine("Fetched interviews:");
        //foreach (var i in interviews)
        //{
        //    Console.WriteLine($"{i.pnm_fname} {i.pnm_lname} - {i.interview_datetime}");
        //}

        var events = interviews.Select(i => new
        {
            id = i.interview_id,
            title = $"{i.pnm_fname} {i.pnm_lname}",
            start = i.interview_datetime.ToString("s"),
            end = i.interview_datetime.AddMinutes(30).ToString("s"),
            allDay = false
        });

        return Json(events);
    }

    //Logout
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("MyCookieAuth");
        return RedirectToAction("Login", "Login");
    }
}
