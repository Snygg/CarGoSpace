using Godot;
using System.Runtime.CompilerServices;

namespace CargoSpace.Core
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public static class GameLogger
    {
        public static void Log(LogLevel level, string message, [CallerFilePath] string filePath = "")
        {
            string className = ExtractClassName(filePath);
            string fullMessage = string.IsNullOrEmpty(className) ? message : $"[{className}] {message}";
            
            switch (level)
            {
                case LogLevel.Debug:
                    if (Constants.EnableDebugLogging)
                        GD.Print(fullMessage);
                    break;
                case LogLevel.Info:
                    GD.Print(fullMessage);
                    break;
                case LogLevel.Warning:
                    GD.Print(fullMessage);
                    break;
                case LogLevel.Error:
                    GD.PrintErr(fullMessage);
                    break;
            }
        }

        public static void Debug(string message, [CallerFilePath] string filePath = "")
        {
            Log(LogLevel.Debug, message, filePath);
        }

        public static void Info(string message, [CallerFilePath] string filePath = "")
        {
            Log(LogLevel.Info, message, filePath);
        }

        public static void Warning(string message, [CallerFilePath] string filePath = "")
        {
            Log(LogLevel.Warning, message, filePath);
        }

        public static void Error(string message, [CallerFilePath] string filePath = "")
        {
            Log(LogLevel.Error, message, filePath);
        }

        private static string ExtractClassName(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "";

            // Get the file name without extension
            string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            return fileName;
        }
    }
}
