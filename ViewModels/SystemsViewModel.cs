namespace GGLogsApi.ViewModels
{
    public class SystemsViewModel
    {
        public List<SystemRow> Items { get; set; } = new();

        public class SystemRow
        {
            public string SystemDetails { get; set; } = "";
            public int Count { get; set; }
            public DateTime LastLogAt { get; set; }
        }
    }
}
