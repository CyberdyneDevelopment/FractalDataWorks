namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>
/// A pipeline implementation that states what happens to one of its runs found half-done after a restart (FDW-6141): a reader asks for the fact without asking what the
/// engine is, as the layer does for the facts it owns.
/// </summary>
public interface IHalfDoneRulePipelineConfiguration
{
    /// <summary>Gets the name of the option of <c>HalfDoneRules</c> that says what happens to a run found half-done; <see langword="null"/> when the pipeline states none.</summary>
    string? HalfDoneRule { get; }
}
