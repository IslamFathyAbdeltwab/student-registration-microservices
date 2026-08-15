
using EnrollmentService.Data;
using EnrollmentService.Messaging;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;

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

            

            // Retry policy: try 3 extra times with exponential backoff (1s, 2s, 4s)
            static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
            {
                return HttpPolicyExtensions
                    .HandleTransientHttpError() // handles 5xx and connection failures
                    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound == false && !msg.IsSuccessStatusCode)
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)));
            }

            // Circuit breaker: after 5 failures in a row, stop calling for 30 seconds
            static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
            {
                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
            }
            builder.Services.AddSingleton<EventPublisher>();
            builder.Services.AddHttpClient("StudentService", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["Services:StudentService"]!);
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

            builder.Services.AddHttpClient("CourseService", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["Services:CourseService"]!);
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

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
