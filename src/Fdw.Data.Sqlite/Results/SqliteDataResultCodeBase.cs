using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// Base class for SQLite data result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class SqliteDataResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected SqliteDataResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteDataResultCodeBase"/> class
    /// from a categorized number (Id == EventId == number, Code == "SQLITE-{number}").
    /// </summary>
    protected SqliteDataResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "SQLITE", isRetryable)
    {
    }
}
