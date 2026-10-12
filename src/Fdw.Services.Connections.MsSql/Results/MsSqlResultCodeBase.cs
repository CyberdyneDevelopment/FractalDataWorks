using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// Base class for MsSql result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class MsSqlResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected MsSqlResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlResultCodeBase"/> class.
    /// </summary>
    protected MsSqlResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "MsSql", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance from a categorized number (catalog scheme): Code = "MSSQL-{number}".
    /// </summary>
    protected MsSqlResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "MSSQL", isRetryable)
    {
    }
}
