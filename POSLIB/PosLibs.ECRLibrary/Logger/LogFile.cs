using Serilog.Events;
using Serilog;
using PosLibs.ECRLibrary.Common;

namespace PosLibs.ECRLibrary.Logger
{
    public class LogFile
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
            if (!isLogsEnabled)
                return;

            Log.Information("Inside SetLogOptions method");

            if (!Directory.Exists(logPath))
            {
                try
                {
                    Directory.CreateDirectory(logPath);
                }
                catch (Exception e)
                {
                    Log.Error(PinLabsEcrConstant.FILE_NOT_FOUND);
                    Log.Error($"Error creating log directory: {e.Message}");
                    return;
                }
            }

            string fileName = $"poslib.log";
            string filePath = Path.Combine(logPath, fileName);

            Log.Information($"filename: {fileName}");
            Log.Information($"filepath: {filePath}");

            LogEventLevel logEventLevel = GetLogLevel(logLevel);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(logEventLevel)
                .WriteTo.File(filePath, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

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


