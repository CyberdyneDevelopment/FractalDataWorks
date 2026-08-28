using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.MsSql.Limits;

/// <summary>
/// Header record for a single MsSql connection limit entry.
/// Maps to <c>conn.MsSqlConnectionLimit</c>.
///
/// One parent row exists per active limit on a connection. The LimitType discriminator
/// resolves via <see cref="MsSqlConnectionLimitTypes"/> to determine which subtype
/// table holds the type-specific fields.
///
/// Multiple limits of different types can coexist simultaneously on one connection.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Connection",
    ServiceType = "MsSql")]
public sealed partial class MsSqlConnectionLimitConfiguration : ConnectionLimitConfiguration
{
    /// <summary>
    /// Initializes a new instance of <see cref="MsSqlConnectionLimitConfiguration"/>.
    /// Default constructor for IOptions binding.
    /// </summary>
    public MsSqlConnectionLimitConfiguration()
        : base("Connection", "MsSql", "Connections")
    {
    }


    /// <summary>
    /// Gets or sets the optional description for this limit entry.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this is the current active version.
    /// </summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this record has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // --- Subtype navigation (populated by configuration cascade loader) ---

    /// <summary>
    /// Gets or sets the rate limit subtype fields, if LimitType == "RateLimit".
    /// </summary>
    [NotMapped]
    public MsSqlRateLimitConfiguration? RateLimit { get; set; }

    /// <summary>
    /// Gets or sets the query timeout subtype fields, if LimitType == "QueryTimeout".
    /// </summary>
    [NotMapped]
    public MsSqlQueryTimeoutConfiguration? QueryTimeout { get; set; }

    /// <summary>
    /// Gets or sets the max result size subtype fields, if LimitType == "MaxResultSize".
    /// </summary>
    [NotMapped]
    public MsSqlMaxResultSizeConfiguration? MaxResultSize { get; set; }

    /// <summary>
    /// Gets or sets the concurrency subtype fields, if LimitType == "Concurrency".
    /// </summary>
    [NotMapped]
    public MsSqlConcurrencyConfiguration? Concurrency { get; set; }

    /// <summary>
    /// Gets or sets the daily budget subtype fields, if LimitType == "DailyBudget".
    /// </summary>
    [NotMapped]
    public MsSqlDailyBudgetConfiguration? DailyBudget { get; set; }

    // ── Enforcement overrides ────────────────────────────────────────────────

    /// <inheritdoc/>
    public override int? EnforceMaxPerSecond => RateLimit?.MaxPerSecond;

    /// <inheritdoc/>
    public override int? EnforceBurstSize => RateLimit?.BurstSize;

    /// <inheritdoc/>
    public override int? EnforceMaxConcurrent => Concurrency?.MaxConcurrentQueries;

    /// <inheritdoc/>
    public override int? EnforceMaxRows => MaxResultSize?.MaxRows;

    /// <inheritdoc/>
    public override int? EnforceTimeoutSeconds => QueryTimeout?.TimeoutSeconds;

    /// <inheritdoc/>
    public override int? EnforceMaxQueriesPerDay => DailyBudget?.MaxQueriesPerDay;

    /// <inheritdoc/>
    public override long? EnforceMaxBytesPerDay => DailyBudget?.MaxBytesPerDay;
}
