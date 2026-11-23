using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.Transformations;

/// <summary>
/// Converts an epoch-milliseconds numeric value to <see cref="DateTimeOffset"/> via
/// <see cref="DateTimeOffset.FromUnixTimeMilliseconds"/>.
/// Fails with a non-success result if the input is null or not convertible to a <see langword="long"/>.
/// </summary>
[TypeOption(typeof(TransformationTypes), "FromUnixMilliseconds")]
public sealed class FromUnixMillisecondsFieldTransformer : FieldTransformationBase
{
    private static readonly ILogger Logger = NullLogger.Instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="FromUnixMillisecondsFieldTransformer"/> class.
    /// </summary>
    public FromUnixMillisecondsFieldTransformer()
        : base(
            name: "FromUnixMilliseconds",
            displayName: "From Unix Milliseconds",
            description: "Converts an epoch-milliseconds long (or long-convertible string) to DateTimeOffset. Fails if input is null or not convertible to long.",
            category: "DateTime",
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
            return Task.FromResult(GenericResult<object?>.Failure(FieldTransformerLog.InputIsNull(Logger)));
        }

        if (WholeNumberInputKinds.Read(input) is not { } epochMs)
        {
            return Task.FromResult(GenericResult<object?>.Failure(
                FieldTransformerLog.InputNotConvertibleToLong(Logger, input.GetType().Name)));
        }

        return Task.FromResult(GenericResult<object?>.Success((object?)DateTimeOffset.FromUnixTimeMilliseconds(epochMs)));
    }
}
