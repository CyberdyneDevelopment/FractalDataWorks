using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.Statuses;

/// <summary>
/// Data is stale (hasn't been refreshed recently).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageNodeStatuses), "Stale")]
public sealed class StaleStatus : LineageNodeStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StaleStatus"/> class.
    /// </summary>
    public StaleStatus() : base(4, "Stale") { }
}
