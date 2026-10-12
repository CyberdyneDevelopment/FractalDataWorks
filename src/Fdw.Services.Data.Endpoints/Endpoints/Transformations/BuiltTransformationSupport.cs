using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Transformations;
using Fdw.Data.Transformations.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Turns the definition a request states for a Built transformation into the configuration that is stored, refusing it
/// first when an input is wrong, a formula cannot be built or checked, or a formula reads a field no input declares.
/// </summary>
/// <remarks>
/// The same engine checks a formula here as the one that compiles it when a pipeline runs, against the same limit settings,
/// so a definition that saves is one a run can compile.
/// </remarks>
internal static class BuiltTransformationSupport
{
    private const string NumberType = "Number";
    private const string FlagType = "Flag";

    /// <summary>Checks and builds the configuration for a request.</summary>
    /// <param name="settings">Supplies the limits on a calculation.</param>
    /// <param name="name">The transformation's name.</param>
    /// <param name="description">The description, or null.</param>
    /// <param name="inputs">The declared inputs the request states.</param>
    /// <param name="results">The results the request states.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The configuration ready to save, or the first refusal.</returns>
    internal static IGenericResult<BuiltTransformationConfiguration> Prepare(
        IEffectiveSettingsProvider settings,
        string name,
        string? description,
        IList<TransformationInputPayload> inputs,
        IList<BuiltResultPayload> results,
        ILogger logger)
    {
        var limits = CalculationLimitSettings.Read(settings, logger);
        if (limits.IsFailure)
        {
            return limits.ToNewResult<BuiltTransformationConfiguration>();
        }

        var declared = Inputs(inputs, logger);
        if (declared.IsFailure)
        {
            return declared.ToNewResult<BuiltTransformationConfiguration>();
        }

        var array = BuildArray(name, results, limits.Value!, logger);
        if (array.IsFailure)
        {
            return array.ToNewResult<BuiltTransformationConfiguration>();
        }

        var undeclared = UndeclaredField(array.Value!, declared.Value!, logger);
        if (undeclared.IsFailure)
        {
            return undeclared.ToNewResult<BuiltTransformationConfiguration>();
        }

        var built = BuiltTransformationConverter.ToConfiguration(name, array.Value!, declared.Value!);
        built.Description = description;
        return GenericResult<BuiltTransformationConfiguration>.Success(built);
    }

    internal static IGenericResult<IReadOnlyList<BuiltTransformationInputConfiguration>> Inputs(
        IList<TransformationInputPayload> inputs, ILogger logger)
    {
        var declared = new List<BuiltTransformationInputConfiguration>(inputs.Count);
        foreach (var input in inputs)
        {
            var problem = InputProblem(input, declared, logger);
            if (problem is not null)
            {
                return GenericResult<IReadOnlyList<BuiltTransformationInputConfiguration>>.Failure(problem);
            }

            declared.Add(new BuiltTransformationInputConfiguration { Name = input.Name, ValueType = input.ValueType, ExecutionOrder = declared.Count });
        }

        return GenericResult<IReadOnlyList<BuiltTransformationInputConfiguration>>.Success(declared);
    }

    private static IGenericMessage? InputProblem(
        TransformationInputPayload? input, List<BuiltTransformationInputConfiguration> declared, ILogger logger)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.Name))
        {
            return BuiltTransformationEndpointLog.InputBlank(logger);
        }

        if (declared.Any(candidate => string.Equals(candidate.Name, input.Name, StringComparison.Ordinal)))
        {
            return BuiltTransformationEndpointLog.InputRepeated(logger, input.Name);
        }

        return string.Equals(input.ValueType, NumberType, StringComparison.Ordinal) || string.Equals(input.ValueType, FlagType, StringComparison.Ordinal)
            ? null
            : BuiltTransformationEndpointLog.InputTypeUnknown(logger, input.Name, input.ValueType);
    }

    // The engine's array for the request, mapped within the depth limit and then checked by the engine's own validator.
    private static IGenericResult<CalculationArrayDefinition> BuildArray(
        string name, IList<BuiltResultPayload> results, CalculationLimits limits, ILogger logger)
    {
        var definitions = new List<CalculationResultDefinition>(results.Count);
        foreach (var result in results)
        {
            var tree = BuiltFormulaMapper.ToNode(result.Name, result.Formula, limits.MaxDepth, logger);
            if (tree.IsFailure)
            {
                return tree.ToNewResult<CalculationArrayDefinition>();
            }

            definitions.Add(new CalculationResultDefinition(result.Name, tree.Value!, result.IsKept));
        }

        var array = new CalculationArrayDefinition(name, definitions);
        var checkedArray = CalculationArrayValidator.Validate(array, limits);
        return checkedArray.IsFailure
            ? checkedArray.ToNewResult<CalculationArrayDefinition>()
            : GenericResult<CalculationArrayDefinition>.Success(array);
    }

    // A field a formula reads has to be an input, or a run would hand it nothing.
    private static IGenericResult UndeclaredField(
        CalculationArrayDefinition array, IReadOnlyList<BuiltTransformationInputConfiguration> declared, ILogger logger)
    {
        foreach (var result in array.Results)
        {
            var read = new List<string>();
            BuiltFormulaFields.Collect(result.Expression, read);
            var missing = read.FirstOrDefault(field => !declared.Any(input => string.Equals(input.Name, field, StringComparison.Ordinal)));
            if (missing is not null)
            {
                return GenericResult.Failure(BuiltTransformationEndpointLog.FieldNotDeclared(logger, result.Name, missing));
            }
        }

        return GenericResult.Success();
    }
}
