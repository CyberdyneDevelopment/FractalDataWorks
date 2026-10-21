using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Every left row, with the right fields null where there is no partner.</summary>
[TypeOption(typeof(JoinKinds), "LeftOuter")]
public sealed class LeftOuterJoinKind : JoinKindBase
{
    /// <summary>Initializes a new instance of the <see cref="LeftOuterJoinKind"/> class.</summary>
    public LeftOuterJoinKind() : base("LeftOuter", "Every left row, with the right fields null where there is no partner.", keepsUnmatchedLeft: true, keepsUnmatchedRight: false)
    {
    }
}
