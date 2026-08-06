using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base interface for data transformers (non-generic marker).
/// </summary>
public interface IDataTransformer
{
    /// <summary>
    /// Transformer name.
    /// </summary>
    string TransformerName { get; }
}

/// <summary>
/// Interface for data transformers that apply ETL-style transformations.
/// Simple generic interface with single Transform method.
/// Examples: AggregateTransformer, FilterTransformer, JoinTransformer, CalculatedFieldTransformer
/// </summary>
/// <typeparam name="TResult">The result type after transformation.</typeparam>
/// <typeparam name="TInput">The input type to transform.</typeparam>
public interface IDataTransformer<TResult, TInput> : IDataTransformer
{
    /// <summary>
    /// Transform input data to result data.
    /// </summary>
    /// <param name="input">Input data to transform.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformation result.</returns>
    Task<IGenericResult<TResult>> Transform(
        TInput input,
        CancellationToken cancellationToken = default);
}
