using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using GreekRecruit.Models;
using System.Text.Encodings.Web;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using GreekRecruit.Services;

namespace GreekRecruit.Controllers;

public class HomeController : Controller
{

    private readonly SqlDataContext _context;
    private readonly S3Service _s3Service;

    public HomeController(SqlDataContext context, S3Service s3Service)
    {
        _context = context;
        _s3Service = s3Service;
    }

    //Homepage
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index(string? semester, string? status, string? search, string? sort)
    {
        var username = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
        if (user == null) return Unauthorized();

        semester ??= GetCurrentSemester();

        var pnmsQuery = _context.PNMs
            .Where(p => p.organization_id == user.organization_id && p.pnm_semester == semester);

        var validStatuses = new[] { "Offered", "No Offer", "Pending", "Declined", "Accepted" };
        if (!string.IsNullOrEmpty(status) && validStatuses.Contains(status))
        {
            pnmsQuery = pnmsQuery.Where(p => p.pnm_status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            pnmsQuery = pnmsQuery.Where(p =>
                (p.pnm_fname + " " + p.pnm_lname).ToLower().Contains(search));
        }

        pnmsQuery = sort switch
        {
            "name_asc" => pnmsQuery.OrderBy(p => p.pnm_lname).ThenBy(p => p.pnm_fname),
            "name_desc" => pnmsQuery.OrderByDescending(p => p.pnm_lname).ThenByDescending(p => p.pnm_fname),
            "gpa_asc" => pnmsQuery.OrderBy(p => p.pnm_gpa),
            "gpa_desc" => pnmsQuery.OrderByDescending(p => p.pnm_gpa),
            "date_newest" => pnmsQuery.OrderByDescending(p => p.pnm_dateadded),
            "date_oldest" => pnmsQuery.OrderBy(p => p.pnm_dateadded),
            _ => pnmsQuery.OrderBy(p => p.pnm_lname)
        };

        var pnms = await pnmsQuery.ToListAsync();

        foreach (var pnm in pnms)
        {
            if (!string.IsNullOrEmpty(pnm.pnm_profilepictureurl))
            {
                pnm.pnm_profilepictureurl = _s3Service.GetFileUrl(pnm.pnm_profilepictureurl);
            }
        }

        List<AdminTask> taskPreview = new();
        if (user.role == "Admin")
        {
            taskPreview = await _context.AdminTasks
                .Where(t => t.organization_id == user.organization_id && !t.is_completed)
                .OrderBy(t => t.due_date ?? t.date_created)
                .Take(3)
                .ToListAsync();
        }

        ViewData["TaskPreview"] = taskPreview;
        ViewData["CurrentSemester"] = semester;

        var now = DateTime.UtcNow;

        var activeEvent = await _context.Events
            .Where(e => e.event_datetime <= now && e.event_datetime.AddHours(3) >= now)
            .OrderBy(e => e.event_datetime)
            .FirstOrDefaultAsync();

        ViewData["ActiveEvent"] = activeEvent;


        return View(pnms);
    }


    //Applies a status change or deletes PNMs from homepage
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchUpdate(int[] selectedPnms, string newStatus, string newSemester, bool delete = false)
    {
        var username = User.Identity.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
        if (user == null) return Unauthorized();
        if (user.role != "Admin") return Forbid();

        if (selectedPnms == null || selectedPnms.Length == 0)
        {
            TempData["ErrorMessage"] = "No PNMs selected.";
            return RedirectToAction("Index");
        }

        var pnms = await _context.PNMs
            .Where(p => selectedPnms.Contains(p.pnm_id) && p.organization_id == user.organization_id)
            .ToListAsync();

        if (delete)
        {
            _context.PNMs.RemoveRange(pnms);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{pnms.Count} PNMs deleted.";
            return RedirectToAction("Index");
        }

        bool madeChanges = false;

        if (!string.IsNullOrWhiteSpace(newStatus))
        {
            pnms.ForEach(p => p.pnm_status = newStatus);
            madeChanges = true;
        }

        if (!string.IsNullOrWhiteSpace(newSemester))
        {
            pnms.ForEach(p => p.pnm_semester = newSemester);
            madeChanges = true;
        }

        if (madeChanges)
        {
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Changes applied successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "No changes were made. Please select a status or semester.";
        }

        return RedirectToAction("Index");
    }

    //Handles the form for checking into an event as a member, logging points as well
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckInToEvent(int eventId)
    {
        var username = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
        if (user == null) return Unauthorized();

        var eventExists = await _context.Events.AnyAsync(e => e.event_id == eventId);
        if (!eventExists)
        {
            TempData["ErrorMessage"] = "Event not found.";
            return RedirectToAction("Index");
        }

        var alreadyCheckedIn = await _context.MemberEventAttendances
            .AnyAsync(m => m.EventId == eventId && m.UserId == user.user_id);

        if (alreadyCheckedIn)
        {
            TempData["ErrorMessage"] = "You already checked in.";
            return RedirectToAction("Index");
        }

        var checkIn = new MemberEventAttendance
        {
            EventId = eventId,
            UserId = user.user_id,
            CheckedInAt = DateTime.UtcNow
        };
        _context.MemberEventAttendances.Add(checkIn);
        await _context.SaveChangesAsync();

        try
        {
            var pointsCategory = await _context.PointsCategories
                .FirstOrDefaultAsync(c => c.ActionName == "Attend an Event" && c.organization_id == user.organization_id);

            if (pointsCategory != null)
            {
                var pointLog = new UserPointLog
                {
                    UserID = user.user_id,
                    PointsCategoryID = pointsCategory.PointsCategoryID,
                    PointsAwarded = pointsCategory.PointsValue,
                    DateAwarded = DateTime.UtcNow
                };
                _context.UserPointLogs.Add(pointLog);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Points awarding failed: {ex}");
        }

        TempData["SuccessMessage"] = "Check-in successful!";
        return RedirectToAction("Index");
    }

    //Returns the view for the privacy policy
    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    //Returns the view for the Terms and conditions
    [AllowAnonymous]
    public IActionResult Terms()
    {
        return View();
    }




    //Logout
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("MyCookieAuth");
        return RedirectToAction("Login", "Login");
    }

    private string GetCurrentSemester()
    {
        var now = DateTime.UtcNow;
        return (now.Month <= 6 && !(now.Month == 6 && now.Day > 1))
            ? $"Spring {now.Year}"
            : $"Fall {now.Year}";
    }


}
