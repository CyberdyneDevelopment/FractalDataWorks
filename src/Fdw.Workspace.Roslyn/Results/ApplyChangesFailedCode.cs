using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// One or more documents could not be written during ApplyChanges.
/// Detail object carries WrittenCount, ErrorCount, and a concatenated Errors string.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "ApplyChangesFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ApplyChangesFailedCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplyChangesFailedCode"/> class.
    /// </summary>
    public ApplyChangesFailedCode()
        : base(70002, "ApplyChangesFailed",
            ResultSeverities.ByName("Error"),
            "One or more documents could not be written during ApplyChanges. Inspect the Errors detail for per-file failure messages.",
            isRetryable: true)
    {
    }
}
