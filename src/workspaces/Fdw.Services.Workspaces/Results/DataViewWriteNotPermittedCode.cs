using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>The caller may read this data view but is not its owner, so it cannot change or delete it.</summary>
/// <remarks>Category 10 (Forbidden), like <see cref="WorkspaceWriteNotPermittedCode"/>. A data view belongs to the user who made it (FDW-1032). HTTP 403.</remarks>
[TypeOption(typeof(WorkspacesResultCodes), "DataViewWriteNotPermitted", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataViewWriteNotPermittedCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataViewWriteNotPermittedCode"/> class.</summary>
    public DataViewWriteNotPermittedCode()
        : base(100007, "DataViewWriteNotPermitted",
            ResultSeverities.ByName("Error"),
            "Data view '{name}' cannot be changed by this caller: {reason}",
            isRetryable: false)
    {
    }
}
