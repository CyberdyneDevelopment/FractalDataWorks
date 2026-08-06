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
/// Adds a fixed duration to a DateTime or DateTimeOffset value.
/// The output type matches the input type.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "AddDuration")]
public sealed class AddDurationFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddDurationFieldTransformer"/> class.
    /// </summary>
    public AddDurationFieldTransformer()
        : base(
            id: 103,
            name: "AddDuration",
            displayName: "Add Duration",
            description: "Adds a fixed duration (Hours, Minutes, Days, or Seconds) to a DateTime or DateTimeOffset value.",
            category: "DateTime",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "amount",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Amount",
                HelpText = "The numeric amount of duration to add (may be negative).",
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

        var duration = ResolveDuration(parameters);

        return Task.FromResult<IGenericResult<object?>>(input switch
        {
            DateTimeOffset dto => GenericResult<object?>.Success(dto.Add(duration)),
            DateTime dt => GenericResult<object?>.Success(dt.Add(duration)),
            _ => throw new InvalidOperationException(
                $"AddDuration expects DateTime or DateTimeOffset but received '{input.GetType().Name}'.")
        });
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<IReadOnlyList<object?>>> ExecuteBatch(
        IReadOnlyList<object?> inputs,
        IReadOnlyDictionary<string, string> parameters,
        FieldTransformContext context,
        CancellationToken cancellationToken = default)
    {
        var duration = ResolveDuration(parameters);
        var results = new List<object?>(inputs.Count);

        foreach (var input in inputs)
        {
            if (input is null)
            {
                results.Add(null);
                continue;
            }

            switch (input)
            {
                case DateTimeOffset dto:
                    results.Add(dto.Add(duration));
                    break;

                case DateTime dt:
                    results.Add(dt.Add(duration));
                    break;

                default:
                    throw new InvalidOperationException(
                        $"AddDuration expects DateTime or DateTimeOffset but received '{input.GetType().Name}'.");
            }
        }

        return Task.FromResult(GenericResult<IReadOnlyList<object?>>.Success(results));
    }

    private static TimeSpan ResolveDuration(IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("amount", out var amountStr) ||
            !double.TryParse(amountStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
        {
            throw new InvalidOperationException("AddDuration requires a valid numeric 'amount' parameter.");
        }

        if (!parameters.TryGetValue("unit", out var unit) ||
            string.IsNullOrWhiteSpace(unit))
        {
            throw new InvalidOperationException("AddDuration requires a 'unit' parameter.");
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
