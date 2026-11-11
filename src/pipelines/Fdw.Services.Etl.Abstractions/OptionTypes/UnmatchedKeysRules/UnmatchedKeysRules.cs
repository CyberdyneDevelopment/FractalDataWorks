using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// What an update or delete destination does with a key that matched no row: Fail or Warn (FDW-4988).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(UnmatchedKeysRuleBase), typeof(IUnmatchedKeysRule), typeof(UnmatchedKeysRules))]
public abstract partial class UnmatchedKeysRules : TypeCollectionBase<UnmatchedKeysRuleBase, IUnmatchedKeysRule>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
