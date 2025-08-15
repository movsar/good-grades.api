namespace GGLogsApi.ViewModels
{
    public class StatsViewModel
    {
        public Dictionary<string, int>? LastDay { get; set; }
        public Dictionary<string, int>? LastWeek { get; set; }
        public Dictionary<string, int>? LastMonth { get; set; }

        public Dictionary<string, int>? Period { get; set; } // for custom range
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
