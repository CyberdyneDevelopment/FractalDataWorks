namespace Fdw.Services.Etl;

/// <summary>
/// A pipeline implementation that states how many of its runs may run at once and what happens to a run triggered past that (FDW-3117): a
/// reader asks for the fact without asking what the engine is, as it does for <see cref="IApprovalGatedPipelineConfiguration"/>.
/// </summary>
public interface IParallelRunPipelineConfiguration
{
    /// <summary>Gets how many runs of the pipeline may run at once.</summary>
    int MaxParallelRuns { get; }

    /// <summary>Gets the name of the option of <c>OverflowBehaviors</c> that says what happens to a run triggered while that many are active.</summary>
    string OverflowBehavior { get; }

    /// <summary>Gets the seconds a held run waits before it joins the queue, for the <c>DelayedStart</c> behavior with a delay; otherwise <see langword="null"/> (FDW-3129).</summary>
    int? DelayedStartSeconds { get; }

    /// <summary>Gets the time of day (<c>HH:mm</c>) a held run joins the queue, for the <c>DelayedStart</c> behavior with a time of day; otherwise <see langword="null"/> (FDW-3129).</summary>
    string? DelayedStartTimeOfDay { get; }

    /// <summary>Gets the time zone <see cref="DelayedStartTimeOfDay"/> is read in; otherwise <see langword="null"/> (FDW-3129).</summary>
    string? DelayedStartTimeZoneId { get; }
}
