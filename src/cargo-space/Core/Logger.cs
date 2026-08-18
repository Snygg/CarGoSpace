using Godot;
using System.Runtime.CompilerServices;

namespace CargoSpace.Core
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    public static class GameLogger
    {
        public static void Log(LogLevel level, string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
        {
            // Only log if level is at or above minimum level
            if ((int)level < Constants.MinimumLogLevel)
                return;

            string className = ExtractClassName(filePath);
            string contextPrefix = GetContextPrefix();
            string methodPrefix = string.IsNullOrEmpty(memberName) ? className : $"{className}::{memberName}";
            string fullMessage = $"[{contextPrefix}] [{methodPrefix}] {message}";
            
            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    GD.Print(fullMessage);
                    break;
                case LogLevel.Warning:
                    GD.PushWarning(fullMessage);
                    break;
                case LogLevel.Error:
                    GD.PushError(fullMessage);
                    break;
            }
        }

        public static void Debug(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
        {
            Log(LogLevel.Debug, message, filePath, memberName);
        }

        public static void Info(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
        {
            Log(LogLevel.Info, message, filePath, memberName);
        }

        public static void Warning(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
        {
            Log(LogLevel.Warning, message, filePath, memberName);
        }

        public static void Error(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
        {
            Log(LogLevel.Error, message, filePath, memberName);
        }

        private static string ExtractClassName(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "";

            // Get the file name without extension
            string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            return fileName;
        }

        private static string GetContextPrefix()
        {
            // Check if running as dedicated server
            if (OS.HasFeature("dedicated_server"))
                return "SERVER";
            
            // Check command line args for --server flag
            string[] args = OS.GetCmdlineArgs();
            if (((System.Collections.Generic.ICollection<string>)args).Contains("--server"))
                return "SERVER";
            
            return "CLIENT";
        }
    }
}
