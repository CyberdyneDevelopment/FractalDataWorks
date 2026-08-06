using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Failed to get semantic model.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "FailedToGetSemanticModel", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FailedToGetSemanticModelCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedToGetSemanticModelCode"/> class.
    /// </summary>
    public FailedToGetSemanticModelCode()
        : base(91005, "FailedToGetSemanticModel",
            ResultSeverities.ByName("Error"),
            "Failed to get semantic model",
            isRetryable: false)
    {
    }
}
