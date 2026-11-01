using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Writes the request of an accepted pipeline run as text and reads it back (FDW-3216). A parameter value is kept with the exact type it has, so a number
/// comes back as a number, a date as a date and a list as a list of the same items; the types that can be kept are the CLR types of the data types a run
/// parameter can be declared as, and a value of any other type is refused naming the parameter, never turned into text.
/// </summary>
internal static class PipelineRunRequestJson
{
    private static readonly Dictionary<string, Type> Kept =
        DataTypes.All().Select(type => type.ClrType).Distinct().ToDictionary(type => type.FullName!, StringComparer.Ordinal);

    internal sealed record RequestRecord(
        Guid ExecutionId, Guid PipelineId, string PipelineName, string TriggerSource, string? ScheduleName, Guid? TenantId,
        IReadOnlyList<string> RunRoles, int? TestRowsPerSource, IReadOnlyList<ValueRecord>? Values);

    internal sealed record ValueRecord(string Name, string? Type, bool IsList, JsonElement? Value);

    /// <summary>Writes the request as text.</summary>
    /// <param name="request">The request.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The text, or a failure naming the parameter whose value cannot be kept.</returns>
    public static IGenericResult<string> Write(PipelineExecutionRequest request, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(logger);
        List<ValueRecord>? values = null;
        if (request.Parameters is { } parameters)
        {
            values = [];
            foreach (var (name, value) in parameters)
            {
                var packed = Pack(name, value, logger);
                if (packed.IsFailure || packed.Value is null)
                {
                    return packed.ToNewResult<string>();
                }

                values.Add(packed.Value);
            }
        }

        return GenericResult<string>.Success(JsonSerializer.Serialize(new RequestRecord(
            request.ExecutionId, request.PipelineId, request.PipelineName, request.TriggerSource, request.ScheduleName, request.TenantId,
            [.. request.RunRoles], request.TestRowsPerSource, values)));
    }

    /// <summary>Reads a request written by <see cref="Write"/>.</summary>
    /// <param name="executionId">The run's id, for the message of a record that cannot be read.</param>
    /// <param name="text">The text.</param>
    /// <param name="heldUntil">When the run is held until, from its record; none for a run that is not held.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The request, or a failure naming what cannot be read.</returns>
    public static IGenericResult<PipelineExecutionRequest> Read(Guid executionId, string text, DateTimeOffset? heldUntil, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(logger);
        RequestRecord? record;
        try
        {
            record = JsonSerializer.Deserialize<RequestRecord>(text);
        }
        catch (JsonException exception)
        {
            return GenericResult<PipelineExecutionRequest>.Failure(PipelineRunRecoveryLog.RequestUnreadable(logger, executionId, exception.Message));
        }

        if (record is null)
        {
            return GenericResult<PipelineExecutionRequest>.Failure(PipelineRunRecoveryLog.RequestUnreadable(logger, executionId, "the record is empty"));
        }

        Dictionary<string, object?>? values = null;
        if (record.Values is { } packed)
        {
            values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var one in packed)
            {
                if (!TryUnpack(one, out var value, out var reason))
                {
                    return GenericResult<PipelineExecutionRequest>.Failure(PipelineRunRecoveryLog.RequestUnreadable(logger, executionId, reason));
                }

                values[one.Name] = value;
            }
        }

        return GenericResult<PipelineExecutionRequest>.Success(new PipelineExecutionRequest
        {
            ExecutionId = record.ExecutionId,
            PipelineId = record.PipelineId,
            PipelineName = record.PipelineName,
            TriggerSource = record.TriggerSource,
            ScheduleName = record.ScheduleName,
            TenantId = record.TenantId,
            RunRoles = record.RunRoles,
            Parameters = values,
            TestRowsPerSource = record.TestRowsPerSource,
            HeldUntil = heldUntil
        });
    }

    // A list value is the items in order, each of the same type (FDW-1745); it is kept as an array of items of that type.
    private static IGenericResult<ValueRecord> Pack(string name, object? value, ILogger logger)
    {
        if (value is null)
        {
            return GenericResult<ValueRecord>.Success(new ValueRecord(name, null, false, null));
        }

        var items = value as IReadOnlyList<object>;
        var type = items is null ? value.GetType() : SingleItemType(items);
        if (type is null || !Kept.ContainsKey(type.FullName!))
        {
            return GenericResult<ValueRecord>.Failure(PipelineRunRecoveryLog.ValueNotKept(
                logger, name, value.GetType().FullName!, type is null ? "a list holds items of one type" : "it is not the type of a data type"));
        }

        var element = items is null
            ? JsonSerializer.SerializeToElement(value, type)
            : JsonSerializer.SerializeToElement(items.Select(item => JsonSerializer.SerializeToElement(item, type)).ToList());
        return GenericResult<ValueRecord>.Success(new ValueRecord(name, type.FullName, items is not null, element));
    }

    private static Type? SingleItemType(IReadOnlyList<object> items)
    {
        var types = items.Select(item => item.GetType()).Distinct().ToList();
        return types.Count == 1 ? types[0] : null;
    }

    private static bool TryUnpack(ValueRecord one, out object? value, out string reason)
    {
        value = null;
        reason = string.Empty;
        if (one.Type is null)
        {
            return true;
        }

        if (!Kept.TryGetValue(one.Type, out var type))
        {
            reason = $"parameter '{one.Name}' has the type {one.Type}, which is not the type of a data type";
            return false;
        }

        if (one.Value is not { } element)
        {
            reason = $"parameter '{one.Name}' has a type and no value";
            return false;
        }

        try
        {
            if (!one.IsList)
            {
                value = element.Deserialize(type);
                reason = value is null ? $"parameter '{one.Name}' has no value" : string.Empty;
                return value is not null;
            }

            var list = new List<object>();
            foreach (var item in element.EnumerateArray())
            {
                var read = item.Deserialize(type);
                if (read is null)
                {
                    reason = $"an item of parameter '{one.Name}' has no value";
                    return false;
                }

                list.Add(read);
            }

            value = list;
            return true;
        }
        catch (JsonException exception)
        {
            reason = $"parameter '{one.Name}' cannot be read as {one.Type}: {exception.Message}";
            return false;
        }
        catch (InvalidOperationException exception)
        {
            reason = $"parameter '{one.Name}' cannot be read as {one.Type}: {exception.Message}";
            return false;
        }
    }
}
