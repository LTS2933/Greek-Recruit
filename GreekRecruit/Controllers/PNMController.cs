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
using System.Net;
using System.Net.Mail;
using GreekRecruit.DTOs;

namespace GreekRecruit.Controllers
{
    [Authorize(AuthenticationSchemes = "MyCookieAuth")]
    public class PNMController : Controller
    {
        private readonly SqlDataContext _context;
        private readonly S3Service _s3Service;
        private readonly IConfiguration _configuration;
        private readonly OpenAIServiceWrapper _openAIService;

        public PNMController(SqlDataContext context, S3Service s3Service, IConfiguration configuration, OpenAIServiceWrapper openAIService  ) // add S3Service here
        {
            _context = context;
            _s3Service = s3Service;
            _configuration = configuration;
            _openAIService = openAIService;
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

            var pnmComments = comments.Select(c => c.comment_text).ToList();

            bool canShowSummaryButton = pnmComments.Count >= 5;

            ViewData["CanShowSummaryButton"] = canShowSummaryButton;
            ViewData["PNMComments"] = pnmComments;


            return View((pnm, comments, sessions));
        }

        //Get the AI summary for a PNM
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateSummary(int pnmId)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
            if (user == null) return Unauthorized();

            var pnm = await _context.PNMs.FirstOrDefaultAsync(p => p.pnm_id == pnmId);
            if (pnm == null)
            {
                TempData["ErrorMessage"] = "PNM ID not found.";
                return RedirectToAction("Index", "Home");
            }
            if (pnm.organization_id != user.organization_id)
            {
                TempData["ErrorMessage"] = "You do not have access to this PNM.";
                return RedirectToAction("Index", "Home");
            }

            var commentsQuery = _context.Comments.Where(c => c.pnm_id == pnmId);
            var commentCount = await commentsQuery.CountAsync();

            if (commentCount < 5)
            {
                TempData["ErrorMessage"] = "Not enough comments to generate a summary. At least 5 comments are required.";
                return RedirectToAction("Index", new { id = pnmId });
            }

            var allComments = await commentsQuery
                .Select(c => c.comment_text)
                .ToListAsync();

            var random = new Random();
            var selectedComments = allComments
                .OrderBy(x => random.Next())
                .Take(2)
                .ToList();

            var trimmedFname = pnm.pnm_fname.Trim().ToLower();
            var trimmedLname = pnm.pnm_lname.Trim().ToLower();

            var eventsAttended = await _context.EventsAttendance
                .Where(e => e.pnm_fname.Trim().ToLower() == trimmedFname
                    && e.pnm_lname.Trim().ToLower() == trimmedLname
                    && e.organization_id == pnm.organization_id)
                .CountAsync();

            try
            {
                var summary = await _openAIService.GeneratePNMSummaryAsync(selectedComments, eventsAttended);
                TempData["Summary"] = summary;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                TempData["ErrorMessage"] = "Something went wrong while generating the summary. Please try again.";
            }

            return RedirectToAction("Index", new { id = pnmId });
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

                if (string.IsNullOrWhiteSpace(comment.comment_text))
                {
                    TempData["ErrorMessage"] = "Comment cannot be empty. Please try again.";

                    return RedirectToAction("Index", new { id = pnm_id });
                }

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();


