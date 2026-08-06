using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.EdgeTypes;

/// <summary>
/// Pipeline reads from a Connection.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageEdgeTypes), "ReadsFrom")]
public sealed class ReadsFromEdgeType : LineageEdgeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadsFromEdgeType"/> class.
    /// </summary>
    public ReadsFromEdgeType() : base(3, "ReadsFrom") { }
}
