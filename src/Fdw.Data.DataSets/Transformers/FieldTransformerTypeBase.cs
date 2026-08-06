using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>
/// Base class for field-level transform TypeOptions in the <see cref="DataTransformerTypes"/> collection.
/// Extends <see cref="DataTransformerTypeBase"/> with parameter definitions, single-value execution,
/// and optional batch execution for transforms that don't reference other fields.
/// </summary>
public abstract class FieldTransformerTypeBase : DataTransformerTypeBase,
    IDataTransformer<object?, object?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldTransformerTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this field transformer type.</param>
    /// <param name="name">The name of this field transformer type.</param>
    /// <param name="displayName">The display name for this field transformer type.</param>
    /// <param name="description">A description of what this field transformer does.</param>
    /// <param name="category">The category (String, DateTime, Numeric, Injection, Conditional, Boolean).</param>
    /// <param name="supportsBatching">Whether this transform can operate on columns rather than rows.</param>
    /// <param name="parameters">The parameter definitions this transform accepts.</param>
    protected FieldTransformerTypeBase(
        int id,
        string name,
        string displayName,
        string description,
        string category,
        bool supportsBatching,
        params OperationParameterDefinition[] parameters)
        : base(id, name, displayName, description, supportsStreaming: supportsBatching, category)
    {
        ExpectedParameters = parameters;
        SupportsBatching = supportsBatching;
    }

    /// <summary>
    /// Gets the parameter definitions that this field transformer expects.
    /// Parameter values are stored in transform.FieldMappingTransformParameter and passed
    /// to <see cref="Execute"/> at runtime.
    /// </summary>
    /// <inheritdoc/>
    public string TransformerName => Name;

    /// <summary>
    /// Gets the parameter definitions that this field transformer expects.
    /// Parameter values are stored in transform.FieldMappingTransformParameter and passed
    /// to <see cref="Execute"/> at runtime.
    /// </summary>
    public IReadOnlyList<OperationParameterDefinition> ExpectedParameters { get; }

    /// <summary>
    /// Gets a value indicating whether this transform can operate on an entire column at once.
    /// Transforms that reference <see cref="FieldTransformContext.CurrentRecord"/> must return false.
    /// </summary>
    public bool SupportsBatching { get; }

    /// <summary>
    /// Transforms a single field value via the <see cref="IDataTransformer{TResult, TInput}"/> seam.
    /// </summary>
    /// <remarks>
    /// Why: this is the cross-assembly async seam; it is one delegating implementation in the base so
    /// every concrete field transformer overrides only the single <see cref="Execute"/> method (no
    /// duplicated Transform/Execute pair). It awaits the real <see cref="Execute"/> — no sync-over-async.
    /// </remarks>
    /// <param name="input">The field value to transform (may be null).</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>The transformed value wrapped in a result.</returns>
    public Task<IGenericResult<object?>> Transform(
        object? input,
        CancellationToken cancellationToken = default)
    {
        // Why: single-expression passthrough to Execute — return the Task directly (no async state
        // machine; AsyncFixer01). Execute carries the real (possibly I/O-bound) async work.
        return Execute(
            input,
            new Dictionary<string, string>(StringComparer.Ordinal),
            new FieldTransformContext { CancellationToken = cancellationToken },
            cancellationToken);
    }

    /// <summary>
    /// Execute a single-value field transform with parameters and context.
    /// </summary>
    /// <param name="input">The field value to transform (may be null).</param>
    /// <param name="parameters">Parameter values from transform.FieldMappingTransformParameter.</param>
    /// <param name="context">Runtime context including current record and operating date.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>The transformed value wrapped in a result.</returns>
    public abstract Task<IGenericResult<object?>> Execute(
        object? input,
        IReadOnlyDictionary<string, string> parameters,
        FieldTransformContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a batch of field transforms for an entire column.
    /// Default implementation iterates <see cref="Execute"/> per value.
    /// TypeOptions that support batching should override for vectorized performance.
    /// </summary>
    /// <param name="inputs">All values for this field across the record set.</param>
    /// <param name="parameters">Parameter values from transform.FieldMappingTransformParameter.</param>
    /// <param name="context">Runtime context (CurrentRecord is not populated in batch mode).</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>The transformed values wrapped in a result.</returns>
    public virtual async Task<IGenericResult<IReadOnlyList<object?>>> ExecuteBatch(
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
