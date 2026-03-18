using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GamePrince
{
    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4
    }

    /// <summary>
    /// 日志条目模型
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Category { get; set; } = "";
        public string Message { get; set; } = "";
        public string? Exception { get; set; }
        public string? StackTrace { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] ");
            sb.Append($"[{Level.ToString().ToUpper()}] ");
            sb.Append($"[{Category}] ");
            sb.Append(Message);
            
            if (!string.IsNullOrEmpty(Exception))
            {
                sb.AppendLine();
                sb.Append($"Exception: {Exception}");
            }
            
            if (!string.IsNullOrEmpty(StackTrace))
            {
                sb.AppendLine();
                sb.Append($"StackTrace: {StackTrace}");
            }
            
            return sb.ToString();
        }
    }

    /// <summary>
    /// 日志记录服务
    /// 提供统一的日志记录接口，支持控制台输出和文件输出
    /// </summary>
    public static class LoggerService
    {
        private static readonly object _lock = new object();
        private static readonly List<LogEntry> _logs = new List<LogEntry>();
        private static readonly int _maxLogsInMemory = 1000;
        
        private static string? _logFilePath;
        private static LogLevel _minLevel = LogLevel.Debug;
        private static bool _enableConsoleOutput = true;
        private static bool _enableFileOutput = true;

        /// <summary>
        /// 初始化日志服务
        /// </summary>
        /// <param name="logDirectory">日志文件目录</param>
        /// <param name="minLevel">最低日志级别</param>
        public static void Initialize(string? logDirectory = null, LogLevel minLevel = LogLevel.Info)
        {
            _minLevel = minLevel;
            
            // 设置默认日志目录
            if (string.IsNullOrEmpty(logDirectory))
            {
                logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            }
            
            // 确保日志目录存在
            if (!Directory.Exists(logDirectory))
            {
                try
                {
                    Directory.CreateDirectory(logDirectory);
                }
                catch
                {
                    // 如果无法创建日志目录，禁用文件输出
                    _enableFileOutput = false;
                    logDirectory = null;
                }
            }
            
            // 设置日志文件路径
            if (!string.IsNullOrEmpty(logDirectory))
            {
                _logFilePath = Path.Combine(logDirectory, $"GamePrince_{DateTime.Now:yyyyMMdd}.log");
            }
            
            Info("Logger", "日志服务已初始化");
            if (!string.IsNullOrEmpty(_logFilePath))
            {
                Info("Logger", $"日志文件路径: {_logFilePath}");
            }
            Info("Logger", $"日志级别: {_minLevel}");
        }

        /// <summary>
        /// 记录调试级别日志
        /// </summary>
        public static void Debug(string category, string message)
        {
            Log(LogLevel.Debug, category, message);
        }

        /// <summary>
        /// 记录信息级别日志
        /// </summary>
        public static void Info(string category, string message)
        {
            Log(LogLevel.Info, category, message);
        }

        /// <summary>
        /// 记录警告级别日志
        /// </summary>
        public static void Warning(string category, string message)
        {
            Log(LogLevel.Warning, category, message);
        }

        /// <summary>
        /// 记录错误级别日志
        /// </summary>
        public static void Error(string category, string message, Exception? ex = null)
        {
            Log(LogLevel.Error, category, message, ex);
        }

        /// <summary>
        /// 记录致命错误级别日志
        /// </summary>
        public static void Fatal(string category, string message, Exception? ex = null)
        {
            Log(LogLevel.Fatal, category, message, ex);
        }

        /// <summary>
        /// 核心日志记录方法
        /// </summary>
        private static void Log(LogLevel level, string category, string message, Exception? ex = null)
        {
            // 检查日志级别
            if (level < _minLevel)
                return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Category = category,
                Message = message,
                Exception = ex?.Message,
                StackTrace = ex?.StackTrace
            };

            lock (_lock)
            {
                // 添加到内存日志列表
                _logs.Add(entry);
                
                // 限制内存中日志数量
                while (_logs.Count > _maxLogsInMemory)
                {
                    _logs.RemoveAt(0);
                }

                // 输出到控制台
                if (_enableConsoleOutput)
                {
                    OutputToConsole(entry);
                }

                // 输出到文件
                if (_enableFileOutput && !string.IsNullOrEmpty(_logFilePath))
                {
                    OutputToFile(entry);
                }
            }
        }

        /// <summary>
        /// 输出到控制台
        /// </summary>
        private static void OutputToConsole(LogEntry entry)
        {
            var color = entry.Level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Fatal => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };

            try
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(entry.ToString());
                Console.ForegroundColor = originalColor;
            }
            catch
            {
                // 控制台可能不可用
                Console.WriteLine(entry.ToString());
            }
        }

        /// <summary>
        /// 输出到文件
        /// </summary>
        private static void OutputToFile(LogEntry entry)
        {
            if (string.IsNullOrEmpty(_logFilePath))
                return;

            try
            {
                File.AppendAllText(_logFilePath, entry.ToString() + Environment.NewLine);
            }
            catch
            {
                // 忽略文件写入错误，避免递归异常
            }
        }

        /// <summary>
        /// 获取所有日志
        /// </summary>
        public static List<LogEntry> GetAllLogs()
        {
            lock (_lock)
            {
                return new List<LogEntry>(_logs);
            }
        }

        /// <summary>
        /// 获取指定级别的日志
        /// </summary>
        public static List<LogEntry> GetLogs(LogLevel minLevel)
        {
            lock (_lock)
            {
                return _logs.FindAll(l => l.Level >= minLevel);
            }
        }

        /// <summary>
        /// 获取指定类别的日志
        /// </summary>
        public static List<LogEntry> GetLogs(string category)
        {
            lock (_lock)
            {
                return _logs.FindAll(l => l.Category == category);
            }
        }

        /// <summary>
        /// 清除所有日志
        /// </summary>
        public static void ClearLogs()
        {
            lock (_lock)
            {
                _logs.Clear();
            }
        }

        /// <summary>
        /// 导出日志到文件
        /// </summary>
        public static void ExportLogs(string filePath, LogLevel minLevel = LogLevel.Debug)
        {
            lock (_lock)
            {
                var sb = new StringBuilder();
                sb.AppendLine("GamePrince Log Export");
                sb.AppendLine($"Export Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine(new string('=', 80));
                sb.AppendLine();

                foreach (var log in _logs)
                {
                    if (log.Level >= minLevel)
                    {
                        sb.AppendLine(log.ToString());
                    }
                }

                File.WriteAllText(filePath, sb.ToString());
            }
        }

        /// <summary>
        /// 记录方法入口日志（用于调试）
        /// </summary>
        public static IDisposable? MethodEnter(string category, string methodName)
        {
            Debug(category, $"Entering: {methodName}");
            return new MethodLogger(category, methodName, false);
        }

        /// <summary>
        /// 记录方法退出日志（用于调试）
        /// </summary>
        public static IDisposable? MethodExit(string category, string methodName)
        {
            return new MethodLogger(category, methodName, true);
        }

        /// <summary>
        /// 方法日志追踪器
        /// </summary>
        private class MethodLogger : IDisposable
        {
            private readonly string _category;
            private readonly string _methodName;
            private readonly bool _isExit;
            private readonly DateTime _startTime;

            public MethodLogger(string category, string methodName, bool isExit)
            {
                _category = category;
                _methodName = methodName;
                _isExit = isExit;
                _startTime = DateTime.Now;
            }

            public void Dispose()
            {
                var elapsed = DateTime.Now - _startTime;
                if (_isExit)
                {
                    Debug(_category, $"Exiting: {_methodName} (took {elapsed.TotalMilliseconds:F2}ms)");
                }
            }
        }
    }
}
