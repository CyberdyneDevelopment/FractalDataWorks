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
/// Rounds a decimal value to the specified number of decimal places
/// using <see cref="MidpointRounding.AwayFromZero"/>.
/// </summary>
[TypeOption(typeof(TransformationTypes), "Round")]
public sealed class RoundFieldTransformer : FieldTransformationBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoundFieldTransformer"/> class.
    /// </summary>
    public RoundFieldTransformer()
        : base(
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
            },
            new OperationParameterDefinition
            {
                Name = "mode",
                Kind = "Scalar",
                IsRequired = false,
                DisplayName = "Rounding mode",
                HelpText = ModeHelpText
            })
    {
    }

    /// <summary>
    /// The help text of the optional <c>mode</c> parameter. It names every option of <see cref="RoundingTypes"/>; a test
    /// fails when the collection gains or loses an option this text does not name.
    /// </summary>
    private const string ModeHelpText =
        "How a value exactly halfway between two results is rounded: AwayFromZero, ToEven, ToZero, ToPositiveInfinity or ToNegativeInfinity. "
        + "When omitted, AwayFromZero is used.";

    /// <inheritdoc/>
    public override Task<IGenericResult<object?>> Transform(
        object? input,
        TransformationContext context,
        CancellationToken cancellationToken = default)
    {
        if (input is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        if (!context.Parameters.TryGetValue("precision", out var precisionText)
            || !int.TryParse(precisionText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var precision))
        {
            throw new InvalidOperationException(
                "Round transform requires a valid 'precision' parameter (integer).");
        }

        var modeName = context.Parameters.TryGetValue("mode", out var configured) && !string.IsNullOrWhiteSpace(configured)
            ? configured
            : "AwayFromZero";

        // ByName returns a NotFound sentinel, never null — the same check AddDuration and Timezone make.
        var mode = RoundingTypes.ByName(modeName);
        if (mode is null || string.Equals(mode.Name, "NotFound", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unknown rounding mode '{modeName}'. Use RoundingTypes.All() for available options.");
        }

        var value = ConvertToDecimal(input);
        return Task.FromResult(GenericResult<object?>.Success(mode.Round(value, precision)));
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
