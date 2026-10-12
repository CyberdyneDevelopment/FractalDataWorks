using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A read succeeded but carried no workspace, after existence had already been established.
/// </summary>
/// <remarks>
/// This is an internal inconsistency, not a missing row — a caller asking for something that does
/// not exist gets a not-found path instead. It exists so that case fails loudly with its own code
/// rather than being absorbed by an empty list or a substituted record. HTTP 500.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceLoadReturnedNoValue", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceLoadReturnedNoValueCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceLoadReturnedNoValueCode"/> class.</summary>
    public WorkspaceLoadReturnedNoValueCode()
        : base(90000, "WorkspaceLoadReturnedNoValue",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' reported success but returned no value",
            isRetryable: false)
    {
    }
}
