using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.Transformations;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Builds output rows from incoming rows by field mappings (FDW-1386): the one place a mapping is applied, for the input of any component
/// (FDW-2013). A mapping that carries a <see cref="FlowFieldConversion"/> (FDW-2000) also defaults, chains, converts and requires. A row it
/// cannot map fails the run.
/// </summary>
internal static class FlowRowMapper
{
    /// <summary>The field an error output adds: the name of the component that refused the row.</summary>
    public const string ErrorComponentField = "ErrorComponent";

    /// <summary>The field an error output adds: why the component refused the row.</summary>
    public const string ErrorReasonField = "ErrorReason";

    /// <summary>Maps every row of a batch.</summary>
    /// <param name="maps">The output fields and where each comes from.</param>
    /// <param name="batch">The incoming rows.</param>
    /// <param name="component">The component the input belongs to, for a refusal.</param>
    /// <param name="input">The input node the mapping is on, for a refusal.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>The mapped rows, or the failure naming the component, the input and the field an incoming row lacks or the mapping that refused it.</returns>
    public static async ValueTask<IGenericResult<FlowBatch>> Apply(
        IReadOnlyList<FlowFieldMap> maps, FlowBatch batch, string component, string input, ILogger logger, CancellationToken cancellationToken = default)
    {
        var good = new List<IDictionary<string, object?>>(batch.Count);
        foreach (var row in batch.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var mapped = await MapRow(maps, row, cancellationToken).ConfigureAwait(false);
            if (mapped.Row is null)
            {
                return GenericResult<FlowBatch>.Failure(mapped.MissingField is not null
                    ? FlowLog.FieldMissing(logger, component, input, mapped.MissingField)
                    : FlowLog.MappingRefused(logger, component, input, mapped.Reason!));
            }

            good.Add(mapped.Row);
        }

        return GenericResult<FlowBatch>.Success(new FlowBatch(good));
    }

    /// <summary>Makes the row an error output sends: a copy of the row with the component and the reason added.</summary>
    /// <param name="row">The row that was refused.</param>
    /// <param name="component">The component that refused it.</param>
    /// <param name="reason">Why.</param>
    /// <returns>The error row.</returns>
    public static Dictionary<string, object?> ErrorRow(IDictionary<string, object?> row, string component, string reason) =>
        new(row, StringComparer.OrdinalIgnoreCase)
        {
            [ErrorComponentField] = component,
            [ErrorReasonField] = reason
        };

    private static async ValueTask<(Dictionary<string, object?>? Row, string? Reason, string? MissingField)> MapRow(
        IReadOnlyList<FlowFieldMap> maps, IDictionary<string, object?> row, CancellationToken cancellationToken)
    {
        var built = new Dictionary<string, object?>(maps.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var map in maps)
        {
            object? value = null;
            if (map.IsConstant)
            {
                value = map.Constant;
            }
            else if (row.TryGetValue(map.Source!, out var found))
            {
                value = found;
            }
            else if (map.Conversion?.DefaultValue is null)
            {
                return (null, $"the row has no field '{map.Source}'", map.Source);
            }

            if (map.Conversion is null)
            {
                built[map.Target] = value;
                continue;
            }

            var converted = await Convert(map, value, row, cancellationToken).ConfigureAwait(false);
            if (converted.Reason is not null)
            {
                return (null, converted.Reason, null);
            }

            built[map.Target] = converted.Value;
        }

        return (built, null, null);
    }

    // A null value takes the default; one that is still null is refused when the mapping is required and otherwise stays null; any other value goes
    // through the chain and is converted. Returns the reason the row is refused, or the value.
    private static async ValueTask<(object? Value, string? Reason)> Convert(
        FlowFieldMap map, object? value, IDictionary<string, object?> row, CancellationToken cancellationToken)
    {
        var conversion = map.Conversion!;
        var label = map.Source ?? map.Target;
        object? current = value ?? conversion.DefaultValue;
        if (current is null)
        {
            return conversion.IsRequired ? (null, $"the field '{label}' is required and has no value") : (null, null);
        }

        foreach (var step in conversion.Chain.OrderBy(step => step.Ordinal))
        {
            var applied = await Chain(step, current, label, row, cancellationToken).ConfigureAwait(false);
            if (applied.Reason is not null)
            {
                return (null, applied.Reason);
            }

            if (applied.Value is null)
            {
                return (null, null);
            }

            current = applied.Value;
        }

        if (conversion.TargetType is { } targetType && !FieldTypeConversion.TryConvert(current, targetType, out current, out var failure))
        {
            return (null, $"the field '{label}' could not be converted to {targetType}: {failure}");
        }

        return (current, null);
    }

    // One step of a chain: the named field transformation, with the parameters the Transformation states. A transformation that is not
    // registered, that lacks a required parameter or that fails refuses the row, naming the step.
    private static async ValueTask<(object? Value, string? Reason)> Chain(
        IFieldMappingTransformation step, object value, string label, IDictionary<string, object?> row, CancellationToken cancellationToken)
    {
        if (TransformationTypes.ByName(step.TransformType) is not FieldTransformationBase transformer)
        {
            return (null, $"the transformation '{step.TransformType}' of the field '{label}' is not a registered field transformation");
        }

        var missing = transformer.ExpectedParameters
            .Where(definition => definition.IsRequired && !step.Parameters.ContainsKey(definition.Name))
            .Select(definition => definition.Name)
            .ToList();
        if (missing.Count > 0)
        {
            return (null, $"the transformation '{step.TransformType}' of the field '{label}' is missing the required parameter(s) {string.Join(", ", missing)}");
        }

        var result = await transformer.Transform(
            value,
            new TransformationContext
            {
                OperatingDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ExecutionTimestamp = DateTimeOffset.UtcNow,
                CurrentRecord = new Dictionary<string, object?>(row, StringComparer.Ordinal),
                Parameters = step.Parameters,
                CancellationToken = cancellationToken,
            },
            cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? (result.Value, null)
            : (null, $"the transformation '{step.TransformType}' failed for the field '{label}': {result.CurrentMessage}");
    }
}
