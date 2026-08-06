using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections.MsSql.Limits;

/// <summary>
/// Subtype configuration for DailyBudget limit entries on MsSql connections.
/// Maps to <c>conn.MsSqlDailyBudget</c>. is <c>conn.MsSqlConnectionLimit</c>.
///
/// Caps total daily usage per connection using a write-ahead counter in
/// <c>ops.ConnectionLimitCounter</c>. The counter is checked in-memory first
/// (fast path) and written to the DB periodically. At midnight UTC the nightly
/// reset job zeros all counters for the new day.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Connection",
    ServiceType = "MsSql")]
public sealed partial class MsSqlDailyBudgetConfiguration
{
    /// <summary>Gets or sets the parent limit header identifier.</summary>
    public Guid MsSqlConnectionLimitId { get; set; }


    /// <summary>
    /// Gets or sets the maximum total number of queries allowed per calendar day.
    /// When null, no query count cap is enforced.
    /// </summary>
    public int? MaxQueriesPerDay { get; set; }

    /// <summary>
    /// Gets or sets the maximum total bytes read/written per calendar day.
    /// When null, no byte budget is enforced.
    /// </summary>
    public long? MaxBytesPerDay { get; set; }
}
