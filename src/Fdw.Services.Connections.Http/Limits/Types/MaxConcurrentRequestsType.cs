using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Http.Limits.Types;

/// <summary>
/// TypeOption for the MaxConcurrentRequests connection limit kind on Http connections.
/// Controls max simultaneous in-flight requests via a per-connection SemaphoreSlim.
/// Subtype configuration is stored in <c>conn.HttpConcurrency</c>.
/// </summary>
[TypeOption(typeof(HttpConnectionLimitTypes), "MaxConcurrentRequests")]
public sealed class MaxConcurrentRequestsType : ConnectionLimitTypeBase
{
    /// <summary>Initializes a new instance of <see cref="MaxConcurrentRequestsType"/>.</summary>
    public MaxConcurrentRequestsType()
        : base(
            3,
            "MaxConcurrentRequests",
            "Max Concurrent Requests",
            "Limits the number of simultaneous outbound HTTP requests.",
            [
                new ConfigurationFieldDescriptor(
                    "MaxConcurrent",
                    "Max Concurrent",
                    "e.g. 10",
                    ConfigurationFieldKinds.Numeric,
                    IsRequired: true),
            ])
    {
    }
}
