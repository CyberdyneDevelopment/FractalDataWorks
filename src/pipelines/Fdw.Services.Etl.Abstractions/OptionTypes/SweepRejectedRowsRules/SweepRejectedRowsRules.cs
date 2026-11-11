using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// What an absent-key soft delete does when rows were rejected upstream of its destination: Refuse or Proceed (FDW-4956).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SweepRejectedRowsRuleBase), typeof(ISweepRejectedRowsRule), typeof(SweepRejectedRowsRules))]
public abstract partial class SweepRejectedRowsRules : TypeCollectionBase<SweepRejectedRowsRuleBase, ISweepRejectedRowsRule>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
