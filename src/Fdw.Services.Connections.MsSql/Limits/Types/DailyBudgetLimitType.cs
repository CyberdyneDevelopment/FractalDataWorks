using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.MsSql.Limits.Types;

/// <summary>
/// TypeOption for the DailyBudget connection limit kind.
/// Caps total daily queries via a write-ahead counter in OpsDb.
/// Subtype configuration is stored in <c>conn.MsSqlDailyBudget</c>.
/// </summary>
[TypeOption(typeof(MsSqlConnectionLimitTypes), "DailyBudget")]
public sealed class DailyBudgetLimitType : ConnectionLimitTypeBase
{
    /// <summary>Initializes a new instance of <see cref="DailyBudgetLimitType"/>.</summary>
    public DailyBudgetLimitType()
        : base(
            "DailyBudget",
            "Daily Budget",
            "Limits the total number of queries per calendar day.",
            [
                new ConfigurationFieldDescriptor(
                    "MaxQueriesPerDay",
                    "Max Queries Per Day",
                    "e.g. 10000 (optional)",
                    ConfigurationFieldKinds.Numeric),
            ])
    {
    }
}
