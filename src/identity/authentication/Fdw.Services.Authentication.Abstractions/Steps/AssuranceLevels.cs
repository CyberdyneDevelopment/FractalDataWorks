using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// The assurance levels a deployment recognises, each reached by a number of distinct factor kinds.
/// </summary>
/// <remarks>
/// FDW declares no levels. The platform owns the mechanism (count the kinds of factor proved, report the
/// highest level they reach, compare two levels by what they demand); the deployment owns the values, by
/// referencing a package that declares them. A host that declares none reports no <c>acr</c>, and a flow
/// that demands a level nobody declared is refused as misconfigured (FDW-954).
/// </remarks>
[TypeCollection(typeof(AssuranceLevelBase), typeof(IAssuranceLevel), typeof(AssuranceLevels))]
public abstract partial class AssuranceLevels : TypeCollectionBase<AssuranceLevelBase, IAssuranceLevel>
{
}
