using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Configuration;

/// <summary>
/// Downstream service endpoint URLs for API gateway proxying.
/// Loaded from appsettings.json "ServiceEndpoints" section.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public sealed class ServiceEndpointsOptions
{
    public const string SectionName = "ServiceEndpoints";

    public string Scheduler { get; set; } = string.Empty;

    public string Etl { get; set; } = string.Empty;
}
