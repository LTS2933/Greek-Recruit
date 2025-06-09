using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GreekRecruit.Models;
using Microsoft.AspNetCore.Authentication;

namespace GreekRecruit.Controllers;

public class DashboardController : Controller
{
    private readonly SqlDataContext _context;

    public DashboardController(SqlDataContext context)
    {
        _context = context;
    }

    //Shows the view for the stats and insights page
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var username = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
        if (user == null) return Unauthorized();

        var orgId = user.organization_id;
        var currentSemester = GetCurrentSemester();

        var totalPnms = await _context.PNMs
            .Where(p => p.organization_id == orgId && p.pnm_semester == currentSemester)
            .CountAsync();

        var statusCounts = await _context.PNMs
            .Where(p => p.organization_id == orgId && p.pnm_semester == currentSemester)
            .GroupBy(p => p.pnm_status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var statusDict = statusCounts.ToDictionary(k => k.Status ?? "Unknown", v => v.Count);

        var averageGpa = await _context.PNMs
            .Where(p => p.organization_id == orgId && p.pnm_semester == currentSemester && p.pnm_gpa.HasValue)
            .AverageAsync(p => p.pnm_gpa.Value);

        DateTime semesterStart, semesterEnd;
        var now = DateTime.UtcNow;
        if (currentSemester.StartsWith("Spring"))
        {
            semesterStart = new DateTime(now.Year, 1, 1);
            semesterEnd = new DateTime(now.Year, 6, 1); // Inclusive of June 1
        }
        else
        {
            semesterStart = new DateTime(now.Year, 6, 2);
            semesterEnd = new DateTime(now.Year, 12, 31);
        }

        var totalEvents = await _context.Events
            .Where(e => e.organization_id == orgId &&
                        e.event_datetime >= semesterStart &&
                        e.event_datetime <= semesterEnd)
            .CountAsync();


        var recentSessions = await _context.PNMVoteSessions
            .Where(v => _context.PNMs.Any(p => p.pnm_id == v.pnm_id && p.organization_id == orgId && p.pnm_semester == currentSemester))
            .OrderByDescending(v => v.session_open_dt)
            .Take(5)
            .ToListAsync();

        var mostRecentVoted = recentSessions.Select(session => new {
            Session = session,
            PNM = _context.PNMs.FirstOrDefault(p => p.pnm_id == session.pnm_id)
        }).ToList();


        var topAttendees = await _context.EventsAttendance
            .Where(e => e.organization_id == orgId)
            .GroupBy(e => new { e.pnm_fname, e.pnm_lname })
            .Select(g => new {
                pnm_fname = g.Key.pnm_fname,
                pnm_lname = g.Key.pnm_lname,
                EventCount = g.Count()
            })
            .OrderByDescending(g => g.EventCount)
            .Take(5)
            .ToListAsync();

        var topCommenters = await _context.Comments
            .Where(c => _context.PNMs.Any(p => p.pnm_id == c.pnm_id && p.organization_id == orgId && p.pnm_semester == currentSemester))
            .GroupBy(c => c.comment_author_name)
            .Select(g => new {
                Name = g.Key,
                CommentCount = g.Count()
            })
            .OrderByDescending(g => g.CommentCount)
            .Take(5)
            .ToListAsync();

        // 1. Get all relevant users for the org
        var users = await _context.Users
            .Where(u => u.organization_id == orgId)
            .ToListAsync();

        // 2. Get all logs for the org users
        var userIds = users.Select(u => u.user_id).ToList();

        var logs = await _context.UserPointLogs
            .Where(l => userIds.Contains(l.UserID))
            .ToListAsync();

        // 3. Filter logs for current semester (C# side)
        var semesterLogs = logs
            .Where(l => GetSemesterFromDate(l.DateAwarded) == currentSemester)
            .ToList();

        // 4. Get all point categories for the org
        var pointsCategories = await _context.PointsCategories
            .Where(pc => pc.organization_id == orgId)
            .ToListAsync();

        // Build a lookup to avoid repeated LINQ queries
        var categoryLookup = pointsCategories.ToDictionary(c => c.PointsCategoryID, c => c.PointsValue);
        var userLookup = users.ToDictionary(u => u.user_id, u => u.username);

        // 5. Calculate total points per user
        var userPoints = semesterLogs
            .GroupBy(l => l.UserID)
            .Select(g =>
            {
                var totalPoints = g.Sum(log =>
                {
                    var category = pointsCategories.FirstOrDefault(c => c.PointsCategoryID == log.PointsCategoryID);
                    return category?.PointsValue ?? 0;
                });

                return new
                {
                    FullName = userLookup.ContainsKey(g.Key) ? userLookup[g.Key] : "Unknown",
                    TotalPoints = totalPoints
                };

            })
            .OrderByDescending(x => x.TotalPoints)
            .ToList();



        ViewData["TopAttendees"] = topAttendees;
        ViewData["TopCommenters"] = topCommenters;
        ViewData["StatusCounts"] = statusDict;
        ViewData["TotalPNMs"] = totalPnms;
        ViewData["AvgGPA"] = averageGpa;
        ViewData["TotalEvents"] = totalEvents;
        ViewData["RecentVotes"] = mostRecentVoted;
        ViewData["CurrentSemester"] = GetCurrentSemester();
        ViewData["UserPoints"] = userPoints;

        return View();
    }

    private string GetCurrentSemester()
    {
        var now = DateTime.UtcNow;
        return (now.Month <= 6 && !(now.Month == 6 && now.Day > 1))
            ? $"Spring {now.Year}"
            : $"Fall {now.Year}";
    }

    private string GetSemesterFromDate(DateTime date)
    {
        return (date.Month <= 6 && !(date.Month == 6 && date.Day > 1))
            ? $"Spring {date.Year}"
            : $"Fall {date.Year}";
    }

    //Logout
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("MyCookieAuth");
        return RedirectToAction("Login", "Login");
    }
}
