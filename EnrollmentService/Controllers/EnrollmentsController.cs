using Microsoft.AspNetCore.Mvc;
using EnrollmentService.Data;
using EnrollmentService.Models;

namespace EnrollmentService.Controllers
{
    [ApiController]
    [Route("enroll")]
    public class EnrollmentsController : ControllerBase
    {
        private readonly EnrollmentDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public EnrollmentsController(EnrollmentDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        public async Task<IActionResult> Enroll([FromBody] EnrollmentRequest request)
        {
            var studentClient = _httpClientFactory.CreateClient("StudentService");
            var courseClient = _httpClientFactory.CreateClient("CourseService");

            var studentResponse = await studentClient.GetAsync($"/students/{request.StudentId}");
            if (!studentResponse.IsSuccessStatusCode)
                return NotFound($"Student with id {request.StudentId} does not exist.");

            var courseResponse = await courseClient.GetAsync($"/courses/{request.CourseId}");
            if (!courseResponse.IsSuccessStatusCode)
                return NotFound($"Course with id {request.CourseId} does not exist.");

            var enrollment = new Enrollment
            {
                StudentId = request.StudentId,
                CourseId = request.CourseId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return Ok(enrollment);
        }
    }
}