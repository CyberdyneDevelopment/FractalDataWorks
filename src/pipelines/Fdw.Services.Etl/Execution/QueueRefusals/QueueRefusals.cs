using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The refusals the run queue gives to a run whose caller opened its execution: a full queue, stopped runs, and a refusal the queue could not record.
/// </summary>
/// <remarks>A failure message is read back by its number through <c>ByNumber</c>; a number that matches none gets <c>NotFound</c>, which means the failure is not a refusal of the queue, and the caller says so.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(QueueRefusalBase), typeof(IQueueRefusal), typeof(QueueRefusals))]
public abstract partial class QueueRefusals : TypeCollectionBase<QueueRefusalBase, IQueueRefusal>
{
}
