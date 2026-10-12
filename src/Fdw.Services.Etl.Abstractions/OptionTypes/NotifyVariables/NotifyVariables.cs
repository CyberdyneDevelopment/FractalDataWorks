using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// The variables a Notify message may use: PipelineName, ExecutionId, RowsExtracted, RowsRejected and RowsLoaded (FDW-1120).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(NotifyVariableBase), typeof(INotifyVariable), typeof(NotifyVariables))]
public abstract partial class NotifyVariables : TypeCollectionBase<NotifyVariableBase, INotifyVariable>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
