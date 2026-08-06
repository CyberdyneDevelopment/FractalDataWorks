using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Types.MsSql;

/// <summary>
/// Base class for Types MsSql result codes.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class MsSqlTypesResultCodeBase : ResultCodeBase
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected MsSqlTypesResultCodeBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlTypesResultCodeBase"/> class.
    /// </summary>
    protected MsSqlTypesResultCodeBase(
        int id,
        string name,
        string code,
        int eventId,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(id, name, code, eventId, severity, "Types", messageTemplate, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance from a categorized number (catalog scheme):
    /// Id == EventId == number and Code == "TYPES-{number}".
    /// </summary>
    protected MsSqlTypesResultCodeBase(
        int number,
        string name,
        IResultSeverity severity,
        string messageTemplate,
        bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "TYPES", isRetryable)
    {
    }
}