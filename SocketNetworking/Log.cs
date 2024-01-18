using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking
{
    public static class Log
    {
        public static event Action<LogData> OnLog;

        private static readonly HashSet<LogSeverity> hiddenSeverities = new HashSet<LogSeverity>();

        public static void SetHiddenFlag(LogSeverity severity)
        {
            if(hiddenSeverities.Contains(severity))
            {
                return;
            }
            else
            {
                hiddenSeverities.Add(severity);
            }
        }

        public static void RemoveHiddenFlag(LogSeverity severity)
        {
            if (hiddenSeverities.Contains(severity))
            {
                hiddenSeverities.Remove(severity);
            }
            else
            {
                return;
            }
        }

        public static void Debug(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Debug,
                CallerType = GetCallerType(),
            };
            Invoke(data);
        }

        public static void Info(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Info,
                CallerType = GetCallerType(),
            };
            Invoke(data);
        }

        public static void Warning(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Warning,
                CallerType = GetCallerType(),
            };
            Invoke(data);
        }

        public static void Error(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Error,
                CallerType = GetCallerType(),
            };
            Invoke(data);
        }

        private static void Invoke(LogData data)
        {
            if(hiddenSeverities.Contains(data.Severity))
            {
                return;
            }
            else
            {
                OnLog?.Invoke(data);
            }
        }

        private static Type GetCallerType()
        {
            StackFrame frame = new StackFrame(2);
            var method = frame.GetMethod();
            var type = method.DeclaringType;
            return type;
        }
    }

    public struct LogData
    {
        public string Message;
        public LogSeverity Severity;
        public Type CallerType;
    }

    public enum LogSeverity
    {
        Debug,
        Info,
        Warning,
        Error
    }
}
