using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>The kinds of thing that can hold a dataverse membership.</summary>
/// <remarks>
/// Closed, and closed for a stronger reason than the lifecycle sets: there is no third kind a
/// future domain could contribute. Something either is a person or is a named group of people.
/// That is why dataverse.DataverseMember.SubjectType carries a CHECK constraint while
/// DataverseResource.ResourceType deliberately does not.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DataverseSubjectTypeBase), typeof(IDataverseSubjectType), typeof(DataverseSubjectTypes))]
public abstract partial class DataverseSubjectTypes : TypeCollectionBase<DataverseSubjectTypeBase, IDataverseSubjectType>
{
}
