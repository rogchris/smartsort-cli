using Serilog;
using Serilog.Core;
using Serilog.Formatting.Json;

namespace SmartSort.App.Logging;

/// <summary>Configures the console host's owned logger. Domain services do not access a global logger.</summary>
public static class LoggerFactory
{
    public static Logger Create(string logFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logFolder);
        Directory.CreateDirectory(logFolder);
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:j}{NewLine}")
            .WriteTo.File(new JsonFormatter(), Path.Combine(logFolder, "log-.jsonl"),
                rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .CreateLogger();
    }
}
