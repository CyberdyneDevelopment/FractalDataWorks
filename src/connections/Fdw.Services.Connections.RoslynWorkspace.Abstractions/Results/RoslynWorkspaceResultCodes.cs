using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions.Results;

/// <summary>
/// TypeCollection for RoslynWorkspace connection result codes.
/// Result codes use categorized numbers (prefix RW): InvalidSymbolId=20001, SolutionFileNotFound=30000, SymbolNotFound=31000, ModeRequiresLive=40000, SolutionPathNotConfigured=60000, WorkspaceLoadFailed=70000.
/// </summary>
[TypeCollection(typeof(RoslynWorkspaceResultCodeBase), typeof(IResultCode), typeof(RoslynWorkspaceResultCodes))]
public abstract partial class RoslynWorkspaceResultCodes : TypeCollectionBase<RoslynWorkspaceResultCodeBase, IResultCode>
{
}
