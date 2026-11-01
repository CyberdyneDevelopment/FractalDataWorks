using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>Result codes raised by the workspaces domain.</summary>
[TypeCollection(typeof(WorkspacesResultCodeBase), typeof(IResultCode), typeof(WorkspacesResultCodes))]
public abstract partial class WorkspacesResultCodes : TypeCollectionBase<WorkspacesResultCodeBase, IResultCode>
{
}
