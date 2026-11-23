using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>The description sub-types a description can hold, part of its key beside the type, the thing and the language.</summary>
/// <remarks>
/// An open set of options that any owning package extends, with nothing fixed to one meaning (Mike, 2026-11-16, on the question of what a sub-type separates:
/// "it is nested. it depends. it could be the Implementation and Domain, the Collection and the Option, or you could use an audience. any and all of the above
/// make sense"). A sub-type may name the sort of text, the level it is about or the audience it is for, and each package that holds another text about a thing
/// (a business meaning beside a technical one, one for a customer beside one for an engineer) declares its own option against this collection; the table needs
/// no change and no code reads a sub-type's name to decide what to do. One option ships, <see cref="DescriptionTextSubType"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DescriptionSubTypeBase), typeof(IDescriptionSubType), typeof(DescriptionSubTypes))]
public abstract partial class DescriptionSubTypes : TypeCollectionBase<DescriptionSubTypeBase, IDescriptionSubType>
{
}
