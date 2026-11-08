using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.ErrorHandlers;

/// <summary>
/// Handles the errors SQL Server raises for a statement or its data on a connection that works (FDW-4361): severity classes 11 to 16, for a number
/// no other handler claims. A syntax error, a divide by zero (8134), a conversion or truncation, an error the script raised.
/// </summary>
/// <remarks>
/// Why a class and not a list of numbers: the numbers are open ended (every <c>RAISERROR</c> and <c>THROW</c> the script writes), and the severity is
/// what SQL Server documents as "the user can correct it". The failure is a Validation (its code is in the 2xxxx band), so a run that stops on it counts
/// toward a failure threshold and is not read as an unreachable connection; it is not retryable.
/// </remarks>
[TypeOption(typeof(SqlErrorHandlers), "StatementFailed")]
[ExcludeFromCodeCoverage]
public sealed class StatementFailedHandler : SqlErrorHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StatementFailedHandler"/> class.
    /// </summary>
    public StatementFailedHandler() : base("StatementFailed") { }

    /// <inheritdoc />
    public override IReadOnlyList<int> SqlErrorNumbers => [];

    /// <inheritdoc />
    public override IReadOnlyList<int> SqlErrorClasses => [11, 12, 13, 14, 15, 16];

    /// <inheritdoc />
    public override bool IsRetryable => false;

    /// <inheritdoc />
    public override IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText)
        => MsSqlConnectionLogger.SqlStatementFailed(logger, ex, commandText,
            ex is Microsoft.Data.SqlClient.SqlException sqlEx ? sqlEx.Number : 0,
            ex.Message);
}
