using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// What one load step of a BatchCopy pipeline does: BulkInsert, BulkUpsert, InsertFromQuery, Update, Delete, Truncate or Execute (FDW-1108).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(LoadStepKindBase), typeof(ILoadStepKind), typeof(LoadStepKinds))]
public abstract partial class LoadStepKinds : TypeCollectionBase<LoadStepKindBase, ILoadStepKind>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
