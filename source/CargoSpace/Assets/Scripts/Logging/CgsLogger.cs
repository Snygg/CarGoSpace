using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Logging
{
    public class CgsLogger
    {
        /// <summary>
        /// we wrap the default unity logger here in case we want to use a different one later
        /// </summary>
        private readonly Logger _logger = new Logger(Debug.unityLogger.logHandler);

        public CgsLogLevel LogLevel { get; private set; }
    
        public const CgsLogLevel DefaultLogLevel =
#if UNITY_EDITOR
                CgsLogLevel.Debug
#else
        CgsLogLevel.Info
#endif
            ;

        public CgsLogger(CgsLogLevel logLevelOverride = DefaultLogLevel)
        {
            LogLevel = logLevelOverride;
        }
        public void Log(
            CgsLogLevel logLevel, 
            string format, 
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName="",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile",
            params object[] values)
        {
            switch (logLevel)
            {
                case CgsLogLevel.None:
                    break;
                case CgsLogLevel.Warning:
                    LogWarning(format, context:context, callerMemberName:callerMemberName, callerLineNumber: callerLineNumber, callerFilePath:callerFilePath, values: values);
                    break;
                case CgsLogLevel.Info:
                    LogInformation(format, context:context, callerMemberName:callerMemberName, callerLineNumber: callerLineNumber, callerFilePath:callerFilePath, values: values);
                    break;
                case CgsLogLevel.Debug:
                    LogDebug(format, context:context, callerMemberName:callerMemberName, callerLineNumber: callerLineNumber, callerFilePath:callerFilePath, values: values);
                    break;
                case CgsLogLevel.Error:
                default:
                    LogError(format, context:context, callerMemberName:callerMemberName, callerLineNumber: callerLineNumber, callerFilePath:callerFilePath, values: values);
                    break;
            }
        }

        public void LogError(
            string format,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile",
            params object[] values)
        {
            LogFormat(LogType.Error,format, callerMemberName, callerLineNumber,callerFilePath, context:context, values: values);
        }
    
        /// <summary>
        /// 
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="context">optional. if this is a gameobject in the scene, the object will be highlighted in the editor</param>
        /// <param name="callerMemberName"></param>
        /// <param name="callerLineNumber"></param>
        public void LogError(
            Exception exception,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile")
        {
            if (LogLevel < CgsLogLevel.Error)
            {
                return;
            }
            _logger.LogException(new Exception($"{callerMemberName}:{callerFilePath}({callerLineNumber})",exception), context);
        }

        public void LogWarning(
            string format,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "unknownCaller",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile",
            params object[] values)
        {
            if (LogLevel < CgsLogLevel.Warning)
            {
                return;
            }
            LogFormat(LogType.Warning,format, callerMemberName, callerLineNumber,callerFilePath, context:context, values: values);
        }
    
        public void LogInformation(
            string format,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile",
            params object[] values)
        {
            if (LogLevel < CgsLogLevel.Info)
            {
                return;
            }
            LogFormat(LogType.Log,$"[Info]{format}", callerMemberName, callerLineNumber,callerFilePath, context:context, values: values);
        }
    
        public void LogDebug(
            string format,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile",
            params object[] values)
        {
            if (LogLevel < CgsLogLevel.Debug)
            {
                return;
            }
            LogFormat(LogType.Log,$"[Debug]{format}", callerMemberName, callerLineNumber,callerFilePath, context:context, values: values);
        }
        
        public void LogVerbose(
            string format,
            UnityEngine.Object context = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = -1,
            [CallerFilePath] string callerFilePath = "unknownFile",
            params object[] values)
        {
            if (LogLevel < CgsLogLevel.Verbose)
            {
                return;
            }
            LogFormat(LogType.Log,$"[Verbose]{format}", callerMemberName, callerLineNumber,callerFilePath, context:context, values: values);
        }

        private void LogFormat(
            LogType logType,
            string format,
            string callerMemberName,
            int callerLineNumber,
            string callerFilePath,
            UnityEngine.Object context = null,
            params object[] values)
        {
            try
            {
                _logger.LogFormat(
                    logType,
                    context, 
                    $"{format} \n {callerMemberName}:{callerFilePath}({callerLineNumber})" , 
                    values);
            }
            catch (FormatException)
            {
                _logger.LogFormat(
                    LogType.Error,
                    context,
                    $"Error in format: {format.Replace("{","-").Replace("}","-")}",values);
            }
        }
    }
}