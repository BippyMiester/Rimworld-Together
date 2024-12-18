using System.Text;
using static Shared.CommonEnumerators;

namespace GameServer
{
    public static class Logger
    {
        //Variables

        private static readonly Semaphore semaphore = new Semaphore(1, 1);

        private static readonly Dictionary<LogMode, ConsoleColor> colorDictionary = new Dictionary<LogMode, ConsoleColor>
        {
            { LogMode.Message, ConsoleColor.White },
            { LogMode.Warning, ConsoleColor.Yellow },
            { LogMode.Error, ConsoleColor.Red },
            { LogMode.Title, ConsoleColor.Green },
            { LogMode.Outsider, ConsoleColor.Magenta },
            { LogMode.Success, ConsoleColor.Cyan },          // Success: Bright Cyan for visibility
            { LogMode.Debug, ConsoleColor.DarkGray },        // Debug: Dark Gray to distinguish debug info
            { LogMode.VerboseDebug, ConsoleColor.Gray },     // Verbose Debug: Regular Gray for less prominent detail
            { LogMode.Emergency, ConsoleColor.DarkRed },     // Emergency: Dark Red to highlight critical failures
            { LogMode.Alert, ConsoleColor.Red },             // Alert: Bright Red for high-importance messages
            { LogMode.Critical, ConsoleColor.DarkMagenta },  // Critical: Dark Magenta for significant issues
            { LogMode.Notice, ConsoleColor.Blue },           // Notice: Blue for informational notices
            { LogMode.Info, ConsoleColor.DarkCyan }          // Info: Dark Cyan for regular informational logs
        };

        // Consolidated Log Function
        public static void Log(string message, LogMode logMode, LogImportanceMode importance = LogImportanceMode.Normal)
        {
            WriteToConsole(message, logMode, importance);
        }

        // Individual Methods that Call the Consolidated Function
        public static void Message(string message, LogImportanceMode importance = LogImportanceMode.Normal) 
            => Log(message, LogMode.Message, importance);

        public static void Warning(string message, LogImportanceMode importance = LogImportanceMode.Normal) 
            => Log(message, LogMode.Warning, importance);

        public static void Error(string message, LogImportanceMode importance = LogImportanceMode.Normal) 
            => Log(message, LogMode.Error, importance);

        public static void Title(string message, LogImportanceMode importance = LogImportanceMode.Normal) 
            => Log(message, LogMode.Title, importance);

        public static void Outsider(string message, LogImportanceMode importance = LogImportanceMode.Normal) 
            => Log(message, LogMode.Outsider, importance);

        public static void Debug(string message, LogImportanceMode importance = LogImportanceMode.Normal) 
            => Log(message, LogMode.Debug, importance);

        public static void VerboseDebug(string message, LogImportanceMode importance = LogImportanceMode.Normal) 
            => Log(message, LogMode.VerboseDebug, importance);

        public static void Emergency(string message, LogImportanceMode importance = LogImportanceMode.Normal) 
            => Log(message, LogMode.Emergency, importance);

        public static void Alert(string message, LogImportanceMode importance = LogImportanceMode.Normal) 
            => Log(message, LogMode.Alert, importance);

        public static void Critical(string message, LogImportanceMode importance = LogImportanceMode.Normal) 
            => Log(message, LogMode.Critical, importance);

        public static void Notice(string message, LogImportanceMode importance = LogImportanceMode.Normal) 
            => Log(message, LogMode.Notice, importance);

        public static void Info(string message, LogImportanceMode importance = LogImportanceMode.Normal) 
            => Log(message, LogMode.Info, importance);
        
        public static void Rcon(string message, LogImportanceMode importance = LogImportanceMode.Normal) 
            => Log(message, LogMode.Rcon, importance);
        
        private static void WriteToConsole(string text, LogMode mode, LogImportanceMode importance, bool writeToLogs = true)
        {
            
            semaphore.WaitOne();

            try
            {
                if (CheckIfShouldPrint(importance))
                {
                    if (writeToLogs) WriteToLogs(text);

                    Console.ForegroundColor = colorDictionary[mode];
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] | " + text);
                    Console.ForegroundColor = ConsoleColor.White;

                    if (Master.discordConfig != null && Master.discordConfig.Enabled) DiscordManager.SendMessageToConsoleChannelBuffer(text);
                }
            }
            catch { throw new Exception($"Logger encountered an error. This should never happen"); }

            semaphore.Release();
        }

        //Function that writes contents to log file

        private static void WriteToLogs(string toLog)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"[{DateTime.Now:HH:mm:ss}] | " + toLog);
            stringBuilder.Append(Environment.NewLine);

            DateTime dateTime = DateTime.Now.Date;
            string nowFileName = ($"{dateTime.Year}-{dateTime.Month.ToString("D2")}-{dateTime.Day.ToString("D2")}");
            string nowFullPath = Master.systemLogsPath + Path.DirectorySeparatorChar + nowFileName + ".txt";

            File.AppendAllText(nowFullPath, stringBuilder.ToString());
            stringBuilder.Clear();
        }

        //Checks if the importance of the log has been enabled

        private static bool CheckIfShouldPrint(LogImportanceMode importance)
        {
            if (importance == LogImportanceMode.Normal) return true;
            else if (importance == LogImportanceMode.Verbose && Master.serverConfig.VerboseLogs) return true;
            else if (importance == LogImportanceMode.Extreme && Master.serverConfig.ExtremeVerboseLogs) return true;
            else return false;
        }
    }
}
