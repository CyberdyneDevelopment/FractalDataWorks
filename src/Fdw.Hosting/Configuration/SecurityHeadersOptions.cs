using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Configuration;

/// <summary>
/// Configuration options for security headers middleware.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public sealed class SecurityHeadersOptions
{
    public bool AllowFraming { get; set; }

    public string? ContentSecurityPolicy { get; set; }

    public bool EnableDefaultCsp { get; set; } = true;

    /// <summary>
    /// Paths that should have no-cache headers. Defaults to auth and user paths.
    /// </summary>
    public string[] SensitivePaths { get; set; } =
    [
        "/api/v1/auth", "/api/v1/users", "/api/v1/tenants"
    ];
}
