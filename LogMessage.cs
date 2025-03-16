using System.ComponentModel.DataAnnotations;

namespace GGLogsApi
{
    public class LogMessage
    {
        [Key]
        public long Id { get; set; }

        public string? StackTrace { get; set; }
        public string Message { get; set; } = null!;
        public int Level { get; set; } = (int)LogLevel.Information;

        public string WindowsVersion { get; set; } = null!;
        public string SystemDetails { get; set; } = null!;
        public string ProgramVersion { get; set; } = null!;
        public string ProgramName { get; set; } = null!;
        public string CreatedAt { get; set; } = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
    }
}
