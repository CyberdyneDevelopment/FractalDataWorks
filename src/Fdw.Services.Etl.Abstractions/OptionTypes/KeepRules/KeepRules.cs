using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Which row of a key's group a LatestBy transform keeps: First or Last by the order column (FDW-1109).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(KeepRuleBase), typeof(IKeepRule), typeof(KeepRules))]
public abstract partial class KeepRules : TypeCollectionBase<KeepRuleBase, IKeepRule>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
