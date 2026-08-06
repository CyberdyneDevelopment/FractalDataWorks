using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.ErrorHandlers;

/// <summary>
/// Interface for SQL error handlers that dispatch by SQL Server error number.
/// </summary>
public interface ISqlErrorHandler : ITypeOption<int, SqlErrorHandlerBase>
{
    /// <summary>
    /// Gets the SQL Server error numbers that this handler handles.
    /// Multiple numbers can map to the same handler (e.g., -1, 2, 53 all mean connection failed).
    /// </summary>
    IReadOnlyList<int> SqlErrorNumbers { get; }

    /// <summary>
    /// Gets a value indicating whether the error condition is retryable.
    /// </summary>
    bool IsRetryable { get; }

    /// <summary>
    /// Creates a structured failure message for this SQL error, logging the error in the process.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="ex">The caught exception.</param>
    /// <param name="commandText">The SQL command text that failed.</param>
    /// <returns>A generic message describing the failure.</returns>
    IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText);
}
