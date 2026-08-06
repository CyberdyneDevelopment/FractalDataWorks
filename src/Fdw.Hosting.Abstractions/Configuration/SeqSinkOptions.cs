using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// Seq sink configuration options.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public class SeqSinkOptions
{
    /// <summary>
    /// Gets or sets whether the Seq sink is enabled. Default is true when configured.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the Seq server URL.
    /// </summary>
    /// <remarks>
    /// Can be overridden via environment variable: FdwHost__Logging__Seq__ServerUrl
    /// </remarks>
    public string ServerUrl { get; set; } = "http://localhost:5341";

    /// <summary>
    /// Gets or sets the API key for Seq authentication.
    /// </summary>
    /// <remarks>
    /// Should be set via environment variable: FdwHost__Logging__Seq__ApiKey
    /// </remarks>
    public string? ApiKey { get; set; }
}
