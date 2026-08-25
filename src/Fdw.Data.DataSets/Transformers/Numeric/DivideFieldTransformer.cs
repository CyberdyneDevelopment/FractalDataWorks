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
/// Divides the input value by a fixed divisor parameter.
/// Input must be numeric (converted to decimal). Returns null on divide by zero.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "Divide")]
public sealed class DivideFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DivideFieldTransformer"/> class.
    /// </summary>
    public DivideFieldTransformer()
        : base(
            id: 203,
            name: "Divide",
            displayName: "Divide",
            description: "Divides the input value by a fixed divisor. Input must be numeric.",
            category: "Numeric",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "divisor",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Divisor",
                HelpText = "The value to divide the input by. Must not be zero."
            })
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<object?>> Transform(
        object? input,
        FieldTransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (input is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        if (!context.Parameters.TryGetValue("divisor", out var divisorText)
            || !decimal.TryParse(divisorText, NumberStyles.Any, CultureInfo.InvariantCulture, out var divisor))
        {
            throw new InvalidOperationException(
                "Divide transform requires a valid 'divisor' parameter.");
        }

        if (divisor == 0m)
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        var value = ConvertToDecimal(input);
        return Task.FromResult(GenericResult<object?>.Success(value / divisor));
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
                $"Divide does not support input type '{input.GetType().Name}'. Input must be numeric.")
        };
    }
}
