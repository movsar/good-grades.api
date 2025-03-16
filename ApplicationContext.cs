using Microsoft.EntityFrameworkCore;
namespace GGLogsApi
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }
        public DbSet<LogMessage> LogMessages { get; set; } = null!;
    }
}
