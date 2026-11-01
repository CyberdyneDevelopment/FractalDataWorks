using System.Collections.Generic;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// The working state of resolving the Compute stages of one run: what it resolves with, what it has compiled, and the
/// calculation limits, which are read from the settings once and only when a Compute stage is found.
/// </summary>
internal sealed class BuiltCalculationResolution
{
    public BuiltCalculationResolution(
        ITransformationConfigurationProvider? transformations,
        IEffectiveSettingsProvider? settings,
        ILogger logger)
    {
        Transformations = transformations;
        Settings = settings;
        Logger = logger;
    }

    public ITransformationConfigurationProvider? Transformations { get; }

    public IEffectiveSettingsProvider? Settings { get; }

    public ILogger Logger { get; }

    public Dictionary<System.Guid, ICompiledCalculation> Compiled { get; } = new();

    public Dictionary<System.Guid, ICompiledCalculationSet> CompiledSets { get; } = new();

    public SetLimits? SetLimits { get; set; }

    public CalculationLimits? Limits { get; set; }
}
