using GGLogsApi.Models;

namespace GGLogsApi.ViewModels
{
    public class PagedLogsViewModel
    {
        public List<LogMessage> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public LogsFilter Filter { get; set; } = new();

        public int TotalPages => (int)Math.Ceiling((double)Total / Math.Max(1, PageSize));
    }

}
