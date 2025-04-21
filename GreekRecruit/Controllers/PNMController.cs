using Microsoft.AspNetCore.Mvc;
using GreekRecruit.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GreekRecruit.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using GreekRecruit.Services;

namespace GreekRecruit.Controllers
{
    [Authorize(AuthenticationSchemes = "MyCookieAuth")]
    public class PNMController : Controller
    {
        private readonly SqlDataContext _context;
        private readonly S3Service _s3Service;

        public PNMController(SqlDataContext context, S3Service s3Service) // add S3Service here
        {
            _context = context;
            _s3Service = s3Service;
        }

        //Returns the View for the given PNM we are on
        [Authorize]
        public async Task<IActionResult> Index(int id)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
            if (user == null) return Unauthorized();
  
            var pnm = await _context.PNMs.FirstOrDefaultAsync(p => p.pnm_id == id);
            if (pnm == null || pnm.organization_id != user.organization_id)
            {
                TempData["ErrorMessage"] = "PNM ID not found.";
                return RedirectToAction("Index", "Home");
            }

            var comments = await _context.Comments
                .Where(c => c.pnm_id == id)
                .OrderByDescending(c => c.comment_dt)
                .ToListAsync();
            if (comments == null) return NotFound();

            var sessions = await _context.PNMVoteSessions
                .Where(s => s.pnm_id == id)
                .OrderByDescending(s => s.session_open_dt)
                .ToListAsync();

            if (!string.IsNullOrEmpty(pnm.pnm_profilepictureurl))
            {
                var s3Url = _s3Service.GetFileUrl(pnm.pnm_profilepictureurl);
                ViewData["S3ProfilePictureUrl"] = s3Url;
            }


            return View((pnm, comments, sessions));
        }

        //Submit a comment for a specific PNM
        [Authorize]
        [HttpPost("PNM/SubmitComment/{pnm_id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitComment(Comment comment, int pnm_id)
        {
            try
            {
                var username = User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);

                if (user == null) return Unauthorized();

                var pnm = await _context.PNMs.FirstOrDefaultAsync(p => p.pnm_id == pnm_id);
                if (pnm == null || pnm.organization_id != user.organization_id)
                {
                    TempData["ErrorMessage"] = "PNM ID not found.";
                    return RedirectToAction("Index", "Home");
                }

                comment.comment_dt = DateTime.Now;
                comment.pnm_id = pnm_id;
                comment.comment_author = username ?? "Unknown";
                comment.comment_author_name = user.full_name ?? "Unknown";

                if (string.IsNullOrEmpty(comment.comment_text))
                {
                    TempData["ErrorMessage"] = "Comment cannot be empty. Please try again.";

                    return RedirectToAction("Index", new { id = pnm_id });
                }

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Changes Saved.";
                return RedirectToAction("Index", new { id = pnm_id });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                TempData["ErrorMessage"] = "Something went wrong while submitting the comment. Please try again.";

                return RedirectToAction("Index", new { id = pnm_id });
            }
        }

        //Update the status of a PNM (accepted, denied, etc.) (Admin only)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStatus(IFormCollection form)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);

            if (user == null) return Unauthorized();
 

            string? pnm_status = form["pnm_status"];
            var pnm_id_string = form["pnm_id"];

            if (!int.TryParse(pnm_id_string, out int pnm_id))
            {
                TempData["ErrorMessage"] = "PNM ID not found.";
                return RedirectToAction("Index", "Home");
            }

            var pnm = await _context.PNMs.FindAsync(pnm_id);
            if (pnm == null) return NotFound();

            try
            {
                pnm.pnm_status = pnm_status;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Changes saved.";

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                TempData["ErrorMessage"] = "Error saving changes.";
            }
            return RedirectToAction("Index", new { id = pnm_id });
        }

        //Edit the PNM's info such as major, GPA, etc. (Admin only)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInfo(IFormCollection form)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);

            if (user == null) return Unauthorized();
            
            if (!int.TryParse(form["pnm_id"], out int pnm_id))
            {
                TempData["ErrorMessage"] = "PNM ID not found.";
                return RedirectToAction("Index", "Home");
            }

            var pnm = await _context.PNMs.FindAsync(pnm_id);
            if (pnm == null) return NotFound();

            try
            {
                pnm.pnm_email = form["pnm_email"];
                pnm.pnm_phone = form["pnm_phone"];
                if (double.TryParse(form["pnm_gpa"], out double parsedGpa))
                {
                    pnm.pnm_gpa = parsedGpa;
                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid GPA format. Please enter a valid number.";
                    return RedirectToAction("Index", new { id = pnm.pnm_id });
                }

                pnm.pnm_major = form["pnm_major"];
                pnm.pnm_schoolyear = form["pnm_schoolyear"];
                pnm.pnm_semester = form["pnm_semester"];


                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "PNM info updated successfully.";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                TempData["ErrorMessage"] = "Error updating PNM info.";
            }

