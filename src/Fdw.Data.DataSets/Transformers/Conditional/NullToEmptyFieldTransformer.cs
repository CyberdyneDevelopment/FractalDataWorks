using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Data.DataSets;

/// <summary>
/// When the input is null, returns an empty string.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "NullToEmpty")]
public sealed class NullToEmptyFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullToEmptyFieldTransformer"/> class.
    /// </summary>
    public NullToEmptyFieldTransformer()
        : base(
            id: 601,
            name: "NullToEmpty",
            displayName: "Null to Empty",
            description: "When the input is null, returns an empty string.",
            category: "Conditional",
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
            return Task.FromResult(GenericResult<object?>.Success(string.Empty));
        }

        return Task.FromResult(GenericResult<object?>.Success(input));
    }
}
