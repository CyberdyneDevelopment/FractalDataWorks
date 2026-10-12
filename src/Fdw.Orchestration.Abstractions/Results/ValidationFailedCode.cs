using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Orchestration.Abstractions.Results;

/// <summary>
/// Orchestration validation failed.
/// </summary>
[TypeOption(typeof(OrchestrationResultCodes), "ValidationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ValidationFailedCode : OrchestrationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationFailedCode"/> class.
    /// </summary>
    public ValidationFailedCode()
        : base(20002, "ValidationFailed",
            ResultSeverities.ByName("Error"),
            "Orchestration validation failed: {ValidationMessage}",
            isRetryable: false)
    {
    }
}