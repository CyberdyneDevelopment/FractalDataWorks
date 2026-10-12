using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>What a load row is, for the problems found in it: who it is, the stage they are placed on, and where its options sit in the body.</summary>
/// <param name="Row">The load row.</param>
/// <param name="Owner">What a problem says the row is.</param>
/// <param name="Stage">The stage a problem is placed on: the step's name, or <see langword="null"/> for the pipeline's own load.</param>
/// <param name="Prefix">The body field the row's options sit under.</param>
/// <param name="Executes">Whether the row is an Execute step, which names a container.</param>
/// <param name="DestinationName">The data set the row writes to.</param>
public sealed record LoadRowContext(
    PipelineLoadStepConfiguration Row,
    string Owner,
    string? Stage,
    string Prefix,
    bool Executes,
    string? DestinationName)
{
    /// <summary>Describes a load row.</summary>
    /// <param name="row">The load row.</param>
    /// <param name="pipelineDestinationDataSet">The pipeline's destination data set, which its own load writes to.</param>
    /// <returns>The row's context.</returns>
    public static LoadRowContext Of(PipelineLoadStepConfiguration row, string? pipelineDestinationDataSet) =>
        row.StepOrder is null
            ? new LoadRowContext(row, "the pipeline's own load", null, "LoadOptions", false, pipelineDestinationDataSet)
            : new LoadRowContext(row, $"{StepLabel(row)} '{row.Name}'", row.Name, "Options", !string.IsNullOrWhiteSpace(row.DestinationContainerName), row.DestinationDataSet);

    // A reject step is a load step by every rule but one, and a problem says which of the two it is about.
    private static string StepLabel(PipelineLoadStepConfiguration row) =>
        LoadPhases.ByName(row.Phase).ReadsRejectedRecords ? "reject step" : "load step";
}
