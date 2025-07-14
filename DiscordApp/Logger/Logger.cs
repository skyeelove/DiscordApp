public static class Logger
{
    private static readonly object _lock = new();
    private static readonly string _logFilePath = Path.Combine("logs", "app.log");

    private static void Log(string message)
    {
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm}]  {message}";

        File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
        Console.WriteLine(logEntry);
    }

    public static void Error(string message)
    {
        Log("[ERROR]  " + message);
    }

    public static void Warning(string message)
    {
        Log("[WARNING]   " + message);
    }

    public static void Info(string message)
    {
        Log("[INFO]  " + message);
    }

    public static void Debug(string message)
    {
        Log("[DEBUG]  " + message);
    }
}
