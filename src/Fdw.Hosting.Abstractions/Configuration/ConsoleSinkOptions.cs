using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// Console sink configuration options.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public class ConsoleSinkOptions
{
    /// <summary>
    /// Gets or sets whether the console sink is enabled. Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the output format: "Default", "Compact", or "Json". Default is "Compact".
    /// </summary>
    public string Format { get; set; } = "Compact";

    /// <summary>
    /// Gets or sets the output template for custom formatting.
    /// </summary>
    public string? OutputTemplate { get; set; }
}
