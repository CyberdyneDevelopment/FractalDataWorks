using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// How a BatchCopy pipeline without load steps writes into its destination: Append, Replace or Upsert (FDW-1108).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(LoadModeBase), typeof(ILoadMode), typeof(LoadModes))]
public abstract partial class LoadModes : TypeCollectionBase<LoadModeBase, ILoadMode>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
