using Microsoft.EntityFrameworkCore;

namespace GGLogsApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // MVC (controllers + views) and API controllers
            builder.Services.AddControllersWithViews();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            string? connectionString = builder.Configuration.GetConnectionString("Default");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("Connection string was empty");
            }

            builder.Services.AddDbContext<ApplicationContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            // Typed HTTP client for UI (not strictly needed now, but good for future)
            builder.Services.AddHttpClient();

            var app = builder.Build();

            // Swagger
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            // API routes
            app.MapControllers();

            // MVC default route (UI)
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=LogsUi}/{action=Index}/{id?}");

            // Ensure DB exists/migrates (schema unchanged)
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                context.Database.Migrate();
            }

            app.Run();
        }
    }
}
