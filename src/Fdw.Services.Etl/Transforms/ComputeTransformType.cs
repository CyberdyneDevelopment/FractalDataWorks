using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;
using OptionTransformTypes = Fdw.Services.Etl.Abstractions.OptionTypes.TransformTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Stage that applies a Built transformation to each record: it reads the inputs the transformation declares from the fields
/// the stage maps to them, runs the compiled calculation, and adds the transformation's kept results to the record as columns.
/// </summary>
/// <remarks>
/// Why the stage only runs what the run resolved: naming a Transformation, turning it into a calculation and compiling it
/// belongs to <see cref="BuiltCalculationResolver"/>, which does it once when the run starts and hands the result to the stage
/// through <see cref="ITransformContext.BuiltCalculations"/>. The stage never reaches the Transformation domain per record.
/// A record whose calculation fails is reported with the calculation's own coded result and skipped; what a failing record
/// should cause beyond that is not decided here.
/// </remarks>
[TypeOption(typeof(OptionTransformTypes), "Compute")]
public sealed class ComputeTransformType : TransformTypeBase
{
    private const string Kind = "Compute";

    private static readonly StageMember[] OwnedMembers =
    [
        new("compute", "ComputeSpec", true, "The Built transformation applied to each record, and which field of the record feeds each input it declares")
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputeTransformType"/> class.
    /// </summary>
    public ComputeTransformType() : base(
        name: Kind,
        displayName: "Compute",
        description: "Adds the results of a Built transformation to each record",
        category: "Transform",
        modifiesStructure: true,
        canFilterRecords: false,
        inputPorts: ["input"],
        outputPorts: ["output"])
    {
    }

    /// <inheritdoc />
    public override IReadOnlyList<StageMember> Members => OwnedMembers;

    /// <inheritdoc />
    public override Task<IGenericResult<IDictionary<string, object?>>> Transform(
        IDictionary<string, object?> input,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not PipelineTransformConfiguration config)
        {
            return Task.FromResult(GenericResult<IDictionary<string, object?>>.Failure(
                EtlLog.WrongConfigurationType(context.Logger, Kind, configuration.GetType().Name)));
        }

        if (context.BuiltSets.ContainsKey(config.Id))
        {
            return Task.FromResult(GenericResult<IDictionary<string, object?>>.Failure(ComputeSetLog.SetNeedsABatchRun(context.Logger, config.Name)));
        }

        if (!context.BuiltCalculations.TryGetValue(config.Id, out var calculation))
        {
            return Task.FromResult(GenericResult<IDictionary<string, object?>>.Failure(
                ComputeLog.CalculationNotResolved(context.Logger, config.Name)));
        }

        return Task.FromResult(Apply(input, config, calculation, new CalculationRunState(), context));
    }

    /// <inheritdoc />
    public override Task<IGenericResult<IEnumerable<IDictionary<string, object?>>>> TransformBatch(
        IEnumerable<IDictionary<string, object?>> inputs,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (configuration is PipelineTransformConfiguration staged && context.BuiltSets.TryGetValue(staged.Id, out var set))
        {
            return ComputeSetRunner.Run(staged, set, inputs, context, cancellationToken);
        }

        return Task.FromResult(ByRecord(inputs, configuration, context, cancellationToken));
    }

    // A Built transformation that reads one record at a time is applied to every record of the batch; a record that fails is reported and left out.
    private static IGenericResult<IEnumerable<IDictionary<string, object?>>> ByRecord(
        IEnumerable<IDictionary<string, object?>> inputs,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken)
    {
        if (configuration is not PipelineTransformConfiguration config)
        {
            return GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                EtlLog.WrongConfigurationType(context.Logger, Kind, configuration.GetType().Name));
        }

