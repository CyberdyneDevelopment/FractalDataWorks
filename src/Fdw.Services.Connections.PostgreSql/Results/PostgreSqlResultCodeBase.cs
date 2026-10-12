using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.PostgreSql.Results;

/// <summary>
/// Base class for PostgreSQL result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class PostgreSqlResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected PostgreSqlResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlResultCodeBase"/> class.
    /// </summary>
    protected PostgreSqlResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "PostgreSql", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlResultCodeBase"/> class
    /// from a categorized number (Id == EventId == number; Code == "PGSQL-{number}").
    /// </summary>
    protected PostgreSqlResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "PGSQL", isRetryable)
    {
    }
}
