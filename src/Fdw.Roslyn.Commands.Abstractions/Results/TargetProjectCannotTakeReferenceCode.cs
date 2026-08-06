using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// A moved document requires a reference the target project cannot legally take.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "TargetProjectCannotTakeReference", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TargetProjectCannotTakeReferenceCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TargetProjectCannotTakeReferenceCode"/> class.
    /// </summary>
    public TargetProjectCannotTakeReferenceCode()
        : base(31022, "TargetProjectCannotTakeReference",
            ResultSeverities.ByName("Error"),
            "Target project '{TargetProject}' cannot take required reference '{RequiredReference}': {Reason}",
            isRetryable: false)
    {
    }
}
