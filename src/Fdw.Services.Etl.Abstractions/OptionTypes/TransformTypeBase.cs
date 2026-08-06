using Fdw.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Abstract base class for transform types that define data transformations in ETL pipelines.
/// </summary>
public abstract class TransformTypeBase : TypeOptionBase<int, TransformTypeBase>, ITypeOption<int, TransformTypeBase>, ITransformType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransformTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this transform type.</param>
    /// <param name="name">The name of this transform type.</param>
    /// <param name="displayName">The display name for UI presentation.</param>
    /// <param name="description">The description of what this transform does.</param>
    /// <param name="category">The category of this transform.</param>
    /// <param name="modifiesStructure">Whether this transform modifies the record structure.</param>
    /// <param name="canFilterRecords">Whether this transform can filter out records.</param>
    protected TransformTypeBase(
        int id,
        string name,
        string displayName,
        string description,
        string category,
        bool modifiesStructure,
        bool canFilterRecords)
        : base(id, name)
    {
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        ModifiesStructure = modifiesStructure;
        CanFilterRecords = canFilterRecords;
    }

    /// <inheritdoc />
    public new string Category { get; }

    /// <inheritdoc />
    public new string DisplayName { get; }

    /// <inheritdoc />
    public new string Description { get; }

    /// <inheritdoc />
    public bool ModifiesStructure { get; }

    /// <inheritdoc />
    public bool CanFilterRecords { get; }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage] // Abstract method - implementation coverage tested in derived classes
    public abstract Task<IGenericResult<IDictionary<string, object?>>> Transform(
        IDictionary<string, object?> input,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual async Task<IGenericResult<IEnumerable<IDictionary<string, object?>>>> TransformBatch(
        IEnumerable<IDictionary<string, object?>> inputs,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        // Default implementation processes records one at a time
        var results = new List<IDictionary<string, object?>>();
        foreach (var input in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await Transform(input, configuration, context, cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                if (configuration is { } config)
                {
                    // Continue on error behavior can be determined by the pipeline
                    context.ReportError(result.CurrentMessage ?? "Transform failed", input);
                    continue;
                }
                return result.ToNewResult<IEnumerable<IDictionary<string, object?>>>();
            }
            if (result.Value != null)
            {
                results.Add(result.Value);
            }
        }
        return GenericResult<IEnumerable<IDictionary<string, object?>>>.Success(results);
    }

    /// <summary>
    /// Maps a neutral transform-operation spec (the HTTP request DTO, read through
    /// <see cref="ITransformOperationSpec"/>) onto this option's own typed child configuration
    /// collection(s) on <paramref name="target"/>. Mirrors how <see cref="Transform"/> takes an
    /// <see cref="IGenericConfiguration"/> and casts internally — each option validates its own
    /// required parameters and fails loud when they are absent (no silent pass-through).
    /// </summary>
    /// <param name="spec">The neutral operation spec to map from.</param>
    /// <param name="target">The parent transform configuration to populate (cast internally by each option).</param>
    /// <param name="logger">The logger used for the log-and-return MessageLogging failure.</param>
    /// <returns>Success once the option's typed children are populated; failure when required params are missing.</returns>
    public abstract IGenericResult MapSpecToConfiguration(ITransformOperationSpec spec, IGenericConfiguration target, ILogger logger);
}
