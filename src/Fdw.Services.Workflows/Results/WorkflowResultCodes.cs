using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workflows.Results;

/// <summary>
/// TypeCollection of workflow result codes.
/// EventId range: 7850-7869
/// </summary>
[TypeCollection(typeof(WorkflowResultCodeBase), typeof(IResultCode), typeof(WorkflowResultCodes))]
public abstract partial class WorkflowResultCodes : TypeCollectionBase<WorkflowResultCodeBase, IResultCode>
{
}
