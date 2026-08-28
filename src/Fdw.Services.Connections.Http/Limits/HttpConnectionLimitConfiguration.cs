using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Http.Limits;

/// <summary>
/// Header record for a single Http connection limit entry.
/// Maps to <c>conn.HttpConnectionLimit</c>.
///
/// Works identically to MsSqlConnectionLimitConfiguration:
/// one parent row per active limit, LimitType resolves via HttpConnectionLimitTypes.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Connection",
    ServiceType = "Http")]
public sealed partial class HttpConnectionLimitConfiguration : ConnectionLimitConfiguration
{
    /// <summary>
    /// Initializes a new instance of <see cref="HttpConnectionLimitConfiguration"/>.
    /// </summary>
    public HttpConnectionLimitConfiguration()
        : base("Connection", "Http", "Connections")
    {
    }


    /// <summary>Gets or sets an optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether this is the current active version.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    // --- Subtype navigation ---

    /// <summary>Gets or sets the max request rate subtype fields, if LimitType == "MaxRequestRate".</summary>
    [NotMapped]
    public HttpMaxRequestRateConfiguration? MaxRequestRate { get; set; }

    /// <summary>Gets or sets the max response size subtype fields, if LimitType == "MaxResponseSize".</summary>
    [NotMapped]
    public HttpMaxResponseSizeConfiguration? MaxResponseSize { get; set; }

    /// <summary>Gets or sets the max concurrent requests subtype fields, if LimitType == "MaxConcurrentRequests".</summary>
    [NotMapped]
    public HttpConcurrencyConfiguration? Concurrency { get; set; }

    /// <summary>Gets or sets the request timeout subtype fields, if LimitType == "RequestTimeout".</summary>
    [NotMapped]
    public HttpRequestTimeoutConfiguration? RequestTimeout { get; set; }

    // ── Enforcement overrides ────────────────────────────────────────────────

    /// <inheritdoc/>
    public override int? EnforceMaxPerSecond => MaxRequestRate?.RequestsPerSecond;

    /// <inheritdoc/>
    public override int? EnforceBurstSize => MaxRequestRate?.BurstSize;

    /// <inheritdoc/>
    public override int? EnforceMaxConcurrent => Concurrency?.MaxConcurrent;

    /// <inheritdoc/>
    public override int? EnforceTimeoutSeconds => RequestTimeout?.TimeoutSeconds;
}
