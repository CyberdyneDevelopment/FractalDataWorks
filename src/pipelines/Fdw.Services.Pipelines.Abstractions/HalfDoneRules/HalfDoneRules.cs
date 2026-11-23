using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>
/// What happens to a run found half-done after its host restarted (FDW-6986, slice 4 of FDW-6141): the rule a pipeline states in <see cref="IHalfDoneRulePipelineConfiguration.HalfDoneRule"/>.
/// A plain collection, not a service type collection: a rule has no configuration row. One option in 1.6.0, <c>End</c>; starting a run again is its own story after 1.6.0, because a run carried
/// out at once keeps no request to start again from.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(HalfDoneRuleBase), typeof(IHalfDoneRule), typeof(HalfDoneRules))]
public abstract partial class HalfDoneRules : TypeCollectionBase<HalfDoneRuleBase, IHalfDoneRule>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
