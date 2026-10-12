using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// A pipeline create or save was refused: the roles it would run under are not the caller's to give, or they
/// could not read every source or write every destination it names (FDW-1087).
/// </summary>
/// <remarks>
/// 20002 (ValidationFailed): the run roles and the sources and destinations are the caller's input, so the
/// answer is a 400 that says which role or which source and why.
/// </remarks>
[TypeOption(typeof(EtlResultCodes), "PipelineRunRefused", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PipelineRunRefusedCode : EtlResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="PipelineRunRefusedCode"/> class.</summary>
    public PipelineRunRefusedCode()
        : base(20002, "PipelineRunRefused",
            ResultSeverities.ByName("Error"),
            "Pipeline '{PipelineName}' cannot run as {RunRoles}: {Reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the pipeline was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
