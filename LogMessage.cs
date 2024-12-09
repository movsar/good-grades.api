namespace GGLogsApi
{
    public class LogMessage
    {
        public long Id { get; set; }
        
        // 0 - информация(потом сделаем)
        // 1 - предупреждение(потом сделаем)
        // 2 - ошибка(т.е.сейчас всегда ставь 2)
        public int Type { get; set; } = 2;
        public string Message { get; set; } = null!;
        public string? Details { get; set; }
        public string CreatedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
    }
}
