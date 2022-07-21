using Microsoft.Extensions.Logging;

public static class ApplicationLogging
{
    public static ILoggerFactory LogFactory { get; } = LoggerFactory.Create(builder =>
    {
        builder.ClearProviders();
            // Clear Microsoft's default providers (like eventlogs and others)
            builder.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "yyyy-MM-dd hh:mm:ss ";
        }).SetMinimumLevel(LogLevel.Information);
    });

    public static ILogger<T> CreateLogger<T>() => LogFactory.CreateLogger<T>();
}
