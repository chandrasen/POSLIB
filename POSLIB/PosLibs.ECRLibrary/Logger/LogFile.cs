using Serilog.Events;
using Serilog;
using PosLibs.ECRLibrary.Common;
using Serilog.Core;

namespace PosLibs.ECRLibrary.Logger
{
    public static class LogFile
    {


        /// <summary>
        /// This method is used to set log level and log details
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="isLogsEnabled"></param>
        /// <param name="logPath"></param>
        /// <param name="dayToRetainLogs"></param>
        public static void SetLogOptions(int logLevel, bool isLogsEnabled, string logPath, int daysToRetainLogs)
        {
            Log.Debug("Enter SetLogOptions method");
            if (!isLogsEnabled)
            {
                if (Directory.Exists(logPath))
                {
                    try
                    {
                        Directory.CreateDirectory(logPath);
                    }
                    catch (Exception e)
                    {
                        Log.Error(PosLibConstant.FILE_NOT_FOUND);
                        Log.Error($"Error creating log directory: {e.Message}");
                        return;
                    }
                }

            }
            else
            {
                if (!Directory.Exists(logPath))
                {
                    try
                    {
                        Directory.CreateDirectory(logPath);
                        Log.Information("log path created:-" + logPath);
                    }
                    catch (Exception e)
                    {
                        Log.Error(PosLibConstant.FILE_NOT_FOUND);
                        Log.Error($"Error creating log directory: {e.Message}");
                        return;
                    }
                }
            }
           

            string currentDate = DateTime.Now.ToString("yyyy/MM/dd");
            string fileName = $"poslib_{currentDate}.log";
            string filePath = Path.Combine(logPath, fileName);
            Log.Information($"filepath: {filePath}");
            LogEventLevel logEventLevel = GetLogLevel(logLevel);
            Log.Information("Log Level:" + logEventLevel.ToString());

            Log.CloseAndFlush();

            if (logLevel == 1)
            {
                Log.Logger = new LoggerConfiguration()
                .WriteTo.File(filePath, outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}]  [{Level:u3}] [{SourceContext}]  {Message} {NewLine} {Exception}")
                .Filter.ByIncludingOnly(isOnlyErrorLevel)
                .CreateLogger();
            }
            else if (logLevel == 3)
            {
                Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(filePath, outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}]  [{Level:u3}] [{SourceContext}]  {Message} {NewLine} {Exception}")
                .CreateLogger();
            }
            else if (logLevel == 4)
            {
                Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(logEventLevel)
                .WriteTo.File(filePath, outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}]  [{Level:u3}] [{SourceContext}]  {Message} {NewLine} {Exception}")
                .CreateLogger();
            }

            else
            {
                var ls = new LoggingLevelSwitch();
                ls.MinimumLevel = ((LogEventLevel)1 + (int)LogEventLevel.Fatal);
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(ls)
                    .CreateLogger();
            }

            try
            {
                DateTime expirationDate = DateTime.Now.AddDays(-daysToRetainLogs);
                DirectoryInfo logDirectory = new DirectoryInfo(logPath);

                foreach (FileInfo file in logDirectory.GetFiles("poslib*.log"))
                {
                    if (file.LastWriteTime < expirationDate)
                    {
                        file.Delete();
                        Log.Information("Logs Deleted successfully: " + file);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error | Failed to manage log files: " + e.Message);
            }
        }

        private static bool isOnlyErrorLevel(LogEvent @event)
        {
            return @event.Level == LogEventLevel.Error;
        }

        public static bool deleteFileifNeeded(string logPath, string expirationDate)
        {
            bool isDeleted = false;
            try
            {

                string fileName = $"poslib.log";
                string filePath = Path.Combine(logPath, fileName);

                DirectoryInfo logDirectory = new DirectoryInfo(logPath);

                foreach (FileInfo file in logDirectory.GetFiles("poslib*.log"))
                {
                    if (DateTime.Now.Date > DateTime.Parse(expirationDate))
                    {
                        Log.CloseAndFlush();
                        file.Delete();
                        Log.Information("Logs Deleted successfully: " + file);
                        isDeleted = true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error | Failed to manage log files: " + e.Message);
            }

            return isDeleted;
        }

        private static LogEventLevel GetLogLevel(int logLevel)
        {
            return logLevel switch
            {
                0 => LogEventLevel.Fatal,
                1 => LogEventLevel.Error,
                2 => LogEventLevel.Warning,
                3 => LogEventLevel.Information,
                4 => LogEventLevel.Debug,
                5 => LogEventLevel.Verbose,
                _ => LogEventLevel.Information
            };
        }
    }
}


