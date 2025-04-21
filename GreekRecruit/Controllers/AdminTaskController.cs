using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GreekRecruit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;

namespace GreekRecruit.Controllers
{
    public class AdminTaskController : Controller
    {
        private readonly SqlDataContext _context;

        public AdminTaskController(SqlDataContext context)
        {
            _context = context;
        }

        // Show all tasks (admin only)
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);

            if (user == null) return Unauthorized();
            if (user.role != "Admin") return Forbid();

            var tasks = await _context.AdminTasks
                .Where(t => t.organization_id == user.organization_id)
                .OrderByDescending(t => t.date_created)
                .ToListAsync();

            return View(tasks);
        }

        // Add new task
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTask(string title, string? description, DateTime? due_date)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);

            if (user == null) return Unauthorized();
            if (user.role != "Admin") return Forbid();

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["ErrorMessage"] = "Task title cannot be empty.";
                return RedirectToAction("Index");
            }

            var task = new AdminTask
            {
                title = title.Trim(),
                task_description = description?.Trim(),
                organization_id = user.organization_id,
                user_id = user.user_id,
                date_created = DateTime.Now,
                due_date = due_date
            };

            _context.AdminTasks.Add(task);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task added!";
            return RedirectToAction("Index");
        }


        // Mark task complete
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleComplete(int task_id)
        {

            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);

            if (user == null) return Unauthorized();
            if (user.role != "Admin") return Forbid();

            var task = await _context.AdminTasks.FindAsync(task_id);
            if (task == null) return NotFound();

            task.is_completed = !task.is_completed;
            task.date_completed = task.is_completed ? DateTime.Now : null;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        //Delete task
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int task_id)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);

            if (user == null) return Unauthorized();
            if (user.role != "Admin") return Forbid();

            var task = await _context.AdminTasks.FindAsync(task_id);
            if (task == null) return NotFound();

            _context.AdminTasks.Remove(task);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task deleted.";
            return RedirectToAction("Index");
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
