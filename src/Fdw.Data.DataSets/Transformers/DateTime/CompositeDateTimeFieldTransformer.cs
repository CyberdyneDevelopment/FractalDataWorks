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
/// Builds a <see cref="DateTimeOffset"/> from separate date, hour, and optional sub-hour interval
/// fields in the current record. Input is ignored; this is an injection-style transform.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "CompositeDateTime")]
public sealed class CompositeDateTimeFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeDateTimeFieldTransformer"/> class.
    /// </summary>
    public CompositeDateTimeFieldTransformer()
        : base(
            id: 104,
            name: "CompositeDateTime",
            displayName: "Composite DateTime",
            description: "Builds a DateTimeOffset from separate date, hour, and optional sub-hour interval fields in the current record.",
            category: "DateTime",
            supportsBatching: false,
            new OperationParameterDefinition
            {
                Name = "dateField",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Date Field",
                HelpText = "The field name containing the date value (parsed as DateOnly).",
            },
            new OperationParameterDefinition
            {
                Name = "hourField",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Hour Field",
                HelpText = "The field name containing the hour value (0-23).",
            },
            new OperationParameterDefinition
            {
                Name = "intervalField",
                Kind = "Scalar",
                IsRequired = false,
                DisplayName = "Interval Field",
                HelpText = "Optional field name containing the sub-hour interval number.",
            },
            new OperationParameterDefinition
            {
                Name = "intervalMinutes",
                Kind = "Scalar",
                IsRequired = false,
                DisplayName = "Interval Minutes",
                HelpText = "Minutes per interval (e.g., 15 for quarter-hour intervals). Used with intervalField.",
            },
            new OperationParameterDefinition
            {
                Name = "zone",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Timezone",
                HelpText = "Target timezone name from TimeZoneTypes (e.g., UTC, Central, Eastern).",
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
        var (dateFieldName, hourFieldName, timeZone) = ValidateRequiredParameters(parameters);

        if (!TryReadDate(context, dateFieldName, out var date))
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        if (!TryReadHour(context, hourFieldName, out var hour))
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        var additionalMinutes = CalculateIntervalMinutes(parameters, context);
        var dateTime = date.ToDateTime(new TimeOnly(hour, 0)).AddMinutes(additionalMinutes);
        var offset = timeZone.GetUtcOffset(dateTime);
        var result = new DateTimeOffset(dateTime, offset);

        return Task.FromResult(GenericResult<object?>.Success(result));
    }

    private static (string DateFieldName, string HourFieldName, TimeZoneInfo TimeZone) ValidateRequiredParameters(
        IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("dateField", out var dateFieldName) ||
            string.IsNullOrWhiteSpace(dateFieldName))
        {
            throw new InvalidOperationException("CompositeDateTime requires a 'dateField' parameter.");
        }

        if (!parameters.TryGetValue("hourField", out var hourFieldName) ||
            string.IsNullOrWhiteSpace(hourFieldName))
        {
            throw new InvalidOperationException("CompositeDateTime requires an 'hourField' parameter.");
        }

        if (!parameters.TryGetValue("zone", out var zone) ||
            string.IsNullOrWhiteSpace(zone))
        {
            throw new InvalidOperationException("CompositeDateTime requires a 'zone' parameter.");
        }

        var zoneType = TimeZoneTypes.ByName(zone);
        if (string.Equals(zoneType.Name, "_Empty", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unknown timezone '{zone}'. Use TimeZoneTypes.All() for available options.");
        }

        return (dateFieldName, hourFieldName, zoneType.Resolve());
    }

    private static bool TryReadDate(FieldTransformContext context, string dateFieldName, out DateOnly date)
    {
        date = default;
        if (!context.CurrentRecord.TryGetValue(dateFieldName, out var dateRaw) || dateRaw is null)
        {
            return false;
        }

        if (dateRaw is DateOnly dateOnly)
        {
            date = dateOnly;
            return true;
        }

        if (dateRaw is string dateStr &&
            DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            date = parsedDate;
            return true;
        }

        return false;
    }

    private static bool TryReadHour(FieldTransformContext context, string hourFieldName, out int hour)
    {
        hour = 0;
        if (!context.CurrentRecord.TryGetValue(hourFieldName, out var hourRaw) || hourRaw is null)
        {
            return false;
        }

        return TryParseInt(hourRaw, out hour);
    }

    private static int CalculateIntervalMinutes(
        IReadOnlyDictionary<string, string> parameters,
        FieldTransformContext context)
    {
        parameters.TryGetValue("intervalField", out var intervalFieldName);
        parameters.TryGetValue("intervalMinutes", out var intervalMinutesStr);

        if (string.IsNullOrWhiteSpace(intervalFieldName) ||
            string.IsNullOrWhiteSpace(intervalMinutesStr))
        {
            return 0;
        }

        if (!int.TryParse(intervalMinutesStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutesPerInterval))
        {
            throw new InvalidOperationException(
                $"CompositeDateTime 'intervalMinutes' parameter '{intervalMinutesStr}' is not a valid integer.");
        }

        if (context.CurrentRecord.TryGetValue(intervalFieldName, out var intervalRaw) &&
            intervalRaw is not null &&
            TryParseInt(intervalRaw, out var interval))
        {
            return interval * minutesPerInterval;
        }

        return 0;
    }

    private static bool TryParseInt(object value, out int result)
    {
        switch (value)
        {
            case int i:
                result = i;
                return true;

            case long l:
                result = (int)l;
                return true;

            case string s:
                return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

            default:
                result = 0;
                return false;
        }
    }
}
