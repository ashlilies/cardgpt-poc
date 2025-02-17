using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using GrowGreenWeb.Filters;
using GrowGreenWeb.Models;
using GrowGreenWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GrowGreenWeb.Pages.Lecturer.Courses.Manage
{
    [Authenticated(AccountType.Lecturer)]
    public class IndexModel : PageModel
    {
        public Course Course { get; set; } = null!; // for sidebar
        public int CourseId { get; set; }

        [BindProperty, Required, MaxLength(100), DisplayName("Title")]
        public string Name { get; set; } = null!;

        [BindProperty, Required, MaxLength(1000)]
        public string Description { get; set; } = null!;

        [BindProperty, Required, DisplayName("Starting Availability Date"), DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [BindProperty, Required, DisplayName("Ending Availability Date"), DataType(DataType.Date)]

        public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1);

        [BindProperty, Required, DisplayName("Max Capacity"), Range(1, 10000)]
        public int MaxCapacity { get; set; } = 100;

        [BindProperty, DisplayName("New course thumbnail (jpg/jpeg/png)")]
        public IFormFile? Upload { get; set; }

        [BindProperty]
        public string? ImageUrl { get; set; }

        private readonly GrowGreenContext _context;

        private readonly IWebHostEnvironment _environment;
        private AccountService _accountService;

        public IndexModel(GrowGreenContext context, IWebHostEnvironment environment, AccountService accountService)
        {
            _context = context;
            _environment = environment;
            _accountService = accountService;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            User? user = _accountService.GetCurrentUser(HttpContext);
if (user == null)
    return Page();

int lecturerId = user.Id;

            Course? course = await _context.Courses
                .Include(c => c.Lectures)
                .SingleOrDefaultAsync(c => c.Id == id);
            if (course is null)
                return NotFound();

            if (course.LecturerId != lecturerId)
                return Forbid();

            Course = course;
            ViewData["CourseId"] = course.Id;

            CourseId = course.Id;
            Name = course.Name;
            Description = course.Description;
            StartDate = course.StartDate;
            EndDate = course.EndDate;
            MaxCapacity = course.MaxCapacity;

            if (ImageUrl == null) // handle update image case
                ImageUrl = course.ImageUrl;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            User? user = _accountService.GetCurrentUser(HttpContext);
if (user == null)
    return Page();

int lecturerId = user.Id;

            if (EndDate <= StartDate)
            {
                ModelState.AddModelError(nameof(EndDate), "End date cannot be before start date");
            }

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Description))
            {
                TempData["FlashMessage.Type"] = "danger";
                TempData["FlashMessage.Text"] = "There are one or more errors. Please correct and try again.";

                return await OnGetAsync(id);
            }

            Course? course = await _context.Courses.FindAsync(id);
            if (course is null)
                return NotFound();

            if (course.LecturerId != lecturerId)
                return Forbid();

            // check if another course with a similar name already exists
            Course? check = _context.Courses.SingleOrDefault(c => c.Name == Name && c.Id != course.Id);
            if (check != null)
            {
                TempData["FlashMessage.Type"] = "danger";
                TempData["FlashMessage.Text"] = "Another course with a similar name already exists.";

                return await OnGetAsync(id);
            }

            // update course details
            course.Name = Name;
            course.Description = Description;
            course.StartDate = StartDate;
            course.EndDate = EndDate;
            course.ImageUrl = ImageUrl;
            course.MaxCapacity = MaxCapacity;

            _context.Update(course);
            course.LastUpdatedTimestamp = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["FlashMessage.Type"] = "success";
            TempData["FlashMessage.Text"] = "Successfully updated course details.";

            return await OnGetAsync(id);
        }

        public async Task<IActionResult> OnPostUploadAsync(int id)
        {
            User? user = _accountService.GetCurrentUser(HttpContext);
if (user == null)
    return Page();

int lecturerId = user.Id;

            ModelState.Clear();

            if (Upload is null)
            {
                TempData["FlashMessage.Type"] = "danger";
                TempData["FlashMessage.Text"] = "Error uploading image";
                return await OnGetAsync(id);
            }

            if (!Constants.AllowedImageExtensions.Contains(Path.GetExtension(Upload.FileName)))
            {
                TempData["FlashMessage.Type"] = "danger";
                TempData["FlashMessage.Text"] = "Image file type not allowed!";
                return await OnGetAsync(id);
            }

            string random = Guid.NewGuid().ToString();
            string webRootPath = "/uploads/courseThumbnail/" + random + "-" + Upload.FileName;
            var file = Path.Combine(_environment.WebRootPath, "uploads", "courseThumbnail", random + "-" + Upload.FileName);
            await using (var fileStream = new FileStream(file, FileMode.Create))
            {
                await Upload.CopyToAsync(fileStream);
            }

            ImageUrl = webRootPath;

            // update image in db
            Course? course = await _context.Courses.FindAsync(id);
            if (course is null)
                return NotFound();

            if (course.LecturerId != lecturerId)
                return Forbid();

            course.ImageUrl = ImageUrl;
            course.LastUpdatedTimestamp = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["FlashMessage.Type"] = "success";
            TempData["FlashMessage.Text"] = "Successfully updated image";

            return await OnGetAsync(id);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            User? user = _accountService.GetCurrentUser(HttpContext);
if (user == null)
    return Page();

int lecturerId = user.Id;

            Course? course = await _context.Courses.FindAsync(id);
            if (course is null)
                return NotFound();

            if (course.LecturerId != lecturerId)
                return Forbid();

            // delete course
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            TempData["FlashMessage.Type"] = "success";
            TempData["FlashMessage.Text"] = "Successfully deleted course.";

            return RedirectToPage("../Index");
        }
    }
}