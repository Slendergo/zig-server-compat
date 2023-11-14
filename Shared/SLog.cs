using System.Collections.Concurrent;

namespace Shared;

//
// Simple static console Logger by https://github.com/itsEvil - SimpleLog
// 
// How does it work?
// -----------------
// When the user calls any of the public methods 'Debug' 'Trace' etc,
// it formats the message and adds it to the queue to be processed.
// 
// On startup it creates a new thread which runs until termination. 
// Each tick of this thread it tries to dequeue all 'ConsoleMessages' from '_messages' queue.
// After its done with the queue it sleeps for 'SleepTime' MS.
// 
// Termination happens when 'Fatal' method is called.

// Modify these options and colors below as you wish!
public static class SLogConfig
{
    public const string LogDirectoryName = "logs"; //Default = "logs" | Name of the log directory folder
    public const LogLevel MinLevel = LogLevel.Info; //Default = LogLevel.Info | Minimum logging level.
    public const bool ExportErrors = false; //Default = false | Should we export error logs to a file?
    public const int SleepTime = 100; //Default = 100 | Time in MS between sending all queued messages.
}
public static class SLogColors
{
    public const ConsoleColor DebugForeground = ConsoleColor.DarkGray;
    public const ConsoleColor DebugBackground = ConsoleColor.Black;
           
    public const ConsoleColor TraceForeground = ConsoleColor.DarkGray;
    public const ConsoleColor TraceBackground = ConsoleColor.Black;
           
    public const ConsoleColor InfoForeground = ConsoleColor.White;
    public const ConsoleColor InfoBackground = ConsoleColor.Black;
           
    public const ConsoleColor WarnForeground = ConsoleColor.Yellow;
    public const ConsoleColor WarnBackground = ConsoleColor.Black;
           
    public const ConsoleColor ErrorForeground = ConsoleColor.Yellow;
    public const ConsoleColor ErrorBackground = ConsoleColor.Red;
           
    public const ConsoleColor FatalForeground = ConsoleColor.Yellow;
    public const ConsoleColor FatalBackground = ConsoleColor.DarkRed;
}
public enum LogLevel
{
    Debug,
    Trace,
    Info,
    Warn,
    Error,
    Fatal,
    None,
}
public static class SLog {
    private static bool _terminating = false; 
    private static readonly string _path = string.Empty; //directory of the logs folder
    private static readonly ConcurrentQueue<ConsoleMessage> _messages = new(); //queue of messages to write to console
    //init logging directory
    static SLog() {
        _path = Path.Combine(Directory.GetCurrentDirectory(), SLogConfig.LogDirectoryName);
        
        if (!Directory.Exists(_path))
            Directory.CreateDirectory(_path);

        //The #if DEBUG section is safe to remove
//#if DEBUG
//        Debug("SLog::ctor::debug"); 
//        Trace("SLog::ctor::trace"); 
//        Info("SLog::ctor::info"); 
//        Error("SLog::ctor::error"); 
//        Fatal(new Exception("SLog::ctor::fatal"), false); //This will spam your logs directory 
//#endif

        //Creates a new thread to tick the logger
        Task.Run(Tick);
    }
    /// <summary>
    /// Running on a seperate thread to reduce the performance impact on the main thread
    /// </summary>
    private static async void Tick()
    {
        //Keeps thread alive until termination
        while (!_terminating)
        {
            //Dequeue all messages stored up and print to console
            while(_messages.TryDequeue(out var work)) {
                Console.ForegroundColor = work.Foreground;
                Console.BackgroundColor = work.Background;
                Console.Write(work.Message);
                Reset();

                if (SLogConfig.ExportErrors && work.Level == LogLevel.Error) {
                    await File.AppendAllTextAsync(
                        Path.Combine(_path, $"error-log-{DateTime.Now:yyyy-MM-dd---HH-mm-ss-fffffff}.txt"), "\n" + work.Message);
                }

                if(work.Level == LogLevel.Fatal) {
                    await File.AppendAllTextAsync(
                        Path.Combine(_path, $"fatal-log-{DateTime.Now:yyyy-MM-dd---HH-mm-ss-fffffff}.txt"), "\n" + work.Message);
                }
            }

            //How often we tick console logger
            Thread.Sleep(SLogConfig.SleepTime);
        }
    }
    //formats the string adding the time and arguments
    private static string Format(string message, params object[] args) {
        return string.Format($"{DateTime.Now:HH:mm:ss} | {message}", args);
    }
    //This resets the console colors and adds a new line for the next message
    private static void Reset() {
        Console.ResetColor();
        Console.WriteLine();
    }
    //Adds the current message to the queue
    private static void AddMessage(ConsoleMessage msg) {
        _messages.Enqueue(msg);
    }
    //Below are all public methods which can be run from anywhere doing `SLog.Debug("This is my message");` for example
#pragma warning disable CS0162 // Unreachable code detected
    public static void Debug(string message, params object[] args) {
        if (SLogConfig.MinLevel > LogLevel.Debug)
            return;

        AddMessage(new ConsoleMessage(Format(message, args), SLogColors.DebugForeground, SLogColors.DebugBackground, LogLevel.Debug));
    }
    public static void Trace(string message, params object[] args) {
        if (SLogConfig.MinLevel > LogLevel.Trace)
            return;

        AddMessage(new ConsoleMessage(Format(message, args), SLogColors.TraceForeground, SLogColors.TraceBackground, LogLevel.Trace));
    }
    public static void Info(string message, params object[] args) {
        if (SLogConfig.MinLevel > LogLevel.Info)
            return;

        AddMessage(new ConsoleMessage(Format(message, args), SLogColors.InfoForeground, SLogColors.InfoBackground, LogLevel.Info));
    }
    public static void Warn(string message, params object[] args) {
        if (SLogConfig.MinLevel > LogLevel.Warn)
            return;

        AddMessage(new ConsoleMessage(Format(message, args), SLogColors.WarnForeground, SLogColors.WarnBackground, LogLevel.Warn));
    }
    public static void Error(string message, params object[] args) {
        if (SLogConfig.MinLevel > LogLevel.Error)
            return;

        AddMessage(new ConsoleMessage(Format(message, args), SLogColors.ErrorForeground, SLogColors.ErrorBackground, LogLevel.Error));
    }
    public static void Error(Exception ex) {
        if (SLogConfig.MinLevel > LogLevel.Error)
            return;

        AddMessage(new ConsoleMessage(Format(ex.Message + "\n" + ex.StackTrace), SLogColors.ErrorForeground, SLogColors.ErrorBackground, LogLevel.Error));
    }
    public static void Fatal(Exception ex, bool terminate = true) {
        
        if(terminate)
            Terminate();

        if (SLogConfig.MinLevel > LogLevel.Fatal)
            return;

        AddMessage(new ConsoleMessage(Format(ex.Message + "\n" + ex.StackTrace), SLogColors.FatalForeground, SLogColors.FatalBackground, LogLevel.Fatal));
    }
#pragma warning restore CS0162 // Unreachable code detected
    public static void Terminate() {
        _terminating = true;
    }
}
public readonly struct ConsoleMessage(string message, ConsoleColor foreground, ConsoleColor background, LogLevel level)
{
    public readonly string Message = message;
    public readonly ConsoleColor Foreground = foreground;
    public readonly ConsoleColor Background = background;
    public readonly LogLevel Level = level;
}
