using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// Base class for MsSql Connection result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class MsSqlConnectionResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected MsSqlConnectionResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlConnectionResultCodeBase"/> class.
    /// </summary>
    protected MsSqlConnectionResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "MsSqlConnection", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance from a categorized number (catalog scheme): Code = "MSSQLCONN-{number}".
    /// </summary>
    protected MsSqlConnectionResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "MSSQLCONN", isRetryable)
    {
    }
}
