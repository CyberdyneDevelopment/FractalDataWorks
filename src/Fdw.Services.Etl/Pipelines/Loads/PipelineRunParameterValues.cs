using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Reads the run parameters a trigger or a schedule supplies against the parameters the pipeline declares (FDW-1109).
/// </summary>
/// <remarks>
/// Why one static resolver: the trigger, the schedule that passes parameters on every run and the engine all call it, so
/// they refuse the same things the same way: a name the pipeline does not declare, a name supplied twice, a required
/// parameter with no value, and a text that is not a value of the declared type. It never fills a missing value in: an
/// absent optional parameter stays absent. <see cref="LastSuccessfulRun"/> is the one name a pipeline does not declare; it
/// is the stored watermark, and a run may override it by supplying it as a DateTimeOffset.
/// </remarks>
public static class PipelineRunParameterValues
{
    /// <summary>The reserved run parameter that holds the time the pipeline's last successful run started.</summary>
    public const string LastSuccessfulRun = "LastSuccessfulRun";

    private const string WatermarkTypeName = "DateTimeOffset";

    /// <summary>
    /// Resolves the supplied name/value pairs to typed values.
    /// </summary>
    /// <param name="declared">The pipeline's declared parameters, or <see langword="null"/> when it declares none.</param>
    /// <param name="supplied">The name/value pairs a trigger or schedule supplies.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <returns>The typed values by name (case-insensitive), or a failure naming the parameter and the reason.</returns>
    public static IGenericResult<IReadOnlyDictionary<string, object?>> Resolve(
        IList<PipelineRunParameterConfiguration>? declared,
        IReadOnlyList<KeyValuePair<string, string>> supplied,
        ILogger logger)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in supplied)
        {
            var read = ReadOne(declared, pair, values, logger);
            if (read.IsFailure)
                return read.ToNewResult<IReadOnlyDictionary<string, object?>>();
        }

        var required = FirstRequiredMissing(declared, values);
        return required is null
            ? GenericResult<IReadOnlyDictionary<string, object?>>.Success(values)
            : GenericResult<IReadOnlyDictionary<string, object?>>.Failure(PipelineRunLog.RunParameterRequiredMissing(logger, required));
    }

    /// <summary>
    /// Checks values that are already typed (the ones a request or the execution options carry) against the declared
    /// parameters: every name is declared or reserved, every value is of its declared type, every required one is present.
    /// </summary>
    /// <param name="declared">The pipeline's declared parameters, or <see langword="null"/> when it declares none.</param>
    /// <param name="values">The typed values by name, or <see langword="null"/> when none were supplied.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <returns>Success, or a failure naming the parameter and the reason.</returns>
    public static IGenericResult Verify(
        IList<PipelineRunParameterConfiguration>? declared,
        IReadOnlyDictionary<string, object?>? values,
        ILogger logger)
    {
        var held = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (values is not null)
        {
            foreach (var pair in values)
            {
                var checkedOne = VerifyOne(declared, pair, logger);
                if (checkedOne.IsFailure)
                    return checkedOne;
                held[pair.Key] = pair.Value;
            }
        }

        var required = FirstRequiredMissing(declared, held);
        return required is null
            ? GenericResult.Success()
            : GenericResult.Failure(PipelineRunLog.RunParameterRequiredMissing(logger, required));
    }

    private static IGenericResult VerifyOne(
        IList<PipelineRunParameterConfiguration>? declared,
        KeyValuePair<string, object?> pair,
        ILogger logger)
    {
        var typeName = TypeNameOf(declared, pair.Key);
        if (typeName is null)
            return GenericResult.Failure(PipelineRunLog.RunParameterUndeclared(logger, pair.Key));

        var type = DataTypes.ByName(typeName);
        return !ReferenceEquals(type, DataTypes.NotFound) && pair.Value is not null && type.ClrType == pair.Value.GetType()
            ? GenericResult.Success()
            : GenericResult.Failure(PipelineRunLog.RunParameterUnreadable(logger, pair.Key, typeName));
    }

    private static IGenericResult ReadOne(
        IList<PipelineRunParameterConfiguration>? declared,
        KeyValuePair<string, string> pair,
        Dictionary<string, object?> values,
        ILogger logger)
    {
        var typeName = TypeNameOf(declared, pair.Key);
        if (typeName is null)
            return GenericResult.Failure(PipelineRunLog.RunParameterUndeclared(logger, pair.Key));
        if (values.ContainsKey(pair.Key))
            return GenericResult.Failure(PipelineRunLog.RunParameterRepeated(logger, pair.Key));

        var type = DataTypes.ByName(typeName);
        var value = ReferenceEquals(type, DataTypes.NotFound) ? null : type.Convert(pair.Value);
        if (value is null || value.IsFailure)
            return GenericResult.Failure(PipelineRunLog.RunParameterUnreadable(logger, pair.Key, typeName));

        values[pair.Key] = value.Value;
        return GenericResult.Success();
    }

    private static string? TypeNameOf(IList<PipelineRunParameterConfiguration>? declared, string name)
    {
        if (declared is not null)
        {
            foreach (var parameter in declared)
            {
                if (string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase))
                    return parameter.ParameterType;
            }
        }

        return string.Equals(name, LastSuccessfulRun, StringComparison.OrdinalIgnoreCase) ? WatermarkTypeName : null;
    }

    private static string? FirstRequiredMissing(IList<PipelineRunParameterConfiguration>? declared, Dictionary<string, object?> values)
    {
        if (declared is null)
            return null;
        foreach (var parameter in declared)
        {
            if (parameter.IsRequired && !values.ContainsKey(parameter.Name))
                return parameter.Name;
        }

        return null;
    }
}