            return RedirectToAction("Index", new { id = pnm_id });
        }


        //Upload new Profile Picture for PNM based on PNM ID (Admin only)
        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile newProfilePicture, int pnm_id)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);

            if (user == null) return Unauthorized();

            if (newProfilePicture == null || newProfilePicture.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid image.";
                return RedirectToAction("Index", new { id = pnm_id });
            }

            var pnm = await _context.PNMs.FindAsync(pnm_id);
            if (pnm == null) return NotFound();

            try
            {
                // Create a unique filename
                var fileExtension = Path.GetExtension(newProfilePicture.FileName);
                var fileName = $"pnm_{pnm_id}_{Guid.NewGuid()}{fileExtension}";

                // Upload file to S3
                await _s3Service.UploadFileAsync(newProfilePicture.OpenReadStream(), fileName, newProfilePicture.ContentType);

                // Optionally, store the filename (or URL) in your database
                pnm.pnm_profilepictureurl = fileName; // Assuming you create a URL field instead of byte array

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Profile picture updated successfully.";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                TempData["ErrorMessage"] = "Error updating profile picture.";
            }

            return RedirectToAction("Index", new { id = pnm_id });
        }
        // 6) Open a new voting session (Admin only)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OpenVoting(int pnm_id)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
            if (user == null) return Unauthorized();
            if (user.role != "Admin") return Forbid();

            var pnm = await _context.PNMs.FindAsync(pnm_id);
            if (pnm == null) return NotFound();

            var existingOpenSession = await _context.PNMVoteSessions
                .Where(v => v.pnm_id == pnm_id && v.voting_open_yn)
                .FirstOrDefaultAsync();
            if (existingOpenSession != null)
            {
                TempData["ErrorMessage"] = "There's already an active voting session...";
                return RedirectToAction("Index", new { id = pnm_id });
            }

            var newSession = new PNMVoteSession
            {
                pnm_id = pnm_id,
                voting_open_yn = true,
                yes_count = 0,
                no_count = 0
            };
            _context.PNMVoteSessions.Add(newSession);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Voting session opened!";
            return RedirectToAction("Index", new { id = pnm_id });
        }

        // 7) Close the current voting session (Admin only)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseVoting(int pnm_id)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
            if (user == null) return Unauthorized();
            if (user.role != "Admin") return Forbid();

            var pnm = await _context.PNMs.FindAsync(pnm_id);
            if (pnm == null) return NotFound();

            // Find the active session
            var currentSession = await _context.PNMVoteSessions
                .Where(v => v.pnm_id == pnm_id && v.voting_open_yn)
                .FirstOrDefaultAsync();
            if (currentSession == null)
            {
                TempData["ErrorMessage"] = "No active voting session found.";
            }
            else
            {
                currentSession.voting_open_yn = false;
                currentSession.session_close_dt = DateTime.Now;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Voting session closed!";
            }

            return RedirectToAction("Index", new { id = pnm_id });
        }

        // 8) Display a Vote page (for users only)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Vote(int pnm_id)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
            if (user == null) return Unauthorized();

            var pnm = await _context.PNMs.FindAsync(pnm_id);
            if (pnm == null) return NotFound();

            var currentSession = await _context.PNMVoteSessions
                .Where(s => s.pnm_id == pnm_id && s.voting_open_yn)
                .FirstOrDefaultAsync();

            if (currentSession == null)
            {
                return View("NoActiveSession", pnm);
            }

            if (DateTime.Now - currentSession.session_open_dt > TimeSpan.FromMinutes(20))
            {
                currentSession.voting_open_yn = false;
                currentSession.session_close_dt = DateTime.Now;
                await _context.SaveChangesAsync();

                return View("NoActiveSession", pnm);
            }

            return View((pnm, currentSession));
        }

        // 9) Submit the actual vote (users only)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitVote(int pnm_id, string voteValue)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
            if (user == null) return Unauthorized();

            var session = await _context.PNMVoteSessions
                .Where(s => s.pnm_id == pnm_id && s.voting_open_yn)
                .FirstOrDefaultAsync();

            if (session == null)
            {
                TempData["ErrorMessage"] = "No active voting session or it's already closed.";
                return RedirectToAction("Vote", new { pnm_id });
            }

            if (DateTime.Now - session.session_open_dt > TimeSpan.FromMinutes(20))
            {
                session.voting_open_yn = false;
                session.session_close_dt = DateTime.Now;
                await _context.SaveChangesAsync();

                TempData["ErrorMessage"] = "Session has expired (20 min limit).";
                return RedirectToAction("Vote", new { pnm_id });
            }

            // Check if the user already voted in this session
            bool alreadyVoted = await _context.PNMVoteTrackers
                .AnyAsync(t => t.vote_session_id == session.vote_session_id && t.user_id == user.user_id);

            if (alreadyVoted)
            {
                TempData["ErrorMessage"] = "You have already voted in this session.";
                return RedirectToAction("Vote", new { pnm_id });
            }

            // Record vote (tally only)
            if (voteValue == "Yes")
                session.yes_count++;
            else if (voteValue == "No")
                session.no_count++;
            else
            {
                TempData["ErrorMessage"] = "Invalid vote selection.";
                return RedirectToAction("Vote", new { pnm_id });
            }

            // Track vote
            var tracker = new PNMVoteTracker
            {
                vote_session_id = session.vote_session_id,
                user_id = user.user_id
            };
            _context.PNMVoteTrackers.Add(tracker);

            await _context.SaveChangesAsync();
            return RedirectToAction("Thankyou");
        }

        //Page that thanks users for casting their vote
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Thankyou()
        {
            return View();
        }

        //Page that allows users to type out a mass message and send it to PNMs of their choosing
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MassMessage()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
            if (user == null) return Unauthorized();
            if (user.role != "Admin") return Forbid();

            var pnms = await _context.PNMs
                .Where(p => p.organization_id == user.organization_id && !string.IsNullOrEmpty(p.pnm_phone))
                .OrderBy(p => p.pnm_lname)
                .ToListAsync();

            return View(pnms);
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
