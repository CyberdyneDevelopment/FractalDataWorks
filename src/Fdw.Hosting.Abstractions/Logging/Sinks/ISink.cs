namespace Fdw.Hosting.Abstractions.Logging;

/// <summary>
/// Represents a logging sink type for Serilog configuration.
/// </summary>
public interface ISink
{
    /// <summary>
    /// Gets the unique identifier for this sink type.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the name of this sink type.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a description of this sink type.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the configuration section key for this sink in appsettings.json.
    /// </summary>
    string SinkConfigurationKey { get; }

    /// <summary>
    /// Gets whether this sink supports structured logging.
    /// </summary>
    bool SupportsStructuredLogging { get; }

    /// <summary>
    /// Gets whether this sink requires a network connection.
    /// </summary>
    bool RequiresNetwork { get; }
}
