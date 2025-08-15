namespace GGLogsApi.Models
{
    public class LogsFilter
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 100;

        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? Level { get; set; }
        public string? ProgramName { get; set; }
        public string? ProgramVersion { get; set; }
        public string? WindowsVersion { get; set; }
        public string? SystemDetails { get; set; }
        public bool? HasStackTrace { get; set; }
        public string? Search { get; set; }
        public string SortBy { get; set; } = "CreatedAt";
        public string SortDir { get; set; } = "desc";
    }
}