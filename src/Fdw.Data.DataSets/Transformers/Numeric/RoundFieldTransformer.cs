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
/// Rounds a decimal value to the specified number of decimal places
/// using <see cref="MidpointRounding.AwayFromZero"/>.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "Round")]
public sealed class RoundFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoundFieldTransformer"/> class.
    /// </summary>
    public RoundFieldTransformer()
        : base(
            id: 205,
            name: "Round",
            displayName: "Round",
            description: "Rounds a decimal value to N decimal places using MidpointRounding.AwayFromZero.",
            category: "Numeric",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "precision",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Precision",
                HelpText = "The number of decimal places to round to."
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

        if (!parameters.TryGetValue("precision", out var precisionText)
            || !int.TryParse(precisionText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var precision))
        {
            throw new InvalidOperationException(
                "Round transform requires a valid 'precision' parameter (integer).");
        }

        var value = ConvertToDecimal(input);
        var rounded = Math.Round(value, precision, MidpointRounding.AwayFromZero);
        return Task.FromResult(GenericResult<object?>.Success(rounded));
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<IReadOnlyList<object?>>> ExecuteBatch(
        IReadOnlyList<object?> inputs,
        IReadOnlyDictionary<string, string> parameters,
        FieldTransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (!parameters.TryGetValue("precision", out var precisionText)
            || !int.TryParse(precisionText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var precision))
        {
            throw new InvalidOperationException(
                "Round transform requires a valid 'precision' parameter (integer).");
        }

        var results = new List<object?>(inputs.Count);
        foreach (var input in inputs)
        {
            if (input is null)
            {
                results.Add(null);
                continue;
            }

            var value = ConvertToDecimal(input);
            results.Add(Math.Round(value, precision, MidpointRounding.AwayFromZero));
        }

        return Task.FromResult(GenericResult<IReadOnlyList<object?>>.Success(results));
    }

    private static decimal ConvertToDecimal(object input)
    {
        return input switch
        {
            decimal d => d,
            double d => Convert.ToDecimal(d),
            float f => Convert.ToDecimal(f),
            int i => Convert.ToDecimal(i),
            long l => Convert.ToDecimal(l),
            short s => Convert.ToDecimal(s),
            byte b => Convert.ToDecimal(b),
            _ => throw new InvalidOperationException(
                $"Round does not support input type '{input.GetType().Name}'. Input must be numeric.")
        };
    }
}
