
using Microsoft.EntityFrameworkCore;
using StudentService.Data;

namespace StudentService
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
            builder.Services.AddDbContext<StudentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<StudentDbContext>();
                try
                {
                    db.Database.Migrate();
                }
                catch (Exception ex)
                {
                    // Another instance already created/migrated the DB — safe to ignore
                    Console.WriteLine($"Migration skipped (likely already applied by another instance): {ex.Message}");
                }
            }
            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();
            app.MapGet("/health", () => Results.Ok("Healthy"));

            app.Run();
        }
    }
}
