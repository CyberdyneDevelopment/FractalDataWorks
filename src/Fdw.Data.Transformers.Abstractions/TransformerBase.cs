using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Results;

namespace Fdw.Data.Transformers.Abstractions;

/// <summary>
/// Base class for implementing data transformers with type safety.
/// </summary>
/// <typeparam name="TIn">The input type.</typeparam>
/// <typeparam name="TOut">The output type.</typeparam>
public abstract class TransformerBase<TIn, TOut> : IDataTransformer<TIn, TOut>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransformerBase{TIn, TOut}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The transformer name.</param>
    protected TransformerBase(int id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Transformer name cannot be null or whitespace.", nameof(name));
        }

        Id = id;
        Name = name;
        SourceType = typeof(TIn);
        TargetType = typeof(TOut);
    }

    /// <inheritdoc/>
    public int Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public Type SourceType { get; }

    /// <inheritdoc/>
    public Type TargetType { get; }

    /// <inheritdoc/>
    public abstract IGenericResult<IEnumerable<TOut>> Transform(
        IEnumerable<TIn> source,
        TransformContext context,
        CancellationToken cancellationToken = default);
}
