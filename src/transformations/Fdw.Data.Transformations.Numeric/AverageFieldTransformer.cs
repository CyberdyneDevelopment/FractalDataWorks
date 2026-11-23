using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Data.Transformations;

/// <summary>
/// Computes the average of the input value and a second field value from the current record.
/// Both values must be numeric (converted to decimal). Result is decimal.
/// Does not support batching because it reads <see cref="TransformationContext.CurrentRecord"/>.
/// </summary>
[TypeOption(typeof(TransformationTypes), "Average")]
public sealed class AverageFieldTransformer : FieldTransformationBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AverageFieldTransformer"/> class.
    /// </summary>
    public AverageFieldTransformer()
        : base(
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
    public override Task<IGenericResult<object?>> Transform(
        object? input,
        TransformationContext context,
        CancellationToken cancellationToken = default)
    {
        if (input is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        if (!context.Parameters.TryGetValue("field2", out var field2Name)
            || string.IsNullOrWhiteSpace(field2Name))
        {
            throw new InvalidOperationException(
                "Average transform requires a 'field2' parameter.");
        }

        if (!context.CurrentRecord.TryGetValue(field2Name, out var field2Raw) || field2Raw is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        var value1 = NumericInputKinds.ToDecimal(input, "Average");
        var value2 = NumericInputKinds.ToDecimal(field2Raw, "Average");
        var average = (value1 + value2) / 2m;

        return Task.FromResult(GenericResult<object?>.Success(average));
    }
}
