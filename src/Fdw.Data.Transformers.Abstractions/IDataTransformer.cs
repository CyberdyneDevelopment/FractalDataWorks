using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Results;

namespace Fdw.Data.Transformers.Abstractions;

/// <summary>
/// Base interface for all data transformers.
/// Transforms data from one schema/format to another.
/// </summary>
public interface IDataTransformer
{
    /// <summary>
    /// Gets the unique identifier for this transformer.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the name of this transformer.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the source type this transformer accepts.
    /// </summary>
    Type SourceType { get; }

    /// <summary>
    /// Gets the target type this transformer produces.
    /// </summary>
    Type TargetType { get; }
}

/// <summary>
/// Generic interface for typed data transformers.
/// </summary>
/// <typeparam name="TIn">The input type.</typeparam>
/// <typeparam name="TOut">The output type.</typeparam>
public interface IDataTransformer<TIn, TOut> : IDataTransformer
{
    /// <summary>
    /// Transforms a collection of input records to output records.
    /// </summary>
    /// <param name="source">The source records to transform.</param>
    /// <param name="context">The transformation context with metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transformed records.</returns>
    IGenericResult<IEnumerable<TOut>> Transform(
        IEnumerable<TIn> source,
        TransformContext context,
        CancellationToken cancellationToken = default);
}
