using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Schema.Ddl.Results;

/// <summary>
/// Base class for DDL result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class DdlResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected DdlResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DdlResultCodeBase"/> class.
    /// </summary>
    protected DdlResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "DDL", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DdlResultCodeBase"/> class with a categorized number.
    /// </summary>
    protected DdlResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "DDL", isRetryable)
    {
    }
}