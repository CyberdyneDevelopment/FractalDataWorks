namespace Fdw.Collections;

/// <summary>
/// A type option that has a place in an order (FDW-2142): how urgent, how important. A higher rank comes first.
/// </summary>
/// <remarks>The type-collection value listing gives it as <c>rank</c>, so a screen orders the options without naming one.</remarks>
public interface IRankShape
{
    /// <summary>Gets the rank; higher is more urgent or important.</summary>
    int Rank { get; }
}
