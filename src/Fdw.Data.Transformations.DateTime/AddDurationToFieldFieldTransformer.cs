using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Data.Transformations;

/// <summary>
/// When the input is null, reads a source field from the current record and adds a duration.
/// When the input is not null, passes it through unchanged.
/// </summary>
[TypeOption(typeof(TransformationTypes), "AddDurationToField")]
public sealed class AddDurationToFieldFieldTransformer : FieldTransformationBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddDurationToFieldFieldTransformer"/> class.
    /// </summary>
    public AddDurationToFieldFieldTransformer()
        : base(
            id: 105,
            name: "AddDurationToField",
            displayName: "Fallback From Field",
            description: "When input is null, reads a source field from the current record and adds a duration. Passes non-null input through unchanged.",
            category: "DateTime",
            supportsBatching: false,
            new OperationParameterDefinition
            {
                Name = "sourceField",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Source Field",
                HelpText = "The field name to read from the current record when input is null.",
            },
            new OperationParameterDefinition
            {
                Name = "amount",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Amount",
                HelpText = "The numeric amount of duration to add to the source field value (may be negative).",
            },
            new OperationParameterDefinition
            {
                Name = "unit",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Unit",
                HelpText = "Duration unit name from DurationUnitTypes (e.g., Hours, Minutes, Days, Seconds).",
            })
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<object?>> Transform(
        object? input,
        TransformationContext context,
        CancellationToken cancellationToken = default)
    {
        if (input is not null)
        {
            return Task.FromResult(GenericResult<object?>.Success(input));
        }

        if (!context.Parameters.TryGetValue("sourceField", out var sourceField) ||
            string.IsNullOrWhiteSpace(sourceField))
        {
            throw new InvalidOperationException("AddDurationToField requires a 'sourceField' parameter.");
        }

        if (!context.CurrentRecord.TryGetValue(sourceField, out var sourceValue) || sourceValue is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        var duration = ResolveDuration(context.Parameters);

        return Task.FromResult<IGenericResult<object?>>(sourceValue switch
        {
            DateTimeOffset dto => GenericResult<object?>.Success(dto.Add(duration)),
            DateTime dt => GenericResult<object?>.Success(dt.Add(duration)),
            _ => throw new InvalidOperationException(
                $"AddDurationToField source field '{sourceField}' has type '{sourceValue.GetType().Name}'. Expected DateTime or DateTimeOffset.")
        });
    }

    private static TimeSpan ResolveDuration(IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("amount", out var amountStr) ||
            !double.TryParse(amountStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
        {
            throw new InvalidOperationException("AddDurationToField requires a valid numeric 'amount' parameter.");
        }

        if (!parameters.TryGetValue("unit", out var unit) ||
            string.IsNullOrWhiteSpace(unit))
        {
            throw new InvalidOperationException("AddDurationToField requires a 'unit' parameter.");
        }

        var unitType = DurationUnitTypes.ByName(unit);
        if (string.Equals(unitType.Name, "_Empty", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unknown duration unit '{unit}'. Use DurationUnitTypes.All() for available options.");
        }

        return unitType.ToTimeSpan(amount);
    }
}
