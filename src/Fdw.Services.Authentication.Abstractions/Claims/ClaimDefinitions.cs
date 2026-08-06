using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// TypeCollection of FDW JWT claim types. The source generator emits a static accessor per
/// <c>[TypeOption]</c> plus <c>All()</c>, <c>ByName(name)</c>, and <c>ById(id)</c>. Each claim
/// carries its own baking metadata (<see cref="ClaimDefinitionBase.IsArray"/>,
/// <see cref="ClaimDefinitionBase.Destinations"/>) so the claim-baking pipeline is generic — a downstream
/// assembly adds a claim by declaring a new <c>[TypeOption]</c>, with no change to FDW.
/// </summary>
[TypeCollection(typeof(ClaimDefinitionBase), typeof(IClaimDefinition), typeof(ClaimDefinitions))]
public abstract partial class ClaimDefinitions : TypeCollectionBase<ClaimDefinitionBase, IClaimDefinition>
{
}
