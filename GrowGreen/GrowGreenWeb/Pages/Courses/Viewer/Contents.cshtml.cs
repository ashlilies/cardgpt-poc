using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GrowGreenWeb.Filters;
using GrowGreenWeb.Models;
using GrowGreenWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GrowGreenWeb.Pages.Courses.Viewer
{
    [Authenticated(AccountType.Learner)]
    public class ContentsModel : PageModel
    {
        public User Learner { get; set; } = null!;
        public Course Course { get; set; } = null!;
        public Lecture Lecture { get; set; } = null!;

        private readonly GrowGreenContext _context;
        private AccountService _accountService;

        public ContentsModel(GrowGreenContext context, AccountService accountService)
        {
            _context = context;
            _accountService = accountService;
        }

        public async Task<IActionResult> OnGetAsync(int courseId, int lectureId)
        {
            User? learner = _accountService.GetCurrentUser(HttpContext)!;
            _context.Attach(learner);
            Learner = learner;

            Course? course = await _context.Courses
                .Include(c => c.CourseSignups).ThenInclude(cs => cs.Learner)
                .Include(c => c.Lecturer)
                .SingleOrDefaultAsync(c => c.Id == courseId);

            if (course is null)
                return NotFound();

            Course = course;
            ViewData["CourseId"] = course.Id;

            // load lecture videos
            Lecture? lecture = await _context.Lectures
                .Include(l => l.Videos).ThenInclude(l => l.VideoCompletions)
                .ThenInclude(vc => vc.Learner)
                .SingleOrDefaultAsync(l => l.Id == lectureId);

            if (lecture is null)
                return NotFound();
            
            Lecture = lecture;

            return Page();
        }
    }
}