using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// A pipeline step in a create or update request was refused: its operation type is unknown, or its
/// parameters are missing or unusable for that operation.
/// </summary>
/// <remarks>
/// 20001 (InvalidInput): the step is the caller's input, so the answer is a 400 carrying the step's name
/// and the reason, never a 500. The reason is the message the step's own option logged when it refused.
/// </remarks>
[TypeOption(typeof(EtlResultCodes), "InvalidPipelineStep", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidPipelineStepCode : EtlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPipelineStepCode"/> class.
    /// </summary>
    public InvalidPipelineStepCode()
        : base(20001, "InvalidPipelineStep",
            ResultSeverities.ByName("Error"),
            "Pipeline step '{StepName}' is invalid: {Reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
