using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Results;

/// <summary>
/// Base class for Scheduling result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class SchedulingResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected SchedulingResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingResultCodeBase"/> class.
    /// </summary>
    protected SchedulingResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "Scheduling", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingResultCodeBase"/> class from a categorized number.
    /// </summary>
    protected SchedulingResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "SCHEDULING", isRetryable)
    {
    }
}