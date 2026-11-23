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
/// Casts a numeric input (double, float, int, long) to decimal.
/// Returns null for null input. Throws for unsupported input types.
/// </summary>
[TypeOption(typeof(TransformationTypes), "CastDecimal")]
public sealed class CastDecimalFieldTransformer : FieldTransformationBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CastDecimalFieldTransformer"/> class.
    /// </summary>
    public CastDecimalFieldTransformer()
        : base(
            name: "CastDecimal",
            displayName: "Cast to Decimal",
            description: "Casts a numeric input (double, float, int, long) to decimal. Returns null for null input.",
            category: "Numeric",
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
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        return Task.FromResult(GenericResult<object?>.Success(NumericInputKinds.ToDecimal(input, "CastDecimal")));
    }

}
