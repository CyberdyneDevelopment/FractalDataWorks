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
/// Divides the input value by a field value from the current record.
/// Returns a configurable default when the divisor is zero.
/// Does not support batching because it reads <see cref="FieldTransformContext.CurrentRecord"/>.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "ConditionalDivide")]
public sealed class ConditionalDivideFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConditionalDivideFieldTransformer"/> class.
    /// </summary>
    public ConditionalDivideFieldTransformer()
        : base(
            id: 204,
            name: "ConditionalDivide",
            displayName: "Conditional Divide",
            description: "Divides the input by a field value from the current record. Returns a default when the divisor is zero.",
            category: "Numeric",
            supportsBatching: false,
            new OperationParameterDefinition
            {
                Name = "divisorField",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Divisor Field",
                HelpText = "The name of the field in the current record to use as the divisor."
            },
            new OperationParameterDefinition
            {
                Name = "default",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Default Value",
                HelpText = "The decimal value to return when the divisor is zero."
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

        if (!parameters.TryGetValue("divisorField", out var divisorField)
            || string.IsNullOrWhiteSpace(divisorField))
        {
            throw new InvalidOperationException(
                "ConditionalDivide transform requires a 'divisorField' parameter.");
        }

        if (!parameters.TryGetValue("default", out var defaultText)
            || !decimal.TryParse(defaultText, NumberStyles.Any, CultureInfo.InvariantCulture, out var defaultValue))
        {
            throw new InvalidOperationException(
                "ConditionalDivide transform requires a valid 'default' parameter (decimal).");
        }

        if (!context.CurrentRecord.TryGetValue(divisorField, out var divisorRaw) || divisorRaw is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(defaultValue));
        }

        var divisor = ConvertToDecimal(divisorRaw);
        if (divisor == 0m)
        {
            return Task.FromResult(GenericResult<object?>.Success(defaultValue));
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
                $"ConditionalDivide does not support input type '{input.GetType().Name}'. Input must be numeric.")
        };
    }
}
