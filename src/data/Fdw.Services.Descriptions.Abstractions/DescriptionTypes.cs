using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>The description types: the things whose description is held in the descriptions table.</summary>
/// <remarks>
/// Options attach themselves: this package declares the types the platform ships, and a package that owns another described thing declares its own
/// option against this collection. Nothing dispatches on a type's name with a switch; behavior is read off the option.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DescriptionTypeBase), typeof(IDescriptionType), typeof(DescriptionTypes))]
public abstract partial class DescriptionTypes : TypeCollectionBase<DescriptionTypeBase, IDescriptionType>
{
}
