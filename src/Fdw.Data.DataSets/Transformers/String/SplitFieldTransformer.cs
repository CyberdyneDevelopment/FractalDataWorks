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
/// Splits a string field value by a delimiter and returns the element at a specified index.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "Split")]
public sealed class SplitFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SplitFieldTransformer"/> class.
    /// </summary>
    public SplitFieldTransformer()
        : base(
            id: 304,
            name: "Split",
            displayName: "Split",
            description: "Splits a string by a delimiter and returns the element at a specified index.",
            category: "String",
            supportsBatching: true,
            new OperationParameterDefinition
            {
                Name = "delimiter",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Delimiter",
                HelpText = "The delimiter string to split on."
            },
            new OperationParameterDefinition
            {
                Name = "index",
                Kind = "Scalar",
                IsRequired = true,
                DisplayName = "Index",
                HelpText = "The zero-based index of the element to return after splitting."
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

        var value = input.ToString() ?? string.Empty;

        if (!context.Parameters.TryGetValue("delimiter", out var delimiter))
        {
            return Task.FromResult(GenericResult<object?>.Success(value));
        }

        if (!context.Parameters.TryGetValue("index", out var indexStr)
            || !int.TryParse(indexStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
        {
            return Task.FromResult(GenericResult<object?>.Success(value));
        }

        var parts = value.Split(new[] { delimiter }, StringSplitOptions.None);

        if (index < 0 || index >= parts.Length)
        {
            return Task.FromResult(GenericResult<object?>.Success(string.Empty));
        }

        return Task.FromResult(GenericResult<object?>.Success(parts[index]));
    }
}
