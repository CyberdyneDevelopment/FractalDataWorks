using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Configuration;

/// <summary>
/// Configuration options for CORS policy.
/// Loaded from appsettings.json "Cors" section.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public bool Enabled { get; set; } = true;

    public IList<string> Origins { get; set; } = [];

    public IList<string> Methods { get; set; } =
    [
        "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"
    ];

    public IList<string> Headers { get; set; } =
    [
        "Content-Type", "Authorization", "X-Requested-With", "X-Tenant-Id", "X-Correlation-Id"
    ];

    public IList<string> ExposedHeaders { get; set; } =
    [
        "X-Correlation-Id", "X-Request-Id", "WWW-Authenticate",
        "X-RateLimit-Limit", "X-RateLimit-Remaining", "X-RateLimit-Reset"
    ];

    public bool AllowCredentials { get; set; } = true;

    public int PreflightMaxAgeSeconds { get; set; } = 600;
}
