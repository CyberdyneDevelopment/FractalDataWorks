using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.Statuses;

/// <summary>
/// Status is unknown or not monitored.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageNodeStatuses), "Unknown")]
public sealed class UnknownStatus : LineageNodeStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownStatus"/> class.
    /// </summary>
    public UnknownStatus() : base(0, "Unknown") { }
}
