using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Base class for the options of <see cref="JoinKinds"/>, using the CRTP pattern.
/// </summary>
public abstract class JoinKindBase : TypeOptionBase<JoinKindBase>, IJoinKind
{
    /// <summary>Initializes a new instance of the <see cref="JoinKindBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="description">Which rows the join keeps.</param>
    /// <param name="keepsUnmatchedLeft">Whether a left row with no match is sent on.</param>
    /// <param name="keepsUnmatchedRight">Whether a right row with no match is sent on.</param>
    protected JoinKindBase(string name, string description, bool keepsUnmatchedLeft, bool keepsUnmatchedRight)
        : base(name, name, description, "JoinKinds")
    {
        KeepsUnmatchedLeft = keepsUnmatchedLeft;
        KeepsUnmatchedRight = keepsUnmatchedRight;
    }

    /// <inheritdoc />
    public bool KeepsUnmatchedLeft { get; }

    /// <inheritdoc />
    public bool KeepsUnmatchedRight { get; }
}
