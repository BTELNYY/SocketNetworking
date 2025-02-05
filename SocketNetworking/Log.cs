using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking
{
    public class Log
    {
        public static event Action<LogData> OnLog;

        private static readonly HashSet<LogSeverity> hiddenSeverities = new HashSet<LogSeverity>();

        private static Log _instance;

        public static Log GetInstance()
        {
            if(_instance != null)
            {
                return _instance;
            }
            else
            {
                _instance = new Log();
                return _instance;
            }
        }

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

        public static void GlobalDebug(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Debug,
                CallerType = GetCallerType(),
            };
            Invoke(data);
        }

        public static void GlobalSuccess(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Success,
                CallerType = GetCallerType(),
            };
            Invoke(data);
        }

        public void Debug(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Debug,
                CallerType = GetCallerType(),
            };
            InvokeInstance(data);
        }

        public void Success(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Success,
                CallerType = GetCallerType(),
            };
            InvokeInstance(data);
        }

        public static void GlobalInfo(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Info,
                CallerType = GetCallerType(),
            };
            Invoke(data);
        }

        public void Info(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Info,
                CallerType = GetCallerType(),
            };
            InvokeInstance(data);
        }

        public static void GlobalWarning(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Warning,
                CallerType = GetCallerType(),
            };
            Invoke(data);
        }


        public Log(string prefix)
        {
            Prefix = prefix;
        }

        public Log() { }

        public string Prefix { get; set; } = "[Network]";

        public void Warning(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Warning,
                CallerType = GetCallerType(),
            };
            InvokeInstance(data);
        }

        public static void GlobalError(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Error,
                CallerType = GetCallerType(),
            };
            Invoke(data);
        }

        public void Error(string message)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = LogSeverity.Error,
                CallerType = GetCallerType(),
            };
            InvokeInstance(data);
        }

        private void InvokeInstance(LogData data)
        {
            data.Message = Prefix + ": " + data.Message;
            if (hiddenSeverities.Contains(data.Severity))
            {
                return;
            }
            else
            {
                OnLog?.Invoke(data);
            }
        }

        public static void GlobalAny(string message, LogSeverity severity)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = severity,
                CallerType = GetCallerType(),
            };
            Invoke(data);
        }

        public void Any(string message, LogSeverity severity)
        {
            LogData data = new LogData()
            {
                Message = message,
                Severity = severity,
                CallerType = GetCallerType(),
            };
            InvokeInstance(data);
        }

        private static void Invoke(LogData data)
        {
            data.Message = GetInstance().Prefix + ": " + data.Message;
            if (hiddenSeverities.Contains(data.Severity))
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
        Success,
        Warning,
        Error
    }
}
