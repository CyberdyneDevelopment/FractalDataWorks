using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Abstractions.Thresholds;

/// <summary>
/// What one level (the server, a pipeline or a schedule) states about thresholds on waiting runs (FDW-3161): its mode. The thresholds themselves are attached to the owner
/// as threshold types (PipelineWaits, ScheduleWaits, ServerWaits; FDW-3975); the mode says which owner's attached thresholds apply to a run.
/// </summary>
/// <param name="Owner">Who the level is: the server, or a pipeline or a schedule by name.</param>
/// <param name="Mode">Whether the level inherits, applies the thresholds attached to its owner, or applies none.</param>
public sealed record WaitThresholdLayer(string Owner, IWaitThresholdMode Mode);
