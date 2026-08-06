using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>
/// Computes the average of the input value and a second field value from the current record.
/// Both values must be numeric (converted to decimal). Result is decimal.
/// Does not support batching because it reads <see cref="FieldTransformContext.CurrentRecord"/>.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "Average")]
public sealed class AverageFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AverageFieldTransformer"/> class.
    /// </summary>
    public AverageFieldTransformer()
        : base(
            id: 206,
            name: "Average",
            displayName: "Average",
            description: "Computes the average of the input value and a second field value from the current record.",
            category: "Numeric",
            supportsBatching: false,
            new OperationParameterDefinition
            {
                Name = "field2",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Second Field",
                HelpText = "The name of the field in the current record to average with the input."
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

        if (!parameters.TryGetValue("field2", out var field2Name)
            || string.IsNullOrWhiteSpace(field2Name))
        {
            throw new InvalidOperationException(
                "Average transform requires a 'field2' parameter.");
        }

        if (!context.CurrentRecord.TryGetValue(field2Name, out var field2Raw) || field2Raw is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        var value1 = ConvertToDecimal(input);
        var value2 = ConvertToDecimal(field2Raw);
        var average = (value1 + value2) / 2m;

        return Task.FromResult(GenericResult<object?>.Success(average));
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
                $"Average does not support input type '{input.GetType().Name}'. Input must be numeric.")
        };
    }
}
