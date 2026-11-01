using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.MsSql.Limits.Types;

/// <summary>
/// TypeOption for the MaxResultSize connection limit kind.
/// Caps the result set returned per query to prevent unbounded reads.
/// Subtype configuration is stored in <c>conn.MsSqlMaxResultSize</c>.
/// </summary>
[TypeOption(typeof(MsSqlConnectionLimitTypes), "MaxResultSize")]
public sealed class MaxResultSizeLimitType : ConnectionLimitTypeBase
{
    /// <summary>Initializes a new instance of <see cref="MaxResultSizeLimitType"/>.</summary>
    public MaxResultSizeLimitType()
        : base(
            "MaxResultSize",
            "Max Result Size",
            "Caps the number of rows returned by a single query.",
            [
                new ConfigurationFieldDescriptor(
                    "MaxRows",
                    "Max Rows",
                    "e.g. 10000 (optional)",
                    ConfigurationFieldKinds.Numeric),
            ])
    {
    }
}
