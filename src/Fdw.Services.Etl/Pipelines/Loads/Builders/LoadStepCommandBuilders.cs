using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Pipelines.Loads.Builders;

/// <summary>
/// Builds the data commands for each <c>LoadStepKinds</c> kind: BulkInsert, BulkUpsert, InsertFromQuery, Update, Delete,
/// Truncate and Execute (FDW-1109).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(LoadStepCommandBuilderBase), typeof(ILoadStepCommandBuilder), typeof(LoadStepCommandBuilders))]
public abstract partial class LoadStepCommandBuilders : TypeCollectionBase<LoadStepCommandBuilderBase, ILoadStepCommandBuilder>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
