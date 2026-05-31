using System.Diagnostics;

namespace GoblinFarmer
{
    internal static class AppLogger
    {
        private static readonly object SyncRoot = new();
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly string LogFilePath = Path.Combine(LogDirectory, $"GoblinFarmer_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Error(string message, Exception? ex = null)
        {
            Write("ERROR", ex == null ? message : $"{message}{Environment.NewLine}{ex}");
        }

        private static void Write(string level, string message)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
            Debug.WriteLine(line);

            lock (SyncRoot)
            {
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
        }
    }
}
