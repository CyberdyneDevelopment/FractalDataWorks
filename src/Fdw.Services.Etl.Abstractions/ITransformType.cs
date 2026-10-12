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
    /// Gets whether this transform's output depends on seeing every record at once, so that handing it
    /// the input a batch at a time would give a different answer than handing it the whole set.
    /// </summary>
    /// <remarks>
    /// An aggregate is the case: its groups span the whole input. A pipeline that contains such a step
    /// has to gather its records before transforming; a pipeline whose steps all leave this
    /// <see langword="false"/> can pass records through batch by batch.
    /// </remarks>
    bool RequiresCompleteInput { get; }

    /// <summary>
    /// Gets the named inputs this kind reads. A pipeline stage binds each one to a data set, a data
    /// store container, or an earlier stage's output (FDW-962).
    /// </summary>
    IReadOnlyList<string> InputPorts { get; }

    /// <summary>Gets the named outputs this kind writes, each bound to a destination or left for later stages.</summary>
    IReadOnlyList<string> OutputPorts { get; }

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
