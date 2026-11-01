using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Etl.Abstractions.Thresholds;

/// <summary>
/// The level of thresholds on waiting pipeline runs the server, a pipeline or a schedule states, as the API states it (FDW-3161): its mode and, for the mode that states
/// thresholds, the thresholds. The mode and the thresholds are written together.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WaitThresholdLevelPayload
{
    /// <summary>Gets or sets the mode: a name of the type collection <c>WaitThresholdModes</c> (Inherit, Own or None).</summary>
    public string? Mode { get; set; }

    /// <summary>Gets or sets the thresholds; the mode that states none carries none, and the mode that states some carries at least one.</summary>
    public IList<WaitThresholdPayload> Thresholds { get; set; } = [];
}
