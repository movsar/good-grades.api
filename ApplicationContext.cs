using Microsoft.EntityFrameworkCore;

namespace GGLogsApi
{
    public class ApplicationContext : DbContext
    {
        public DbSet<LogMessage> LogMessages { get; set; } = null!;
        public ApplicationContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=gglogsdatabase.db");
        }

    }
}
