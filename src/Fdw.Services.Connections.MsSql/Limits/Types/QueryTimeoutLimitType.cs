using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.MsSql.Limits.Types;

/// <summary>
/// TypeOption for the QueryTimeout connection limit kind.
/// Cancels queries that exceed the configured timeout at the FDW dispatch layer.
/// Subtype configuration is stored in <c>conn.MsSqlQueryTimeout</c>.
/// </summary>
[TypeOption(typeof(MsSqlConnectionLimitTypes), "QueryTimeout")]
public sealed class QueryTimeoutLimitType : ConnectionLimitTypeBase
{
    /// <summary>Initializes a new instance of <see cref="QueryTimeoutLimitType"/>.</summary>
    public QueryTimeoutLimitType()
        : base(
            2,
            "QueryTimeout",
            "Query Timeout",
            "Cancels queries that exceed the specified duration.",
            [
                new ConfigurationFieldDescriptor(
                    "TimeoutSeconds",
                    "Timeout (seconds)",
                    "e.g. 30",
                    ConfigurationFieldKinds.Numeric,
                    IsRequired: true),
            ])
    {
    }
}
