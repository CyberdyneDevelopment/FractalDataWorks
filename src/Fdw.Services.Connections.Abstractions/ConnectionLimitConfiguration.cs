using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Abstract base class for connection limit configuration records.
/// Concrete subclasses live in per-connection-type packages (MsSql, Http)
/// and carry type-specific fields (MaxPerSecond, TimeoutSeconds, etc.).
///
/// The parent header row (conn.*ConnectionLimit table) holds ConnectionConfigurationId
/// and LimitType. Each subtype has its own child table for its specific fields.
///
/// Virtual enforcement properties expose limit values in a connection-type-agnostic
/// way so that <c>LimitEnforcementDataGateway</c> in Services.Data can operate
/// exclusively through the base class — keeping MsSql/Http types invisible above
/// the connection layer.
/// </summary>
[ExcludeFromCodeCoverage]
public class ConnectionLimitConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Initializes a new instance of <see cref="ConnectionLimitConfiguration"/>.
    /// </summary>
    /// <param name="serviceType">Domain name (e.g., "Connection").</param>
    /// <param name="serviceOptionType">Connection option type (e.g., "MsSql", "Http").</param>
    /// <param name="sectionName">IOptions section path.</param>
    protected ConnectionLimitConfiguration(
        string serviceType,
        string serviceOptionType,
        string sectionName)
    {
        ServiceType = serviceType;
        ServiceOptionType = serviceOptionType;
        SectionName = sectionName;
    }

    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SectionName { get; }

    /// <inheritdoc />
    public string ServiceType { get; }

    /// <inheritdoc />
    public string? ServiceOptionType { get; }

    /// <summary>
    /// Gets or sets the connection configuration identifier this limit applies to.
    /// </summary>
    public Guid ConnectionConfigurationId { get; set; }

    /// <summary>
    /// Gets or sets the limit type within the connection-type-specific TypeCollection.
    /// Must match a Name in the connection-type-specific TypeCollection
    /// (e.g., MsSqlConnectionLimitTypes.ByName(LimitType)).
    /// </summary>
    public string LimitType { get; set; } = string.Empty;

    // ── Enforcement properties (type-agnostic) ───────────────────────────────

    /// <summary>
    /// Gets the maximum outbound operations per second for rate-limiting.
    /// Return null when this limit record does not configure a rate limit.
    /// </summary>
    public virtual int? EnforceMaxPerSecond => null;

    /// <summary>
    /// Gets the optional burst allowance above <see cref="EnforceMaxPerSecond"/>.
    /// When null, no burst headroom is granted beyond the base rate.
    /// </summary>
    public virtual int? EnforceBurstSize => null;

    /// <summary>
    /// Gets the maximum number of concurrent in-flight operations.
    /// Return null when this limit record does not configure a concurrency limit.
    /// </summary>
    public virtual int? EnforceMaxConcurrent => null;

    /// <summary>
    /// Gets the maximum rows / result items returned per operation.
    /// Return null when this limit record does not configure a result-size limit.
    /// </summary>
    public virtual int? EnforceMaxRows => null;

    /// <summary>
    /// Gets the timeout in seconds for a single outbound operation.
    /// Return null when this limit record does not configure a timeout.
    /// </summary>
    public virtual int? EnforceTimeoutSeconds => null;

    /// <summary>
    /// Gets the daily operation count budget.
    /// Return null when this limit record does not configure a daily query budget.
    /// </summary>
    public virtual int? EnforceMaxQueriesPerDay => null;

    /// <summary>
    /// Gets the daily byte budget.
    /// Return null when this limit record does not configure a daily byte budget.
    /// </summary>
    public virtual long? EnforceMaxBytesPerDay => null;
}
