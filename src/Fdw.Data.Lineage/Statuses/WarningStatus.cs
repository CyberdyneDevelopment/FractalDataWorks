using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.Statuses;

/// <summary>
/// Node has warnings but is operational.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageNodeStatuses), "Warning")]
public sealed class WarningStatus : LineageNodeStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WarningStatus"/> class.
    /// </summary>
    public WarningStatus() : base(2, "Warning") { }
}
