using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Data.DataSets;

/// <summary>
/// Converts a string field value to lowercase using invariant culture.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "ToLower")]
public sealed class ToLowerFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToLowerFieldTransformer"/> class.
    /// </summary>
    public ToLowerFieldTransformer()
        : base(
            id: 303,
            name: "ToLower",
            displayName: "To Lower",
            description: "Converts a string value to lowercase using invariant culture.",
            category: "String",
            supportsBatching: true)
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

        var value = input.ToString() ?? string.Empty;

        return Task.FromResult(GenericResult<object?>.Success(value.ToLower(CultureInfo.InvariantCulture)));
    }
}
