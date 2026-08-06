using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.EdgeTypes;

/// <summary>
/// Pipeline writes to a Connection.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageEdgeTypes), "WritesTo")]
public sealed class WritesToEdgeType : LineageEdgeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WritesToEdgeType"/> class.
    /// </summary>
    public WritesToEdgeType() : base(4, "WritesTo") { }
}
