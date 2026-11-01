using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Only rows that have a partner.</summary>
[TypeOption(typeof(JoinKinds), "Inner")]
public sealed class InnerJoinKind : JoinKindBase
{
    /// <summary>Initializes a new instance of the <see cref="InnerJoinKind"/> class.</summary>
    public InnerJoinKind() : base("Inner", "Only rows that have a partner.", keepsUnmatchedLeft: false, keepsUnmatchedRight: false)
    {
    }
}
