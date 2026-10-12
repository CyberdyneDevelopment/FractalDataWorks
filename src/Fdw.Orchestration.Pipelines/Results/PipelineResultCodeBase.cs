using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Orchestration.Pipelines.Results;

/// <summary>
/// Base class for Pipeline result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class PipelineResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected PipelineResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineResultCodeBase"/> class.
    /// </summary>
    protected PipelineResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "Pipeline", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineResultCodeBase"/> class with a categorized number.
    /// </summary>
    protected PipelineResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "PIPELINE", isRetryable)
    {
    }
}
