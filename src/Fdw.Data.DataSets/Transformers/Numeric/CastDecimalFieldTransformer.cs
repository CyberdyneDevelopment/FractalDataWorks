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
/// Casts a numeric input (double, float, int, long) to decimal.
/// Returns null for null input. Throws for unsupported input types.
/// </summary>
[TypeOption(typeof(DataTransformerTypes), "CastDecimal")]
public sealed class CastDecimalFieldTransformer : FieldTransformerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CastDecimalFieldTransformer"/> class.
    /// </summary>
    public CastDecimalFieldTransformer()
        : base(
            id: 202,
            name: "CastDecimal",
            displayName: "Cast to Decimal",
            description: "Casts a numeric input (double, float, int, long) to decimal. Returns null for null input.",
            category: "Numeric",
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

        return Task.FromResult<IGenericResult<object?>>(input switch
        {
            decimal d => GenericResult<object?>.Success(d),
            double d => GenericResult<object?>.Success(Convert.ToDecimal(d)),
            float f => GenericResult<object?>.Success(Convert.ToDecimal(f)),
            int i => GenericResult<object?>.Success(Convert.ToDecimal(i)),
            long l => GenericResult<object?>.Success(Convert.ToDecimal(l)),
            short s => GenericResult<object?>.Success(Convert.ToDecimal(s)),
            byte b => GenericResult<object?>.Success(Convert.ToDecimal(b)),
            _ => throw new InvalidOperationException(
                $"CastDecimal does not support input type '{input.GetType().Name}'. " +
                "Supported types: decimal, double, float, int, long, short, byte.")
        });
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<IReadOnlyList<object?>>> ExecuteBatch(
        IReadOnlyList<object?> inputs,
        IReadOnlyDictionary<string, string> parameters,
        FieldTransformContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<object?>(inputs.Count);
        foreach (var input in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await Execute(input, parameters, context, cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                return result.ToNewResult<IReadOnlyList<object?>>();
            }

            results.Add(result.Value);
        }

        return GenericResult<IReadOnlyList<object?>>.Success(results);
    }
}
