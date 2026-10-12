using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Failed to create the session store directory.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "StoreDirectoryCreationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StoreDirectoryCreationFailedCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoreDirectoryCreationFailedCode"/> class.
    /// </summary>
    public StoreDirectoryCreationFailedCode()
        : base(71005, "StoreDirectoryCreationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to create session store directory '{Path}': {ErrorMessage}",
            isRetryable: true)
    {
    }
}