using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Which row of a key's group a LatestBy transform keeps: the first or the last by the order column (FDW-1109).
/// </summary>
public interface IKeepRule : ITypeOption<int, IKeepRule>
{
    /// <summary>Gets whether a candidate row replaces the row kept so far.</summary>
    /// <param name="candidateToKeptComparison">The comparison of the candidate's order value to the kept row's, in ascending order.</param>
    /// <returns><see langword="true"/> when the candidate becomes the kept row.</returns>
    bool Replaces(int candidateToKeptComparison);
}
