using System;
using System.Diagnostics;

namespace SocketNetworking
{
    /// <summary>
    /// The <see cref="Log"/> class provides a implementation to store messages.
    /// </summary>
    public class Log
    {
        /// <summary>
        /// Called when a new <see cref="LogData"/> entry is created. Note that this is called by several threads, and therefore isn't thread safe.
        /// </summary>
        public static event Action<LogData> OnLog;

        //what the fuck
        //private static readonly HashSet<LogSeverity> hiddenSeverities = new HashSet<LogSeverity>();

        /// <summary>
        /// Current log level.
        /// </summary>
        public static LogSeverity Levels = DEFAULT_LOG;

        /// <summary>
        /// The default log configuration. Everything but <see cref="LogSeverity.Debug"/> is logged.
        /// </summary>
        public const LogSeverity DEFAULT_LOG = (LogSeverity)30;

        /// <summary>
        /// The <see cref="FULL_LOG"/> const allows ALL logs to be shown.
        /// </summary>
        public const LogSeverity FULL_LOG = (LogSeverity)31;

        /// <summary>
        /// The <see cref="NO_LOG"/> const hides ALL logs.
        /// </summary>
        public const LogSeverity NO_LOG = 0;

        /// <summary>
        /// Enabling this will print stack traces after every log message.
        /// </summary>
        public static bool ShowStackTrace = false;

        private static Log _instance;

        /// <summary>
        /// Singleton of the <see cref="Log"/> class.
        /// </summary>
        /// <returns></returns>
        public static Log GetInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }
            else
            {
                _instance = new Log();
                return _instance;
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

        /// <summary>
        /// What is displayed in the message before the message content.
        /// </summary>
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
            if (!Levels.HasFlag(data.Severity))
            {
                return;
            }
            else
            {
                if (ShowStackTrace)
                {
                    data.Message += $"\nStack Trace:\n{GetStackTrace().ToString()}";
                }
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
            if (!Levels.HasFlag(data.Severity))
            {
                return;
            }
            else
            {
                if (ShowStackTrace)
                {
                    data.Message += $"\nStack Trace:\n{GetStackTrace().ToString()}";
                }
                OnLog?.Invoke(data);
            }
        }

        private static Type GetCallerType()
        {
            StackFrame frame = new StackFrame(2);
            System.Reflection.MethodBase method = frame.GetMethod();
            Type type = method.DeclaringType;
            return type;
        }

        private static StackTrace GetStackTrace()
        {
            StackTrace stackTrace = new StackTrace();
            return stackTrace;
        }
    }

    /// <summary>
    /// The <see cref="LogData"/> struct handles each log entry.
    /// </summary>
    public struct LogData
    {
        /// <summary>
        /// The log message.
        /// </summary>
        public string Message;
        /// <summary>
        /// The severity of the message.
        /// </summary>
        public LogSeverity Severity;
        /// <summary>
        /// The <see cref="Type"/> which called the log function.
        /// </summary>
        public Type CallerType;
    }

    /// <summary>
    /// The <see cref="LogSeverity"/> enum handles filtering the logs and assigning severities. It does allow flags to be set. Higher numbers represent more severity.
    /// </summary>
    [Flags]
    public enum LogSeverity
    {
        Debug = 1,
        Info = 2,
        Success = 4,
        Warning = 8,
        Error = 16,
    }
}
