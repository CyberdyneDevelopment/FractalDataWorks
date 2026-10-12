using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// The members of a pipeline stage's body that one stage kind owns (FDW-1120).
/// </summary>
/// <remarks>
/// Why one list: it is both what the type-collection listing shows as the kind's <c>parameters</c> and what decides which
/// stated members belong on a stage of that kind. A member stated on a stage whose kind does not own it is refused, so
/// the listing and the refusal cannot disagree.
/// </remarks>
public interface IStageMemberShape
{
    /// <summary>Gets the members the kind owns.</summary>
    IReadOnlyList<StageMember> Members { get; }
}
