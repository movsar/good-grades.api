
using Microsoft.EntityFrameworkCore;
using System;

namespace GGLogsApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            
            string connectionString = builder.Configuration.GetConnectionString("Default");

            builder.Services.AddDbContext<ApplicationContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            using (var Scope = app.Services.CreateScope())
            {
                var context = Scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                context.Database.Migrate();
            }
            app.Run();

        }
    }
}
