using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Which rows a merge join keeps when they find no partner on the other side (FDW-1386).
/// </summary>
public interface IJoinKind : ITypeOption<int, IJoinKind>
{
    /// <summary>Gets whether a left row with no match is sent on, with the right fields null.</summary>
    bool KeepsUnmatchedLeft { get; }

    /// <summary>Gets whether a right row with no match is sent on, with the left fields null.</summary>
    bool KeepsUnmatchedRight { get; }
}
