using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections.Http.Limits;

/// <summary>
/// Subtype configuration for MaxResponseSize limit entries on Http connections.
/// Maps to <c>conn.HttpMaxResponseSize</c>. is <c>conn.HttpConnectionLimit</c>.
///
/// Caps the size of HTTP responses accepted from external systems.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Connection",
    ServiceType = "Http")]
public sealed partial class HttpMaxResponseSizeConfiguration
{
    /// <summary>Gets or sets the parent limit header identifier.</summary>
    public Guid HttpConnectionLimitId { get; set; }


    /// <summary>
    /// Gets or sets the maximum response size in megabytes.
    /// </summary>
    public decimal MaxMb { get; set; }
}
