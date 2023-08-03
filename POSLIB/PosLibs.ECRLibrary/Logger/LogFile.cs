using Serilog.Events;
using Serilog;
using PosLibs.ECRLibrary.Common;

namespace PosLibs.ECRLibrary.Logger
{
    public class LogFile
    {
        public LogFile() { }

        public void SetLogOptions(string logLevel, bool isLogsEnabled, string logPath, int dayToRetainLogs)
        {
            Log.Information("Inside SetLogOptions method");
            try
            {
                if (isLogsEnabled)
                {
                    DirectoryInfo logDirectory = new DirectoryInfo(logPath);
                    if (!logDirectory.Exists)
                    {
                        logDirectory.Create();
                    }
                    // Rest of the code related to log file operations
                    string fileName = $"poslib.log";
                    string filePath = Path.Combine(logPath, fileName);

                    Log.Information($"filename: {fileName}");
                    Log.Information($"filepath: {filePath}");

                    // Set log level
                    LogEventLevel logEventLevel = GetLogLevel(logLevel);
                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Is(logEventLevel)
                        .WriteTo.File(filePath, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                        .CreateLogger();

                    // Set log file retention period
                    DateTime expirationDate = DateTime.Now.AddDays(-dayToRetainLogs);
                    FileInfo[] logFiles = logDirectory.GetFiles("poslib*.log");

                    foreach (FileInfo file in logFiles)
                    {
                        DateTime lastModified = file.LastWriteTime;

                        if (lastModified < expirationDate)
                        {
                            file.Delete();
                        }

                        Log.Information("Logs Generated successfully: " + file);
                    }
                }
            }
            catch (ArgumentException e)
            {
                Log.Error(PinLabsEcrConstant.FILE_NOT_FOUND);
                Log.Error("Error: " + e.Message);
            }
            catch (IOException e)
            {
                Log.Error("Error | Failed to create log file | SetLogOptions method: " + e.Message);
            }
        }


        // The level for logs
        private static LogEventLevel GetLogLevel(string logLevel)
        {
            #region Commented code for later use
            //switch (logLevel)
            //{
            //case 0:
            //    return LogEventLevel.Fatal;
            //case 1:
            //    return LogEventLevel.Error;
            //case 2:
            //    return LogEventLevel.Warning;
            //case 3:
            //    return LogEventLevel.Information;
            //case 4:
            //    return LogEventLevel.Debug;
            //case 5:
            //    return LogEventLevel.Verbose;
            //default:
            //    return LogEventLevel.Information; 
            #endregion

            switch (logLevel.ToLower())
            {
                case "error":
                    return LogEventLevel.Error;
                case "warning":
                    return LogEventLevel.Warning;
                case "information":
                    return LogEventLevel.Information;
                default:
                    return LogEventLevel.Information;
            }
        }
    }
}


