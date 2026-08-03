
using EnrollmentService.Data;
using Microsoft.EntityFrameworkCore;

namespace EnrollmentService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddDbContext<EnrollmentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHttpClient("StudentService", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["Services:StudentService"]!);
            });

            builder.Services.AddHttpClient("CourseService", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["Services:CourseService"]!);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EnrollmentDbContext>();
                db.Database.Migrate();
            }
            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
