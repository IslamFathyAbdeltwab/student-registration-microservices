using Microsoft.EntityFrameworkCore;
using CourseService.Models;

namespace CourseService.Data
{
    public class CourseDbContext : DbContext
    {
        public CourseDbContext(DbContextOptions<CourseDbContext> options) : base(options) { }

        public DbSet<Course> Courses { get; set; }
    }
}