using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Etl.Abstractions.Thresholds;

/// <summary>
/// The level of thresholds on waiting pipeline runs the server, a pipeline or a schedule states, as the API states it (FDW-3161): its mode. The thresholds are attached to
/// the owner as threshold types (PipelineWaits, ScheduleWaits, ServerWaits) through the threshold routes (FDW-3975).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WaitThresholdLevelPayload
{
    /// <summary>Gets or sets the mode: a name of the type collection <c>WaitThresholdModes</c> (Inherit, Own or None).</summary>
    public string? Mode { get; set; }
}
