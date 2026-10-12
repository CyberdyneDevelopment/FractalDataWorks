using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// TypeCollection for Workspace result codes.
/// EventId range: 5800-5899 (Workspace domain)
/// </summary>
[TypeCollection(typeof(WorkspaceResultCodeBase), typeof(IResultCode), typeof(WorkspaceResultCodes))]
public abstract partial class WorkspaceResultCodes : TypeCollectionBase<WorkspaceResultCodeBase, IResultCode>
{
}