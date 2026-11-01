using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// The load definition a create or an update states (its load mode, load steps, reject steps, value sources or run
/// parameters) was refused (FDW-1147).
/// </summary>
/// <remarks>
/// Why 21003 and a code of its own: the refusal is the caller's own input, so the answer is a 400 that names the reason, as
/// <c>InvalidPipelineStep</c> (20001) does for a stage. A refusal logged as a plain message reached the caller as the generic
/// "The request was invalid" because <c>ResultHttpStatusMapper</c> names a reason only for a result whose own code opted in.
/// It follows <c>RejectedRecordReservedColumn</c> (21002) in this collection's further validation codes.
/// </remarks>
[TypeOption(typeof(EtlResultCodes), "InvalidPipelineLoad", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidPipelineLoadCode : EtlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPipelineLoadCode"/> class.
    /// </summary>
    public InvalidPipelineLoadCode()
        : base(21003, "InvalidPipelineLoad",
            ResultSeverities.ByName("Error"),
            "The load definition is refused: {Reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
