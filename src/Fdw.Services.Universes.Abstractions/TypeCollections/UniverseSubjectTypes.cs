using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>The kinds of thing that can hold a universe membership.</summary>
/// <remarks>
/// Closed, and closed for a stronger reason than the lifecycle sets: there is no third kind a
/// future domain could contribute. Something either is a person or is a named group of people.
/// That is why universe.UniverseMember.SubjectType carries a CHECK constraint while
/// UniverseResource.ResourceType deliberately does not.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(UniverseSubjectTypeBase), typeof(IUniverseSubjectType), typeof(UniverseSubjectTypes))]
public abstract partial class UniverseSubjectTypes : TypeCollectionBase<UniverseSubjectTypeBase, IUniverseSubjectType>
{
}
