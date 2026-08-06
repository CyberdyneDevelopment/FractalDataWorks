using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Sql.Commands.Abstractions.Results;

/// <summary>Concrete <see cref="IResultCode"/> for SQL command failures.</summary>
[ExcludeFromCodeCoverage]
public sealed class SqlResultCode : ResultCodeBase
{
    /// <inheritdoc/>
    public SqlResultCode() { }

    /// <inheritdoc/>
    public SqlResultCode(int id, string name, string message, int eventId = 0, bool isRetryable = false)
        : base(id, name, name, eventId == 0 ? id : eventId, ResultSeverities.ByName("Error"), "SqlCommands", message, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance from a categorized <paramref name="number"/> (catalog scheme):
    /// Id == EventId == number and Code == "SQL-{number}".
    /// </summary>
    public SqlResultCode(int number, string name, IResultSeverity severity, string messageTemplate, bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "SQL", isRetryable)
    {
    }
}
