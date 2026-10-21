using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Every row of both sides.</summary>
[TypeOption(typeof(JoinKinds), "FullOuter")]
public sealed class FullOuterJoinKind : JoinKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FullOuterJoinKind"/> class.</summary>
    public FullOuterJoinKind() : base("FullOuter", "Every row of both sides.", keepsUnmatchedLeft: true, keepsUnmatchedRight: true)
    {
    }
}
