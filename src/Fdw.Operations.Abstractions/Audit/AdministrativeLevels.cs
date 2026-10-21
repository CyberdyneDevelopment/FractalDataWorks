using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Audit;

/// <summary>
/// The levels at which a change is administered: Platform, Tenant and Organization.
/// </summary>
[TypeCollection(typeof(AdministrativeLevelBase), typeof(IAdministrativeLevel), typeof(AdministrativeLevels))]
[ExcludeFromCodeCoverage]
public abstract partial class AdministrativeLevels : TypeCollectionBase<AdministrativeLevelBase, IAdministrativeLevel>
{
}
