using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Data.Transformations;

/// <summary>
/// When the input is null, returns an empty string.
/// </summary>
[TypeOption(typeof(TransformationTypes), "NullToEmpty")]
public sealed class NullToEmptyFieldTransformer : FieldTransformationBase
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
    public override Task<IGenericResult<object?>> Transform(
        object? input,
        TransformationContext context,
        CancellationToken cancellationToken = default)
    {
        if (input is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(string.Empty));
        }

        return Task.FromResult(GenericResult<object?>.Success(input));
    }
}
