using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Http.Limits.Types;

/// <summary>
/// TypeOption for the MaxRequestRate connection limit kind on Http connections.
/// Controls outbound request throughput via a per-connection token bucket.
/// Subtype configuration is stored in <c>conn.HttpMaxRequestRate</c>.
/// </summary>
[TypeOption(typeof(HttpConnectionLimitTypes), "MaxRequestRate")]
public sealed class MaxRequestRateType : ConnectionLimitTypeBase
{
    /// <summary>Initializes a new instance of <see cref="MaxRequestRateType"/>.</summary>
    public MaxRequestRateType()
        : base(
            1,
            "MaxRequestRate",
            "Max Request Rate",
            "Limits the number of outbound HTTP requests per second via a token bucket.",
            [
                new ConfigurationFieldDescriptor(
                    "RequestsPerSecond",
                    "Requests Per Second",
                    "e.g. 10",
                    ConfigurationFieldKinds.Numeric,
                    IsRequired: true),
                new ConfigurationFieldDescriptor(
                    "BurstSize",
                    "Burst Size",
                    "e.g. 20 (optional)",
                    ConfigurationFieldKinds.Numeric),
            ])
    {
    }
}
