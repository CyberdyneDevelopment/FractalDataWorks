using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// File sink configuration options.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public class FileSinkOptions
{
    /// <summary>
    /// Gets or sets whether the file sink is enabled. Default is true when configured.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the log file path. Supports date tokens like {Date}.
    /// </summary>
    public string Path { get; set; } = "logs/fdw-.log";

    /// <summary>
    /// Gets or sets the rolling interval: "Infinite", "Year", "Month", "Day", "Hour", "Minute".
    /// Default is "Day".
    /// </summary>
    public string RollingInterval { get; set; } = "Day";

    /// <summary>
    /// Gets or sets whether to roll on file size limit. Default is true.
    /// </summary>
    public bool RollOnFileSizeLimit { get; set; } = true;

    /// <summary>
    /// Gets or sets the file size limit in bytes. Default is 100MB.
    /// </summary>
    public long FileSizeLimitBytes { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the number of retained files. Default is 31.
    /// </summary>
    public int RetainedFileCountLimit { get; set; } = 31;

    /// <summary>
    /// Gets or sets whether to use JSON formatting. Default is true.
    /// </summary>
    public bool UseJsonFormat { get; set; } = true;
}
