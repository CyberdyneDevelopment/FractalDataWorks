using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Target folder is required for a project move operation.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "TargetFolderRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TargetFolderRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TargetFolderRequiredCode"/> class.
    /// </summary>
    public TargetFolderRequiredCode()
        : base(21014, "TargetFolderRequired",
            ResultSeverities.ByName("Error"),
            "Target folder is required for project move: {ProjectName}",
            isRetryable: false)
    {
    }
}
