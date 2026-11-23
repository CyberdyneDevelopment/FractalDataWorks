namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>
/// A pipeline implementation that states the most seconds one of its runs may take (FDW-6141): a reader asks for the fact without asking what the engine is, as it does
/// for <c>IApprovalGatedPipelineConfiguration</c>.
/// </summary>
public interface IRunTimeLimitPipelineConfiguration
{
    /// <summary>Gets the most seconds a run may take; <see langword="null"/> when the pipeline states none.</summary>
    int? MaxRunSeconds { get; }
}
