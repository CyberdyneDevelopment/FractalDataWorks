using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections.Http.Limits;

/// <summary>
/// Subtype configuration for RequestTimeout limit entries on Http connections.
/// Maps to <c>conn.HttpRequestTimeout</c>. is <c>conn.HttpConnectionLimit</c>.
///
/// Wraps each outbound HTTP request in a linked CancellationTokenSource with this timeout.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Connection",
    ServiceType = "Http")]
public sealed partial class HttpRequestTimeoutConfiguration
{
    /// <summary>Gets or sets the parent limit header identifier.</summary>
    public Guid HttpConnectionLimitId { get; set; }


    /// <summary>
    /// Gets or sets the maximum time in seconds an HTTP request may take before cancellation.
    /// </summary>
    public int TimeoutSeconds { get; set; }
}
