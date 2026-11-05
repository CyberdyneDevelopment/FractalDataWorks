using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>
/// What a schedule does with a cron fire time that passed while the scheduler was down (FDW-3788): Skip it, or FireOnce.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(MissedFireRuleBase), typeof(IMissedFireRule), typeof(MissedFireRules))]
public abstract partial class MissedFireRules : TypeCollectionBase<MissedFireRuleBase, IMissedFireRule>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
