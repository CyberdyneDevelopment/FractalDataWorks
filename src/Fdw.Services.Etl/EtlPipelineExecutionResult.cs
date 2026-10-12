using System;
using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl;

/// <summary>
/// Implementation of IEtlPipelineExecutionResult.
/// </summary>
public sealed class EtlPipelineExecutionResult : IEtlPipelineExecutionResult
{
    private readonly List<string> _errors = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EtlPipelineExecutionResult"/> class.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    public EtlPipelineExecutionResult(Guid executionId)
    {
        ExecutionId = executionId;
        StartedAt = DateTime.UtcNow;
    }

    /// <inheritdoc />
    public Guid ExecutionId { get; }

    /// <inheritdoc />
    public bool IsSuccess => RecordsFailed == 0 && _errors.Count == 0;

    /// <inheritdoc />
    public int RecordsExtracted { get; set; }

    /// <inheritdoc />
    public int RecordsTransformed { get; set; }

    /// <inheritdoc />
    public int RecordsLoaded { get; set; }

    /// <inheritdoc />
    public int RecordsFailed { get; set; }

    /// <inheritdoc />
    public TimeSpan ExtractDuration { get; set; }

    /// <inheritdoc />
    public TimeSpan TransformDuration { get; set; }

    /// <inheritdoc />
    public TimeSpan LoadDuration { get; set; }

    /// <inheritdoc />
    public TimeSpan TotalDuration => (CompletedAt ?? DateTime.UtcNow) - StartedAt;

    /// <inheritdoc />
    public DateTime StartedAt { get; }

    /// <inheritdoc />
    public DateTime? CompletedAt { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<string> Errors => _errors.AsReadOnly();

    /// <summary>
    /// Adds an error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    public void AddError(string error)
    {
        _errors.Add(error);
    }

    /// <summary>
    /// Marks the execution as complete.
    /// </summary>
    public void Complete()
    {
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="extracted">Records extracted.</param>
    /// <param name="transformed">Records transformed.</param>
    /// <param name="loaded">Records loaded.</param>
    /// <returns>A successful execution result.</returns>
    public static EtlPipelineExecutionResult Success(Guid executionId, int extracted, int transformed, int loaded)
    {
        return new EtlPipelineExecutionResult(executionId)
        {
            RecordsExtracted = extracted,
            RecordsTransformed = transformed,
            RecordsLoaded = loaded,
            CompletedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="error">The error message.</param>
    /// <returns>A failed execution result.</returns>
    public static EtlPipelineExecutionResult Failed(Guid executionId, string error)
    {
        var result = new EtlPipelineExecutionResult(executionId)
        {
            CompletedAt = DateTime.UtcNow
        };
        result.AddError(error);
        return result;
    }
}
