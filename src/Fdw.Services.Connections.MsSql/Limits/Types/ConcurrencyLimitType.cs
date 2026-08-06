using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.MsSql.Limits.Types;

/// <summary>
/// TypeOption for the Concurrency connection limit kind.
/// Controls maximum simultaneous in-flight queries via a per-connection SemaphoreSlim.
/// Subtype configuration is stored in <c>conn.MsSqlConcurrency</c>.
/// </summary>
[TypeOption(typeof(MsSqlConnectionLimitTypes), "Concurrency")]
public sealed class ConcurrencyLimitType : ConnectionLimitTypeBase
{
    /// <summary>Initializes a new instance of <see cref="ConcurrencyLimitType"/>.</summary>
    public ConcurrencyLimitType()
        : base(
            4,
            "Concurrency",
            "Concurrency Limit",
            "Limits the number of simultaneous queries against this connection.",
            [
                new ConfigurationFieldDescriptor(
                    "MaxConcurrentQueries",
                    "Max Concurrent Queries",
                    "e.g. 5",
                    ConfigurationFieldKinds.Numeric,
                    IsRequired: true),
            ])
    {
    }
}
