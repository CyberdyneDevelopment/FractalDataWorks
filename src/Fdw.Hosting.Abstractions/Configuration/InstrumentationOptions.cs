using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// Auto-instrumentation options for automatic tracing/metrics collection.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public class InstrumentationOptions
{
    /// <summary>
    /// Gets or sets whether to instrument HttpClient calls. Default is true.
    /// </summary>
    public bool HttpClient { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to instrument SQL client calls. Default is true.
    /// </summary>
    public bool SqlClient { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to instrument ASP.NET Core. Default is true (for web hosts).
    /// </summary>
    public bool AspNetCore { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to instrument FDW DataGateway commands. Default is true.
    /// </summary>
    public bool FdwCommands { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to instrument FDW connections. Default is true.
    /// </summary>
    public bool FdwConnections { get; set; } = true;
}
