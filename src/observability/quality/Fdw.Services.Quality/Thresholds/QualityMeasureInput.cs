using System;
using System.Collections.Generic;

namespace Fdw.Services.Quality.Thresholds;

/// <summary>What a quality threshold type takes its measure from (FDW-3935).</summary>
/// <param name="ThresholdName">The threshold, for a failure message.</param>
/// <param name="Parameters">The parameters the threshold states, by name (see <c>ThresholdParameterReader</c>).</param>
/// <param name="Rows">The rows of the data set, each by field name.</param>
/// <param name="RegexTimeout">How long one pattern match may take. Stated by the caller from configuration; a pattern that takes longer counts as not matching.</param>
public sealed record QualityMeasureInput(
    string ThresholdName, IReadOnlyDictionary<string, object?> Parameters, IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows, TimeSpan RegexTimeout);
