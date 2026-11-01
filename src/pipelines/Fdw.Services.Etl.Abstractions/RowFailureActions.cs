using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// What a Compute stage that reads and writes tables does with one row it cannot build. The stage states one of these; there is
/// no default, because a failing row is data the author has to decide about (FDW-1246).
/// </summary>
public static class RowFailureActions
{
    /// <summary>The stage fails and nothing is written.</summary>
    public const string Fail = "Fail";

    /// <summary>The row is dropped and reported as an error of the run.</summary>
    public const string Skip = "Skip";

    /// <summary>The row is rejected with its error code and kept with the stage's rejects.</summary>
    public const string Reject = "Reject";

    /// <summary>Gets every action a stage may state.</summary>
    public static IReadOnlyList<string> All { get; } = [Fail, Skip, Reject];

    /// <summary>Tells whether <paramref name="action"/> is one of the actions.</summary>
    /// <param name="action">The action as a request states it.</param>
    /// <returns><see langword="true"/> when it is Fail, Skip or Reject, spelled exactly so.</returns>
    public static bool IsKnown(string? action) => action is Fail or Skip or Reject;
}
