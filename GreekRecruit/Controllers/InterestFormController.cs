﻿using Microsoft.AspNetCore.Mvc;
using GreekRecruit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GreekRecruit.Services;
using Microsoft.AspNetCore.Authentication;


namespace GreekRecruit.Controllers;

public class InterestFormController : Controller
{
    private readonly SqlDataContext _context;
    private readonly S3Service _s3Service;

    public InterestFormController(SqlDataContext context, S3Service s3Service)
    {
        _context = context;
        _s3Service = s3Service;
    }

    // View all Interest Forms (Admin)
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var username = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);

        if (user == null ) return Unauthorized();

        var forms = await _context.InterestForms
            .Where(f => f.organization_id == user.organization_id)
            .OrderByDescending(f => f.date_created)
            .ToListAsync();

        return View(forms);
    }

    // The view for creating a new Interest Form (Admin)
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Create()
    {

        var username = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);

        if (user == null) return Unauthorized();
        if (user.role != "Admin") return Forbid();

        return View();

    }

    // Handle form submission for creating a new Interest Form
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string form_name)
    {
        var username = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);

        if (user == null) return Unauthorized();
        if (user.role != "Admin") return Forbid();

        if (string.IsNullOrWhiteSpace(form_name))
        {
            TempData["ErrorMessage"] = "Form name cannot be empty.";
            return RedirectToAction("Create");
        }

        var form = new InterestForm
        {
            form_name = form_name.Trim(),
            organization_id = user.organization_id,
            date_created = DateTime.UtcNow
        };

        _context.InterestForms.Add(form);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Form '{form.form_name}' created!";
        return RedirectToAction("Index");
    }

    // View submissions for a specific form (Admin)
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Submissions(int form_id)
    {
        var username = User.Identity?.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.username == username);

        if (user == null) return Unauthorized();

        var form = await _context.InterestForms
            .FirstOrDefaultAsync(f => f.form_id == form_id && f.organization_id == user.organization_id);

        if (form == null)
            return NotFound();

        var submissions = await _context.InterestFormSubmissions
            .Where(s => s.form_id == form_id)
            .OrderByDescending(s => s.date_submitted)
            .ToListAsync();

        ViewData["FormName"] = form.form_name;
        return View(submissions);
    }

    // Public Submission View (Accessible to anyone)
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> SubmitView(int form_id)
    {
        var form = await _context.InterestForms.FirstOrDefaultAsync(f => f.form_id == form_id);
        if (form == null)
            return NotFound("Form does not exist.");

        ViewData["FormName"] = form.form_name;
        ViewData["FormId"] = form_id;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitForm(int form_id, InterestFormSubmission submission, IFormFile? pnm_profilepicture)
    {
        var form = await _context.InterestForms.FirstOrDefaultAsync(f => f.form_id == form_id);
        if (form == null)
            return NotFound("Form does not exist.");

        if (!ModelState.IsValid)
        {
            ViewData["FormName"] = form.form_name;
            ViewData["FormId"] = form_id;
            TempData["ErrorMessage"] = "Please fill out all required fields.";
            return View(submission);
        }

        string? fileName = null;
        if (pnm_profilepicture != null && pnm_profilepicture.Length > 0)
        {
            if (pnm_profilepicture.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Picture must be smaller than 5MB.";
                return RedirectToAction("SubmitView", new { form_id = form_id });
            }

            var extension = Path.GetExtension(pnm_profilepicture.FileName);
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            if (!allowedExtensions.Contains(extension.ToLower()))
            {
                TempData["ErrorMessage"] = "Only JPG and PNG images are allowed.";
                return RedirectToAction("SubmitView", new { form_id = form_id });
            }

            // All good, upload
            fileName = $"pnm_{Guid.NewGuid()}{extension}";
            await _s3Service.UploadFileAsync(pnm_profilepicture.OpenReadStream(), fileName, pnm_profilepicture.ContentType);
        }


        submission.form_id = form_id;
        submission.organization_id = form.organization_id;
        submission.date_submitted = DateTime.UtcNow;
        submission.pnm_profilepictureurl = fileName;

        _context.InterestFormSubmissions.Add(submission);

        var pnm = new PNM
        {
            organization_id = form.organization_id,
            pnm_fname = submission.pnm_fname,
            pnm_lname = submission.pnm_lname,
            pnm_email = submission.pnm_email,
            pnm_phone = submission.pnm_phone,
            pnm_schoolyear = submission.pnm_schoolyear,
            pnm_major = submission.pnm_major,
            pnm_gpa = submission.pnm_gpa,
            pnm_profilepictureurl = fileName,
            pnm_instagramhandle = submission.pnm_instagramhandle,
            pnm_semester = GetCurrentSemester(),
            pnm_dateadded = DateTime.UtcNow
        };

        _context.PNMs.Add(pnm);
        await _context.SaveChangesAsync();

        return RedirectToAction("ThankYou");
    }



    [AllowAnonymous]
    [HttpGet]
    public IActionResult ThankYou()
    {
        return View();
    }

    private string GetCurrentSemester()
    {
        var now = DateTime.UtcNow;
        return (now.Month <= 6 && !(now.Month == 6 && now.Day > 1))
            ? $"Spring {now.Year}"
            : $"Fall {now.Year}";
    }

    //Logout
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("MyCookieAuth");
        return RedirectToAction("Login", "Login");
    }
}


//LOOK BACK AT THIS CONTROLLER AND RENAME METHODS