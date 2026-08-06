using Fdw.Configuration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Interface for data transformation types in ETL pipelines.
/// Extends ITypeOption for TypeCollection support.
/// </summary>
public interface ITransformType : ITypeOption<int>
{
    /// <summary>
    /// Gets the category of this transform.
    /// </summary>
    new string Category { get; }

    /// <summary>
    /// Gets the display name for UI presentation.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the description of what this transform does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets whether this transform modifies the record structure.
    /// </summary>
    bool ModifiesStructure { get; }

    /// <summary>
    /// Gets whether this transform can filter out records.
    /// </summary>
    bool CanFilterRecords { get; }

    /// <summary>
    /// Transforms a single record.
    /// </summary>
    /// <param name="input">The input record.</param>
    /// <param name="configuration">Transform configuration.</param>
    /// <param name="context">Transform execution context.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>The transformed record or failure.</returns>
    Task<IGenericResult<IDictionary<string, object?>>> Transform(
        IDictionary<string, object?> input,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transforms a batch of records.
    /// </summary>
    /// <param name="inputs">The input records.</param>
    /// <param name="configuration">Transform configuration.</param>
    /// <param name="context">Transform execution context.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>The transformed records or failure.</returns>
    Task<IGenericResult<IEnumerable<IDictionary<string, object?>>>> TransformBatch(
        IEnumerable<IDictionary<string, object?>> inputs,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default);
}
