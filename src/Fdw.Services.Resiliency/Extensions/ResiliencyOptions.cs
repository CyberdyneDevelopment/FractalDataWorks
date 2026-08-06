using System;

namespace Fdw.Services.Resiliency.Extensions;

/// <summary>
/// Options for configuring resiliency services.
/// </summary>
/// <remarks>
/// This class is reserved for future configuration options such as:
/// - Default timeout values
/// - Global retry behavior overrides
/// - Telemetry integration settings
/// </remarks>
public sealed class ResiliencyOptions
{
    /// <summary>
    /// Gets or sets the default timeout for operations when not specified by the policy.
    /// </summary>
    /// <remarks>
    /// Default is 30 seconds.
    /// </remarks>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether to enable detailed logging.
    /// </summary>
    /// <remarks>
    /// When enabled, additional debug-level logging will be emitted for pipeline events.
    /// Default is false.
    /// </remarks>
    public bool EnableDetailedLogging { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include exception details in log messages.
    /// </summary>
    /// <remarks>
    /// When disabled, exception messages will be sanitized to avoid leaking sensitive information.
    /// Default is false for production safety. Enable explicitly in development via configuration.
    /// </remarks>
    public bool IncludeExceptionDetails { get; set; }
}