namespace Fdw.Hosting.Abstractions.Logging;

/// <summary>
/// Represents a logging level for Serilog configuration.
/// </summary>
public interface ILogLevel
{
    /// <summary>
    /// Gets the unique identifier for this log level.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the name of this log level.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a description of when this log level should be used.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the Serilog LogEventLevel value.
    /// </summary>
    Serilog.Events.LogEventLevel SerilogLevel { get; }

    /// <summary>
    /// Gets the Microsoft.Extensions.Logging LogLevel value.
    /// </summary>
    Microsoft.Extensions.Logging.LogLevel MicrosoftLevel { get; }
}