                try
                {
                    var commentPointsCategory = await _context.PointsCategories
                        .FirstOrDefaultAsync(c => c.ActionName == "Add a Comment on a PNM" && c.organization_id == user.organization_id);

                    if (commentPointsCategory != null)
                    {
                        var pointLog = new UserPointLog
                        {
                            UserID = user.user_id,
                            PointsCategoryID = commentPointsCategory.PointsCategoryID,
                            PointsAwarded = commentPointsCategory.PointsValue,
                            DateAwarded = DateTime.Now
                        };
                        _context.UserPointLogs.Add(pointLog);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Points awarding failed: {logEx}");
                }


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

            var pnm = await _context.PNMs.FirstOrDefaultAsync(p => p.pnm_id == pnm_id && p.organization_id == user.organization_id);
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

            var pnm = await _context.PNMs.FirstOrDefaultAsync(p => p.pnm_id == pnm_id && p.organization_id == user.organization_id);
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

            var pnm = await _context.PNMs.FirstOrDefaultAsync(p => p.pnm_id == pnm_id && p.organization_id == user.organization_id);
            if (pnm == null) return NotFound();


            try
            {
                // Validate file size
                if (newProfilePicture.Length > 5 * 1024 * 1024) // 5MB
                {
                    TempData["ErrorMessage"] = "Profile picture must be smaller than 5MB.";
                    return RedirectToAction("Index", new { id = pnm_id });
                }

                // Validate file extension
                var fileExtension = Path.GetExtension(newProfilePicture.FileName);
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                if (!allowedExtensions.Contains(fileExtension.ToLower()))
                {
                    TempData["ErrorMessage"] = "Only JPG and PNG images are allowed.";
                    return RedirectToAction("Index", new { id = pnm_id });
                }

                // Create a unique filename
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

            try
            {
                var votePointsCategory = await _context.PointsCategories
                    .FirstOrDefaultAsync(c => c.ActionName == "Vote on a PNM" && c.organization_id == user.organization_id);

                if (votePointsCategory != null)
                {
                    var pointLog = new UserPointLog
                    {
                        UserID = user.user_id,
                        PointsCategoryID = votePointsCategory.PointsCategoryID,
                        PointsAwarded = votePointsCategory.PointsValue,
                        DateAwarded = DateTime.Now
                    };
                    _context.UserPointLogs.Add(pointLog);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"Points awarding failed: {logEx}");
            }



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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkTexted(int pnm_id)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);
            if (user == null) return Unauthorized();

            var pnm = await _context.PNMs.FirstOrDefaultAsync(p => p.pnm_id == pnm_id && p.organization_id == user.organization_id);
            if (pnm == null) return NotFound();

            pnm.have_texted = "Yes";
            await _context.SaveChangesAsync();

            return Ok();

        }

        //Handles view for mass emailing
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MassEmail()
        {
            var curr_user_uname = User.Identity?.Name;
            var curr_user = await _context.Users.FirstOrDefaultAsync(u => u.username == curr_user_uname);
            if (curr_user == null) return Unauthorized();
            if (curr_user.role != "Admin") return Forbid();

            var pnms = await _context.PNMs
                .Where(p => p.organization_id == curr_user.organization_id) // Only show PNMs for the user's organization
                .OrderBy(p => p.pnm_fname)
                .ToListAsync();

            return View(pnms);
        }

        // Handles mass emailing to PNMs
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMassEmail([FromBody] MassEmailRequest request)
        {
            var curr_user_uname = User.Identity?.Name;
            var curr_user = await _context.Users.FirstOrDefaultAsync(u => u.username == curr_user_uname);
            if (curr_user == null) return Unauthorized();
            if (curr_user.role != "Admin") return Forbid();

            if (request == null || string.IsNullOrEmpty(request.Subject) || string.IsNullOrEmpty(request.Message) || request.Recipients == null || !request.Recipients.Any())
            {
                TempData["ErrorMessage"] = "You must include a subject, a message, and recipients to send to.";
                return RedirectToAction("MassEmail");
            }

            var organization = await _context.Organizations.FirstOrDefaultAsync(o => o.organization_id == curr_user.organization_id);
            if (organization == null || string.IsNullOrEmpty(organization.smtp_server) || string.IsNullOrEmpty(organization.smtp_username) || string.IsNullOrEmpty(organization.smtp_password))
            {
                TempData["ErrorMessage"] = "Organization email settings are not properly configured.";
                return RedirectToAction("MassEmail");
            }

            try
            {
                var mail = new MailMessage();
                mail.From = new MailAddress(organization.smtp_username);
                mail.Subject = request.Subject;
                mail.Body = request.Message;
                mail.IsBodyHtml = false;

                foreach (var email in request.Recipients)
                {
                    if (MailAddress.TryCreate(email, out _))
                    {
                        mail.Bcc.Add(email);
                    }
                }

                if (!mail.Bcc.Any())
                {
                    TempData["ErrorMessage"] = "No valid recipient emails were provided.";
                    return RedirectToAction("MassEmail");
                }

                string decryptedPassword;
                try
                {
                    decryptedPassword = AesEncryptionHelper.Decrypt(organization.smtp_password);
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Failed to decrypt email password: {ex.Message}";
                    return RedirectToAction("MassEmail");
                }

                var smtpClient = new SmtpClient(organization.smtp_server)
                {
                    Port = organization.smtp_port ?? 587,
                    Credentials = new NetworkCredential(organization.smtp_username, decryptedPassword), // <-- FIXED here
                    EnableSsl = true,
                };

                try
                {
                    await smtpClient.SendMailAsync(mail);
                    TempData["SuccessMessage"] = "Mass email sent to recipients!";
                    return Ok();
                }
                catch (Exception sendEx)
                {
                    TempData["ErrorMessage"] = $"Error sending mass email: {sendEx.Message}";
                    return RedirectToAction("MassEmail");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Unexpected error: {ex.Message}";
                return RedirectToAction("MassEmail");
            }
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
