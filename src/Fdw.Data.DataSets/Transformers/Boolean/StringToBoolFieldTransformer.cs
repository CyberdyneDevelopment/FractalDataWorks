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
/// Converts a string field value to a boolean by comparing against a configured true value.
/// </summary>
[TypeOption(typeof(TransformationTypes), "StringToBool")]
public sealed class StringToBoolFieldTransformer : FieldTransformationBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringToBoolFieldTransformer"/> class.
    /// </summary>
    public StringToBoolFieldTransformer()
        : base(
            id: 400,
            name: "StringToBool",
            displayName: "String to Bool",
            description: "Converts a string value to a boolean by case-insensitive comparison against a configured true value.",
            category: "Boolean",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "trueValue",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "True Value",
                HelpText = "The string value that should be interpreted as true (case-insensitive)."
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
            return Task.FromResult(GenericResult<object?>.Success((object)false));
        }

        var value = input.ToString() ?? string.Empty;

        if (!context.Parameters.TryGetValue("trueValue", out var trueValue))
        {
            return Task.FromResult(GenericResult<object?>.Success((object)false));
        }

        var result = string.Equals(value, trueValue, StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(GenericResult<object?>.Success((object)result));
    }
}
