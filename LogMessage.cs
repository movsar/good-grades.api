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

        // DateTime тип вообще работает, но решил использовать string,
        // потому что в конце еще добавлялись дополнителье единицы
        // измерения времени, например: 10.12.2024 10:45:33.234234234
        // Поэтому перевел в строку, чтобы переделать как нужно
        public string CreatedAt { get; set; }

        // Для инициализации даты использовал конструктор
        public LogMessage()
        {
            CreatedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        }

    }
}
