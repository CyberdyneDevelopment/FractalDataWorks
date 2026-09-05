using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.ErrorHandlers;

/// <summary>
/// Handles SQL Server errors 4060 and 4064: the target database is unavailable to this login.
/// The database does not exist, was dropped and recreated, or the login has no user mapped in
/// it -- distinct from error 18456 (LoginFailedHandler), which is a server-level authentication
/// failure rather than a database-level access failure.
/// </summary>
[TypeOption(typeof(SqlErrorHandlers), "DatabaseUnavailable")]
[ExcludeFromCodeCoverage]
public sealed class DatabaseUnavailableHandler : SqlErrorHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseUnavailableHandler"/> class.
    /// </summary>
    public DatabaseUnavailableHandler() : base(7, "DatabaseUnavailable") { }

    /// <inheritdoc />
    public override IReadOnlyList<int> SqlErrorNumbers => [4060, 4064];

    /// <inheritdoc />
    public override bool IsRetryable => false;

    /// <inheritdoc />
    public override IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText)
        => MsSqlConnectionLogger.SqlDatabaseUnavailable(logger, ex, commandText);
}
