using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Reads the limits on a calculation (how many results, pieces and levels) from the platform's settings.
/// </summary>
/// <remarks>
/// One reader for every place that checks or runs a calculation, so a run and a save refuse the same formulas. A limit that
/// is not set to a positive number is a refusal that names the setting, never a made-up value.
/// </remarks>
public static class CalculationLimitSettings
{
    /// <summary>Reads the two limits on the rows of a calculation set.</summary>
    /// <param name="settings">The settings the limits are resolved from.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The limits, or the refusal naming the first setting that is not a positive number.</returns>
    public static IGenericResult<SetLimits> ReadSet(IEffectiveSettingsProvider settings, ILogger logger)
    {
        var rows = settings.GetEffectiveValue<int>(SettingDefinitions.MaxSetOutputRows);
        var lookups = settings.GetEffectiveValue<int>(SettingDefinitions.MaxSetLookupRows);
        var missing = rows <= 0 ? SettingDefinitions.MaxSetOutputRows : lookups <= 0 ? SettingDefinitions.MaxSetLookupRows : null;
        return missing is not null
            ? GenericResult<SetLimits>.Failure(ComputeLog.LimitSettingMissing(logger, missing))
            : GenericResult<SetLimits>.Success(new SetLimits(rows, lookups));
    }

    /// <summary>Reads the three limit settings.</summary>
    /// <param name="settings">The settings the limits are resolved from.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The limits, or the refusal naming the first setting that is not a positive number.</returns>
    public static IGenericResult<CalculationLimits> Read(IEffectiveSettingsProvider settings, ILogger logger)
    {
        var results = settings.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationResults);
        var nodes = settings.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationNodes);
        var depth = settings.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationDepth);
        var missing = results <= 0 ? SettingDefinitions.MaxCalculationResults
            : nodes <= 0 ? SettingDefinitions.MaxCalculationNodes
            : depth <= 0 ? SettingDefinitions.MaxCalculationDepth
            : null;
        return missing is not null
            ? GenericResult<CalculationLimits>.Failure(ComputeLog.LimitSettingMissing(logger, missing))
            : GenericResult<CalculationLimits>.Success(new CalculationLimits(results, nodes, depth));
    }
}
