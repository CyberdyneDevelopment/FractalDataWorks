using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// What a Branch stage does with a record that matches none of its paths: Pass or Reject (FDW-1120).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(UnmatchedActionBase), typeof(IUnmatchedAction), typeof(UnmatchedActions))]
public abstract partial class UnmatchedActions : TypeCollectionBase<UnmatchedActionBase, IUnmatchedAction>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
