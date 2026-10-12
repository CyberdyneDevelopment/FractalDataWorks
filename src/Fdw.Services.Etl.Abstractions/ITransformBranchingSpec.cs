using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// The Branch members of a stage, as a request states them.
/// </summary>
/// <remarks>
/// A separate interface, not a member of <see cref="ITransformOperationSpec"/>: that interface has other implementers,
/// and only a Branch stage reads these.
/// </remarks>
public interface ITransformBranchingSpec
{
    /// <summary>Gets the paths of the stage, in the order they are tried; the first path whose condition is true takes the record.</summary>
    IReadOnlyList<ITransformPathSpec> Paths { get; }

    /// <summary>Gets what happens to a record that matches no path: an <c>UnmatchedActions</c> option name (Pass or Reject).</summary>
    string? Unmatched { get; }
}
