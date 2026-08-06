using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// Base class for MsSql Data result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class MsSqlDataResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected MsSqlDataResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlDataResultCodeBase"/> class.
    /// </summary>
    protected MsSqlDataResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "MsSqlData", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlDataResultCodeBase"/> class
    /// from a categorized number (Id == EventId == number, Code == "MSSQL-{number}").
    /// </summary>
    protected MsSqlDataResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "MSSQL", isRetryable)
    {
    }
}