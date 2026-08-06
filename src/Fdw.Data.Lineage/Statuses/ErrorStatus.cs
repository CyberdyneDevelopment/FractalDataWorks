using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.Statuses;

/// <summary>
/// Node has errors.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageNodeStatuses), "Error")]
public sealed class ErrorStatus : LineageNodeStatusBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorStatus"/> class.
    /// </summary>
    public ErrorStatus() : base(3, "Error") { }
}
