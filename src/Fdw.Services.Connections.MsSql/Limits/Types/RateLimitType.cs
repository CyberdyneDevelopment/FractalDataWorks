using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.MsSql.Limits.Types;

/// <summary>
/// TypeOption for the RateLimit connection limit kind.
/// Governs outbound query throughput via a per-connection token bucket.
/// Subtype configuration is stored in <c>conn.MsSqlRateLimit</c>.
/// </summary>
[TypeOption(typeof(MsSqlConnectionLimitTypes), "RateLimit")]
public sealed class RateLimitType : ConnectionLimitTypeBase
{
    /// <summary>Initializes a new instance of <see cref="RateLimitType"/>.</summary>
    public RateLimitType()
        : base(
            1,
            "RateLimit",
            "Rate Limit",
            "Limits the number of queries per second (and optionally per minute) via a token bucket.",
            [
                new ConfigurationFieldDescriptor(
                    "MaxPerSecond",
                    "Max Per Second",
                    "e.g. 10",
                    ConfigurationFieldKinds.Numeric,
                    IsRequired: true),
                new ConfigurationFieldDescriptor(
                    "MaxPerMinute",
                    "Max Per Minute",
                    "e.g. 300 (optional)",
                    ConfigurationFieldKinds.Numeric),
                new ConfigurationFieldDescriptor(
                    "BurstSize",
                    "Burst Size",
                    "e.g. 20 (optional)",
                    ConfigurationFieldKinds.Numeric),
            ])
    {
    }
}