        if (!context.BuiltCalculations.TryGetValue(config.Id, out var calculation))
        {
            return GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                ComputeLog.CalculationNotResolved(context.Logger, config.Name));
        }

        // One run state for the batch: the calculation resets it before every record, so it carries no state between them.
        var state = new CalculationRunState();
        var results = new List<IDictionary<string, object?>>();
        foreach (var input in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var applied = Apply(input, config, calculation, state, context);
            if (applied.IsFailure)
            {
                context.ReportError(string.Join("; ", applied.Messages.Select(message => message.Message)), input);
                continue;
            }

            results.Add(applied.Value!);
        }

        return GenericResult<IEnumerable<IDictionary<string, object?>>>.Success(results);
    }

    /// <inheritdoc />
    public override IGenericResult MapSpecToConfiguration(ITransformOperationSpec spec, IGenericConfiguration target, ILogger logger)
    {
        if (target is not PipelineTransformConfiguration config)
        {
            return GenericResult.Failure(EtlLog.WrongConfigurationType(logger, spec.Name, target.GetType().Name));
        }

        if (spec is not ITransformComputeSpec compute)
        {
            return GenericResult.Failure(TransformLog.TransformParamsInvalid(logger, spec.Name, Kind, "the request does not state a transformation"));
        }

        var problem = FirstProblem(compute);
        if (problem is not null)
        {
            return GenericResult.Failure(TransformLog.TransformParamsInvalid(logger, spec.Name, Kind, problem));
        }

        // A request that states the transformation by name leaves the id empty here: the API resolves the name when the
        // pipeline is saved (StageChainLookup), and the mapper stores the id it found.
        config.ComputeTransformationId = compute.ComputeTransformationId;
        config.ComputeOnRowFailure = compute.ComputeOnRowFailure;
        config.ComputeInputs = compute.ComputeInputs
            .Select(mapping => new PipelineTransformComputeInputConfiguration { InputName = mapping.InputName, SourceField = mapping.SourceField })
            .ToList();
        config.ComputeTables = compute.ComputeTables
            .Select(table => new PipelineTransformComputeTableConfiguration { TableName = table.TableName, FromKind = table.FromKind, FromName = table.FromName })
            .ToList();
        config.ComputeOutputs = compute.ComputeOutputs
            .Select(output => new PipelineTransformComputeOutputConfiguration { OutputName = output.OutputName, Route = output.Route, RouteName = output.RouteName })
            .ToList();
        config.ComputeScalars = compute.ComputeScalars
            .Select(scalar => new PipelineTransformComputeScalarConfiguration { InputName = scalar.InputName, ValueText = scalar.ValueText, ValueType = scalar.ValueType })
            .ToList();
        EtlLog.TransformSpecMapped(logger, spec.Name, spec.OperationType);
        return GenericResult.Success();
    }

    // The record the calculation reads has one field per declared input, named as the input is: the stage's mapping says which
    // field of the stage's record each one comes from. A mapped field the record lacks is a refusal, never an empty value.
    private static IGenericResult<IDictionary<string, object?>> Apply(
        IDictionary<string, object?> input,
        PipelineTransformConfiguration config,
        ICompiledCalculation calculation,
        CalculationRunState state,
        ITransformContext context)
    {
        var inputs = new Dictionary<string, object?>(config.ComputeInputs.Count, StringComparer.Ordinal);
        foreach (var mapping in config.ComputeInputs)
        {
            if (!input.TryGetValue(mapping.SourceField, out var value))
            {
                return GenericResult<IDictionary<string, object?>>.Failure(
                    ComputeLog.SourceFieldMissing(context.Logger, config.Name, mapping.InputName, mapping.SourceField));
            }

            inputs[mapping.InputName] = value;
        }

        var results = new decimal[calculation.EmittedNames.Count];
        if (!calculation.TryEvaluate(inputs, results, state))
        {
            return calculation.Describe(state).ToNewResult<IDictionary<string, object?>>();
        }

        var output = new Dictionary<string, object?>(input, StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < results.Length; index++)
        {
            output[calculation.EmittedNames[index]] = results[index];
        }

        return GenericResult<IDictionary<string, object?>>.Success(output);
    }

    private static string? FirstProblem(ITransformComputeSpec compute)
    {
        var named = !string.IsNullOrWhiteSpace(compute.ComputeTransformation);
        if (compute.ComputeTransformationId is null && !named)
        {
            return "the request states no transformation to apply";
        }

        if (compute.ComputeTransformationId is not null && named)
        {
            return "the request states the transformation by name and by id; state one of the two";
        }

        var readsTables = compute.ComputeTables.Count > 0 || compute.ComputeOutputs.Count > 0 || compute.ComputeScalars.Count > 0;
        if (compute.ComputeInputs.Count == 0 && !readsTables)
        {
            return "the request maps no field to any input and binds no tables";
        }

        if (compute.ComputeInputs.Any(mapping => string.IsNullOrWhiteSpace(mapping.InputName) || string.IsNullOrWhiteSpace(mapping.SourceField)))
        {
            return "a mapping states a blank input or field";
        }

        return BlankBinding(compute)
            ?? (compute.ComputeInputs.Select(mapping => mapping.InputName).Distinct(StringComparer.Ordinal).Count() == compute.ComputeInputs.Count
                ? null
                : "two mappings feed the same input");
    }

    // A table, output or single value binding names what it binds; a blank name binds nothing.
    private static string? BlankBinding(ITransformComputeSpec compute)
    {
        if (compute.ComputeTables.Any(table => string.IsNullOrWhiteSpace(table.TableName) || string.IsNullOrWhiteSpace(table.FromKind)))
        {
            return "a table binding states a blank table or source";
        }

        if (compute.ComputeOutputs.Any(output => string.IsNullOrWhiteSpace(output.OutputName) || string.IsNullOrWhiteSpace(output.Route)))
        {
            return "an output route states a blank output or route";
        }

        return compute.ComputeScalars.Any(scalar => string.IsNullOrWhiteSpace(scalar.InputName) || string.IsNullOrWhiteSpace(scalar.ValueType))
            ? "a single value states a blank input or type"
            : null;
    }
}
