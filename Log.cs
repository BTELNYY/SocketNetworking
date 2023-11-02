using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking
{
    public static class Log
    {
        public static Action<LogData> OnLog;

        public static void Debug(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Debug
            };
            OnLog?.Invoke(data);
        }

        public static void Info(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Info
            };
            OnLog?.Invoke(data);
        }

        public static void Warning(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Warning
            };
            OnLog?.Invoke(data);
        }

        public static void Error(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Error
            };
            OnLog?.Invoke(data);
        }
    }

    public struct LogData
    {
        public string Message;
        public LogSeverity Severity;
    }

    public enum LogSeverity
    {
        Debug,
        Info,
        Warning,
        Error
    }
}
