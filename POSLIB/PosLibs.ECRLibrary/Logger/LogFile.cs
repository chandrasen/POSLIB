using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Events;
using Serilog;
using PosLibs.ECRLibrary.Common;

namespace PosLibs.ECRLibrary.Logger
{
    public class LogFile
    {
       public LogFile() { }

        public void SetLogOptions(int logLevel, bool isLogsEnabled, string logPath, int dayToRetainLogs)
        {
            Log.Information("Inside SetLogOptions method");

            DirectoryInfo logDirectory = null;
            if (isLogsEnabled)
            {
                try
                {
                    logDirectory = new DirectoryInfo(logPath);
                    if (!logDirectory.Exists)
                    {
                        logDirectory.Create();
                    }
                }
                catch (ArgumentException e)
                {
                    Log.Error(PinLabsEcrConstant.FILE_NOT_FOUND);
                    
                }

                if (logDirectory != null)
                {
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

                    try
                    {
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
                    catch (IOException e)
                    {
                        Log.Error("Error | Failed to create log file | SetLogOptions method: " + e.Message);
                    }
                }
            }
        }


        // The level for logs
        private static LogEventLevel GetLogLevel(int logLevel)
        {
            switch (logLevel)
            {
                case 0:
                    return LogEventLevel.Fatal;
                case 1:
                    return LogEventLevel.Error;
                case 2:
                    return LogEventLevel.Warning;
                case 3:
                    return LogEventLevel.Information;
                case 4:
                    return LogEventLevel.Debug;
                case 5:
                    return LogEventLevel.Verbose;
                default:
                    return LogEventLevel.Information;
            }
        }
    }
}
