using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// TypeCollection of the modes in which a save cascades to a child collection that hangs from the owner's durable Id.
/// <c>[ConfigurationChildCascade(typeof(...))]</c> names one of its option types, because an attribute argument must be a constant.
/// PocoMapperGenerator, which cannot reference this collection, reads the option type by its full name:
/// <c>Fdw.Data.AuthoritativeChildCascade</c> or <c>Fdw.Data.UpsertOnlyChildCascade</c>.
/// </summary>
[TypeCollection(typeof(ChildCascadeModeBase), typeof(IChildCascadeMode), typeof(ChildCascadeModes))]
[ExcludeFromCodeCoverage]
public abstract partial class ChildCascadeModes : TypeCollectionBase<ChildCascadeModeBase, IChildCascadeMode>
{
}
