using Fdw.Messages;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Pipelines.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The relation between a body's <c>truncateBeforeLoad</c> and its load mode (FDW-1110): the load mode is the one
/// definition and Replace means truncate, then load. <c>truncateBeforeLoad</c> stays on the body only as a consistency
/// check and is read back derived from the load mode.
/// </summary>
/// <remarks>
/// Why only <c>true</c> can disagree: a create body's flag is a plain bool, so "absent" and "false" are the same value, and
/// a Replace body that leaves it out must be accepted. The engine's load plan refuses the same single contradiction (the
/// flag set beside a load mode that does not truncate first, or beside load steps) and takes the truncation from the load
/// mode alone, so false never contradicts anything.
/// </remarks>
public static class PipelineTruncateConsistency
{
    /// <summary>Whether a load mode truncates the destination first: true exactly for the modes that say so (Replace).</summary>
    /// <param name="loadMode">The load mode name, or <see langword="null"/> when the pipeline states load steps.</param>
    /// <returns>Whether the load truncates first.</returns>
    public static bool Derive(string? loadMode)
    {
        if (string.IsNullOrWhiteSpace(loadMode))
            return false;
        var mode = LoadModes.ByName(loadMode);
        return !ReferenceEquals(mode, LoadModes.NotFound) && mode.TruncatesFirst;
    }

    /// <summary>Checks a stated <c>truncateBeforeLoad</c> against the load mode the pipeline will have: true needs a load mode that truncates first.</summary>
    /// <param name="stated">The value the body states, or <see langword="null"/> when it states none.</param>
    /// <param name="loadMode">The load mode the saved pipeline will have, or <see langword="null"/> when it states load steps.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The refusal, or <see langword="null"/> when the value does not contradict the load mode.</returns>
    public static IGenericMessage? Check(bool? stated, string? loadMode, ILogger logger) =>
        stated is true && !Derive(loadMode)
            ? PipelineEndpointLog.TruncateDisagreesWithLoadMode(logger, true, string.IsNullOrWhiteSpace(loadMode) ? "load steps" : loadMode)
            : null;
}
