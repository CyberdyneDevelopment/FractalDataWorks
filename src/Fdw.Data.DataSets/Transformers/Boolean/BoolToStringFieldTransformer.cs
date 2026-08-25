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
/// Maps a boolean field value to one of two configured string labels.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "BoolToString")]
public sealed class BoolToStringFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoolToStringFieldTransformer"/> class.
    /// </summary>
    public BoolToStringFieldTransformer()
        : base(
            id: 401,
            name: "BoolToString",
            displayName: "Bool to String",
            description: "Maps a boolean value to one of two configured string labels.",
            category: "Boolean",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "trueLabel",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "True Label",
                HelpText = "The string to return when the value is true."
            },
            new OperationParameterDefinition
            {
                Name = "falseLabel",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "False Label",
                HelpText = "The string to return when the value is false."
            })
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<object?>> Transform(
        object? input,
        FieldTransformContext context,
        CancellationToken cancellationToken = default)
    {
        context.Parameters.TryGetValue("trueLabel", out var trueLabel);
        context.Parameters.TryGetValue("falseLabel", out var falseLabel);

        trueLabel ??= string.Empty;
        falseLabel ??= string.Empty;

        if (input is bool boolValue)
        {
            return Task.FromResult(GenericResult<object?>.Success(boolValue ? trueLabel : falseLabel));
        }

        if (input is not null
            && bool.TryParse(input.ToString() ?? string.Empty, out var parsed))
        {
            return Task.FromResult(GenericResult<object?>.Success(parsed ? trueLabel : falseLabel));
        }

        return Task.FromResult(GenericResult<object?>.Success(falseLabel));
    }
}
