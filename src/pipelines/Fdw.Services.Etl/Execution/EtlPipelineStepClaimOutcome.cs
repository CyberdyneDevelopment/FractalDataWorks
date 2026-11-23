using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution;

/// <summary>What a claim's row is given when the step that was claimed ends (FDW-6139): how it ended, and nothing else of the row.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineStepClaimOutcome
{
    /// <summary>Gets or sets how the step ended, one of <c>StepClaimOutcomes</c>.</summary>
    public string Outcome { get; set; } = string.Empty;
}
