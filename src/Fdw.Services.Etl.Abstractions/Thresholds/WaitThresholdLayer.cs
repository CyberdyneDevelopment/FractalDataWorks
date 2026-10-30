using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Abstractions.Thresholds;

/// <summary>
/// What one level (the server, a pipeline or a schedule) states about thresholds on waiting runs (FDW-3161): its mode and, when the mode says it states
/// thresholds of its own, the thresholds.
/// </summary>
/// <param name="Owner">Who the level is: the server, or a pipeline or a schedule by name.</param>
/// <param name="Mode">Whether the level inherits, states thresholds of its own, or states none.</param>
/// <param name="Thresholds">The thresholds the level states; empty unless its mode says it states some.</param>
public sealed record WaitThresholdLayer(string Owner, IWaitThresholdMode Mode, IReadOnlyList<WaitThreshold> Thresholds);
