using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// The kind-specific members a stage request actually states.
/// </summary>
/// <remarks>
/// A member counts as stated when it is not null and not empty; an absent member and an empty list are the same thing.
/// The names are the camel-case names of <see cref="StageMember.Name"/>.
/// </remarks>
public interface IStatedMembersSpec
{
    /// <summary>Gets the names of the kind-specific members the request states.</summary>
    IReadOnlyList<string> StatedMembers { get; }

    /// <summary>
    /// Gets the names of the members the request states on its field mappings (for example <c>transforms</c>), each once.
    /// A member of a field mapping is stated on a stage only when the stage's kind lists it in <see cref="IStageMemberShape.Members"/>.
    /// </summary>
    IReadOnlyList<string> StatedMappingMembers { get; }
}
