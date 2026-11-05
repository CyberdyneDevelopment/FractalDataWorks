using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// A run was asked of a pipeline that is a template (FDW-3379).
/// </summary>
/// <remarks>
/// 20005 (ValidationFailed): which pipeline to run is the caller's input, so the answer is a 400 that names the pipeline and says what to do instead.
/// </remarks>
[TypeOption(typeof(EtlResultCodes), "PipelineIsTemplate", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PipelineIsTemplateCode : EtlResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="PipelineIsTemplateCode"/> class.</summary>
    public PipelineIsTemplateCode()
        : base(20005, "PipelineIsTemplate",
            ResultSeverities.ByName("Error"),
            "Pipeline '{PipelineName}' is a template and cannot be run. Make a pipeline from it with From template, and run that.",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the pipeline was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
