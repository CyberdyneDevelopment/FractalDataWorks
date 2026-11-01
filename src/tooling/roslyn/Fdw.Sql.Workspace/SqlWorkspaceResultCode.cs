using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Sql.Workspace;

/// <summary>Concrete <see cref="IResultCode"/> for SQL workspace failures.</summary>
[ExcludeFromCodeCoverage]
public sealed class SqlWorkspaceResultCode : ResultCodeBase
{
    /// <inheritdoc/>
    public SqlWorkspaceResultCode() { }

    /// <inheritdoc/>
    public SqlWorkspaceResultCode(int id, string name, string message, int eventId = 0, bool isRetryable = false)
        : base(id, name, name, eventId == 0 ? id : eventId, ResultSeverities.ByName("Error"), "SqlWorkspace", message, isRetryable)
    {
    }

    /// <summary>
    /// Initializes a new instance from a categorized number (catalog scheme): Id == EventId == number,
    /// Code == "SQLWS-{number}". The handling category is derived from the number, not stored on the code.
    /// </summary>
    public SqlWorkspaceResultCode(int number, string name, IResultSeverity severity, string messageTemplate, bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "SQLWS", isRetryable)
    {
    }
}
