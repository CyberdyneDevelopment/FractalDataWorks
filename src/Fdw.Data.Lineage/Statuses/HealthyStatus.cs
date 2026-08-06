using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.Statuses;

/// <summary>
/// Node is healthy and operational.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageNodeStatuses), "Healthy")]
public sealed class HealthyStatus : LineageNodeStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HealthyStatus"/> class.
    /// </summary>
    public HealthyStatus() : base(1, "Healthy") { }
}
