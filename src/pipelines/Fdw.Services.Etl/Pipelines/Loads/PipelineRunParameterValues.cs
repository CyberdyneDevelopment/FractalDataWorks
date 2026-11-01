using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        IEnumerable<IPipelineRunParameter>? declared,
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
        if (required is not null)
            return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(PipelineRunLog.RunParameterRequiredMissing(logger, required));

        var defaulted = ApplyDefaults(declared, values, logger);
        return defaulted.IsFailure
            ? defaulted.ToNewResult<IReadOnlyDictionary<string, object?>>()
            : GenericResult<IReadOnlyDictionary<string, object?>>.Success(values);
    }

    /// <summary>Lists the declared parameters a run did not give and that declare a default (FDW-1975): the ones whose value <see cref="Resolve"/> took from the pipeline.</summary>
    /// <param name="declared">The pipeline's declared parameters, or <see langword="null"/> when it declares none.</param>
    /// <param name="supplied">The names of the parameters the run gave.</param>
    /// <returns>The names of the parameters that got their default.</returns>
    public static IReadOnlySet<string> Defaulted(IEnumerable<IPipelineRunParameter>? declared, IEnumerable<string> supplied)
    {
        ArgumentNullException.ThrowIfNull(supplied);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (declared is null)
            return names;

        var given = new HashSet<string>(supplied, StringComparer.OrdinalIgnoreCase);
        names.UnionWith(declared.Where(parameter => parameter.DefaultText is not null && !given.Contains(parameter.Name)).Select(parameter => parameter.Name));
        return names;
    }

    // A parameter the run did not give takes the default its pipeline declares, read as the parameter's own type; one with no default stays
    // absent, so a setting that names it fails the run saying so. The default is the designer's text, never a value chosen here.
    private static IGenericResult ApplyDefaults(IEnumerable<IPipelineRunParameter>? declared, Dictionary<string, object?> values, ILogger logger)
    {
        if (declared is null)
            return GenericResult.Success();

        foreach (var parameter in declared)
        {
            if (parameter.DefaultText is not { } text || values.ContainsKey(parameter.Name))
                continue;

            var type = DataTypes.ByName(parameter.ParameterType);
            object? value = null;
            var readable = !ReferenceEquals(type, DataTypes.NotFound)
                && (parameter.IsList ? TryReadList(type, text, out value) : TryReadSingle(type, text, out value));
            if (!readable)
                return GenericResult.Failure(PipelineRunLog.RunParameterDefaultUnreadable(logger, parameter.Name, Words((parameter.ParameterType, parameter.IsList))));

            values[parameter.Name] = value;
        }

        return GenericResult.Success();
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
        IEnumerable<IPipelineRunParameter>? declared,
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

    /// <summary>
    /// Checks the names a caller binds against the declared parameters before any value exists: every name is declared or
    /// reserved, none repeats, and every required parameter is named (FDW-1381: a StartPipeline stage is judged when it is saved
    /// and when the run starts, and its values are only known when the run reaches it).
    /// </summary>
    /// <param name="declared">The pipeline's declared parameters, or <see langword="null"/> when it declares none.</param>
    /// <param name="names">The names the caller binds.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <returns>Success, or a failure naming the parameter and the reason.</returns>
    public static IGenericResult VerifyNames(
        IEnumerable<IPipelineRunParameter>? declared,
        IReadOnlyList<string> names,
        ILogger logger)
    {
        var held = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            if (Find(declared, name) is null)
                return GenericResult.Failure(PipelineRunLog.RunParameterUndeclared(logger, name));
            if (!held.TryAdd(name, null))
                return GenericResult.Failure(PipelineRunLog.RunParameterRepeated(logger, name));
        }

        var required = FirstRequiredMissing(declared, held);
        return required is null
            ? GenericResult.Success()
            : GenericResult.Failure(PipelineRunLog.RunParameterRequiredMissing(logger, required));
    }

    private static IGenericResult VerifyOne(
        IEnumerable<IPipelineRunParameter>? declared,
        KeyValuePair<string, object?> pair,
        ILogger logger)
    {
        var found = Find(declared, pair.Key);
        if (found is not { } parameter)
            return GenericResult.Failure(PipelineRunLog.RunParameterUndeclared(logger, pair.Key));

        var type = DataTypes.ByName(parameter.TypeName);
        var fits = !ReferenceEquals(type, DataTypes.NotFound) && pair.Value is not null
            && (parameter.IsList ? IsListOf(type, pair.Value) : type.ClrType == pair.Value.GetType());
        return fits
            ? GenericResult.Success()
            : GenericResult.Failure(PipelineRunLog.RunParameterUnreadable(logger, pair.Key, Words(parameter)));
    }

    // A list value is the items in order, each already of the item type (FDW-1745).
    private static bool IsListOf(IDataType type, object value) =>
        value is IReadOnlyList<object> { Count: > 0 } items && items.All(item => type.ClrType == item.GetType());

    private static IGenericResult ReadOne(
        IEnumerable<IPipelineRunParameter>? declared,
        KeyValuePair<string, string> pair,
        Dictionary<string, object?> values,
        ILogger logger)
    {
        var found = Find(declared, pair.Key);
        if (found is not { } parameter)
            return GenericResult.Failure(PipelineRunLog.RunParameterUndeclared(logger, pair.Key));
        if (values.ContainsKey(pair.Key))
            return GenericResult.Failure(PipelineRunLog.RunParameterRepeated(logger, pair.Key));

        var type = DataTypes.ByName(parameter.TypeName);
        object? value = null;
        var readable = !ReferenceEquals(type, DataTypes.NotFound)
            && (parameter.IsList ? TryReadList(type, pair.Value, out value) : TryReadSingle(type, pair.Value, out value));
        if (!readable)
            return GenericResult.Failure(PipelineRunLog.RunParameterUnreadable(logger, pair.Key, Words(parameter)));

        values[pair.Key] = value;
        return GenericResult.Success();
    }

    private static bool TryReadSingle(IDataType type, string text, out object? value)
    {
        var converted = type.Convert(text);
        value = converted.IsFailure ? null : converted.Value;
        return converted.IsSuccess;
    }

    // A list arrives as the text of a JSON array of items (a schedule keeps every parameter as text). Each item is converted to the
    // item type; an empty list, an item that is not a value of the type, or text that is not an array is unreadable. Nothing is skipped.
    private static bool TryReadList(IDataType type, string text, out object? value)
    {
        value = null;
        var items = ArrayItems(text);
        if (items.Count == 0)
            return false;

        var typed = new List<object>(items.Count);
        foreach (var item in items)
        {
            var converted = type.Convert(item);
            if (converted.IsFailure || converted.Value is null)
                return false;
            typed.Add(converted.Value);
        }

        value = typed;
        return true;
    }

    // The items of a JSON array as text: JsonElement.ToString gives a string as itself and any other item (a number, true) as written. Not an array: no items.
    private static List<string> ArrayItems(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            return document.RootElement.ValueKind == JsonValueKind.Array
                ? [.. document.RootElement.EnumerateArray().Select(item => item.ToString())]
                : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string Words((string TypeName, bool IsList) parameter) => parameter.IsList ? $"a list of {parameter.TypeName}" : parameter.TypeName;

    // The declared parameter a name refers to (its item type and whether it holds a list), or the reserved watermark; null when neither.
    private static (string TypeName, bool IsList)? Find(IEnumerable<IPipelineRunParameter>? declared, string name)
    {
        if (declared is not null)
        {
            foreach (var parameter in declared)
            {
                if (string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase))
                    return (parameter.ParameterType, parameter.IsList);
            }
        }

        return string.Equals(name, LastSuccessfulRun, StringComparison.OrdinalIgnoreCase) ? (WatermarkTypeName, false) : null;
    }

    private static string? FirstRequiredMissing(IEnumerable<IPipelineRunParameter>? declared, Dictionary<string, object?> values)
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
