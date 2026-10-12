using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// Base class for PostgreSQL Data result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class PostgreSqlDataResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected PostgreSqlDataResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlDataResultCodeBase"/> class.
    /// </summary>
    protected PostgreSqlDataResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "PostgreSqlData", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlDataResultCodeBase"/> class with a categorized number.
    /// </summary>
    protected PostgreSqlDataResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "PGSQL", isRetryable)
    {
    }
}
