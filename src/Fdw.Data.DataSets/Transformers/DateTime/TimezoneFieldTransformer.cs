using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>
/// Applies a named timezone offset to a DateTime, DateTimeOffset, or parseable string value.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "Timezone")]
public sealed class TimezoneFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimezoneFieldTransformer"/> class.
    /// </summary>
    public TimezoneFieldTransformer()
        : base(
            id: 100,
            name: "Timezone",
            displayName: "Timezone",
            description: "Applies a named timezone offset to a DateTime, DateTimeOffset, or parseable string value.",
            category: "DateTime",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "zone",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Timezone",
                HelpText = "Target timezone name from TimeZoneTypes (e.g., UTC, Central, Eastern, Pacific, Mountain).",
            })
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<object?>> Execute(
        object? input,
        IReadOnlyDictionary<string, string> parameters,
        FieldTransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (input is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        var timeZone = ResolveTimeZone(parameters);

        var converted = ConvertToTimeZone(input, timeZone);
        return Task.FromResult(GenericResult<object?>.Success(converted));
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<IReadOnlyList<object?>>> ExecuteBatch(
        IReadOnlyList<object?> inputs,
        IReadOnlyDictionary<string, string> parameters,
        FieldTransformContext context,
        CancellationToken cancellationToken = default)
    {
        var timeZone = ResolveTimeZone(parameters);
        var results = new List<object?>(inputs.Count);

        foreach (var input in inputs)
        {
            if (input is null)
            {
                results.Add(null);
                continue;
            }

            results.Add(ConvertToTimeZone(input, timeZone));
        }

        return Task.FromResult(GenericResult<IReadOnlyList<object?>>.Success(results));
    }

    private static DateTimeOffset ConvertToTimeZone(object input, TimeZoneInfo timeZone)
    {
        return input switch
        {
            DateTimeOffset dto => TimeZoneInfo.ConvertTime(dto, timeZone),
            DateTime dt => ConvertDateTime(dt, timeZone),
            string s when DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                => TimeZoneInfo.ConvertTime(parsed, timeZone),
            _ => throw new InvalidOperationException(
                $"Cannot apply timezone to input of type '{input.GetType().Name}'. Expected DateTime, DateTimeOffset, or parseable string.")
        };
    }

    private static DateTimeOffset ConvertDateTime(DateTime input, TimeZoneInfo timeZone)
    {
        var utcDt = input.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(input, DateTimeKind.Utc)
            : input.ToUniversalTime();
        var converted = TimeZoneInfo.ConvertTimeFromUtc(utcDt, timeZone);
        return new DateTimeOffset(converted, timeZone.GetUtcOffset(converted));
    }

    private static TimeZoneInfo ResolveTimeZone(IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("zone", out var zone) ||
            string.IsNullOrWhiteSpace(zone))
        {
            throw new InvalidOperationException("Timezone transform requires a 'zone' parameter.");
        }

        var zoneType = TimeZoneTypes.ByName(zone);
        if (string.Equals(zoneType.Name, "_Empty", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unknown timezone '{zone}'. Use TimeZoneTypes.All() for available options.");
        }

        return zoneType.Resolve();
    }
}
