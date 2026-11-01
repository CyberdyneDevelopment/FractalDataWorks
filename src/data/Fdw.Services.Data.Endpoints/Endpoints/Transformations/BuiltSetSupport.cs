using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Transformations;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Calculations.Sets;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Turns the table inputs, derived values and table outputs a request states for a Built transformation into the configuration
/// that is stored, refusing it first when a name is wrong, a formula cannot be built, an output reads a name it cannot see, or
/// the engine refuses the set against the platform's limits.
/// </summary>
/// <remarks>
/// The same compiler checks a set here as the one that compiles it when a pipeline runs, against the same limit settings, so
/// a definition that saves is one a run can compile.
/// </remarks>
internal static class BuiltSetSupport
{
    internal static IGenericResult<BuiltTransformationConfiguration> Prepare(
        IEffectiveSettingsProvider settings,
        string name,
        string? description,
        IList<TransformationInputPayload> inputs,
        IList<TableInputPayload> tables,
        IList<DerivedValuePayload> derived,
        IList<TableOutputPayload> outputs,
        ILogger logger)
    {
        var limits = CalculationLimitSettings.Read(settings, logger);
        if (limits.IsFailure)
        {
            return limits.ToNewResult<BuiltTransformationConfiguration>();
        }

        var setLimits = CalculationLimitSettings.ReadSet(settings, logger);
        if (setLimits.IsFailure)
        {
            return setLimits.ToNewResult<BuiltTransformationConfiguration>();
        }

        var scalars = BuiltTransformationSupport.Inputs(inputs, logger);
        if (scalars.IsFailure)
        {
            return scalars.ToNewResult<BuiltTransformationConfiguration>();
        }

        var definition = BuiltSetMapper.ToDefinition(name, tables, derived, outputs, limits.Value!.MaxDepth, logger);
        if (definition.IsFailure)
        {
            return definition.ToNewResult<BuiltTransformationConfiguration>();
        }

        return Build(name, description, scalars.Value!, definition.Value!, limits.Value!, setLimits.Value!, logger);
    }

    private static IGenericResult<BuiltTransformationConfiguration> Build(
        string name,
        string? description,
        IReadOnlyList<BuiltTransformationInputConfiguration> scalars,
        CalculationSetDefinition definition,
        CalculationLimits limits,
        SetLimits setLimits,
        ILogger logger)
    {
        var visible = BuiltSetVisibility.Check(definition, scalars.Select(scalar => scalar.Name), logger);
        if (visible.IsFailure)
        {
            return visible.ToNewResult<BuiltTransformationConfiguration>();
        }

        var compiled = new CalculationSetCompiler().Compile(definition, limits, setLimits);
        if (compiled.IsFailure)
        {
            return compiled.ToNewResult<BuiltTransformationConfiguration>();
        }

        var built = new BuiltTransformationConfiguration
        {
            Name = name,
            Description = description,
            Implementation = BuiltTransformationConfiguration.ImplementationName,
        };
        foreach (var scalar in scalars)
        {
            built.Inputs.Add(scalar);
        }

        var written = BuiltTransformationSetWriter.Apply(built, definition);
        return written.IsFailure
            ? written.ToNewResult<BuiltTransformationConfiguration>()
            : GenericResult<BuiltTransformationConfiguration>.Success(built);
    }
}
