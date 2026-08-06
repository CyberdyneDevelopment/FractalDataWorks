using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataStores.SqlServer.Results;

/// <summary>
/// Base class for SqlServer DataStore result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class SqlServerDataStoreResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected SqlServerDataStoreResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerDataStoreResultCodeBase"/> class.
    /// </summary>
    protected SqlServerDataStoreResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "SqlServerDataStore", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance from a categorized number (catalog scheme).
    /// </summary>
    protected SqlServerDataStoreResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "SQLSERVER", isRetryable)
    {
    }
}