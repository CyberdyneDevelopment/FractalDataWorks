using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Hosting.Abstractions.Logging;

/// <summary>
/// Base class for sink TypeOptions.
/// </summary>
// Why: pure data holder — constructors only assign properties, no branching logic; every
// concrete TypeOption in this hierarchy (ConsoleSink, FileSink, etc.) is already excluded.
[ExcludeFromCodeCoverage]
public abstract class SinkBase : TypeOptionBase<int, SinkBase>, ISink
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SinkBase"/> class for Empty sentinel.
    /// </summary>
    protected SinkBase()
        : base(0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
    {
        SinkConfigurationKey = string.Empty;
        SupportsStructuredLogging = false;
        RequiresNetwork = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SinkBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The sink name.</param>
    /// <param name="description">Description of this sink.</param>
    /// <param name="configurationKey">The configuration section key.</param>
    /// <param name="supportsStructuredLogging">Whether structured logging is supported.</param>
    /// <param name="requiresNetwork">Whether network connectivity is required.</param>
    protected SinkBase(
        int id,
        string name,
        string description,
        string configurationKey,
        bool supportsStructuredLogging,
        bool requiresNetwork)
        : base(id, name, $"Sinks:{name}", name, description, "Logging")
    {
        SinkConfigurationKey = configurationKey;
        SupportsStructuredLogging = supportsStructuredLogging;
        RequiresNetwork = requiresNetwork;
    }

    /// <inheritdoc/>
    public string SinkConfigurationKey { get; }

    /// <inheritdoc/>
    public bool SupportsStructuredLogging { get; }

    /// <inheritdoc/>
    public bool RequiresNetwork { get; }
}
