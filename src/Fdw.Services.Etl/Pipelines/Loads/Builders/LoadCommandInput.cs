using System;
using System.Collections.Generic;
using Fdw.Commands.Data.Loads;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads.Builders;

/// <summary>
/// What a load step kind builds its commands from (FDW-1109).
/// </summary>
public sealed class LoadCommandInput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoadCommandInput"/> class.
    /// </summary>
    /// <param name="owner">What the load is, for refusal messages.</param>
    /// <param name="row">The load row: its columns, value sources, versioning and soft delete.</param>
    /// <param name="rows">The pipeline's transformed output rows; a kind that does not take rows ignores them.</param>
    /// <param name="source">The resolved source of an insert-from-query; <see langword="null"/> for every other kind.</param>
    /// <param name="now">The moment the load began, the same for every row a value source stamps.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    public LoadCommandInput(
        string owner,
        PipelineLoadStepConfiguration row,
        IReadOnlyList<IDictionary<string, object?>> rows,
        QuerySource? source,
        DateTimeOffset now,
        ILogger logger)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Row = row ?? throw new ArgumentNullException(nameof(row));
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
        Source = source;
        Now = now;
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Gets what the load is, for refusal messages.</summary>
    public string Owner { get; }

    /// <summary>Gets the load row.</summary>
    public PipelineLoadStepConfiguration Row { get; }

    /// <summary>Gets the pipeline's transformed output rows.</summary>
    public IReadOnlyList<IDictionary<string, object?>> Rows { get; }

    /// <summary>Gets the resolved source of an insert-from-query, or <see langword="null"/>.</summary>
    public QuerySource? Source { get; }

    /// <summary>Gets the moment the load began.</summary>
    public DateTimeOffset Now { get; }

    /// <summary>Gets the logger the refusals are written to.</summary>
    public ILogger Logger { get; }
}
