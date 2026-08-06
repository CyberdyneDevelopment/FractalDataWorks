using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.EdgeTypes;

/// <summary>
/// One stage or step must complete before the next begins (ordered sequence).
/// Used for: Stage → Sequences → Stage (ordinal ordering within a Project).
/// Edge metadata carries the Ordinal value for rendering.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageEdgeTypes), "Sequences")]
public sealed class SequencesEdgeType : LineageEdgeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SequencesEdgeType"/> class.</summary>
    public SequencesEdgeType() : base(8, "Sequences") { }
}
