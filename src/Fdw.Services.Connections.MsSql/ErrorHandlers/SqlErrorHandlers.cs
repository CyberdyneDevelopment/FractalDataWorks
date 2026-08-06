using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.ErrorHandlers;

/// <summary>
/// TypeCollection of SQL error handlers. Each handler maps one or more SQL Server error numbers
/// to a structured failure message. To add a new error handler, create a new class inheriting
/// from <see cref="SqlErrorHandlerBase"/> with the <see cref="TypeOptionAttribute"/>.
/// </summary>
[TypeCollection(typeof(SqlErrorHandlerBase), typeof(ISqlErrorHandler), typeof(SqlErrorHandlers))]
public abstract partial class SqlErrorHandlers : TypeCollectionBase<SqlErrorHandlerBase, ISqlErrorHandler>
{
    private static Dictionary<int, ISqlErrorHandler>? _errorNumberMap;

    /// <summary>
    /// Looks up a handler by SQL Server error number.
    /// Returns the matching handler, or the NotFound sentinel for unregistered error numbers.
    /// </summary>
    /// <param name="sqlErrorNumber">The SQL Server error number (e.g., 229, 208, 18456).</param>
    /// <returns>The handler for the error number, or the NotFound sentinel for unregistered error numbers.</returns>
    public static ISqlErrorHandler ByErrorNumber(int sqlErrorNumber)
    {
        if (_errorNumberMap is null)
        {
            var map = new Dictionary<int, ISqlErrorHandler>();
            foreach (var handler in All())
            {
                foreach (var errorNumber in handler.SqlErrorNumbers)
                {
                    map.TryAdd(errorNumber, handler);
                }
            }

            _errorNumberMap = map;
        }

        return _errorNumberMap.GetValueOrDefault(sqlErrorNumber) ?? NotFound;
    }

    /// <summary>
    /// User-declared partial of the generated NotFound sentinel.
    /// Captures full context for unhandled SQL errors.
    /// </summary>
    private partial class NotFoundSqlErrorHandlers
    {
        /// <inheritdoc />
        public override IReadOnlyList<int> SqlErrorNumbers => [];

        /// <inheritdoc />
        public override bool IsRetryable => false;

        /// <inheritdoc />
        public override IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText)
            => MsSqlConnectionLogger.UnhandledSqlError(logger, ex, commandText,
                ex is Microsoft.Data.SqlClient.SqlException sqlEx ? sqlEx.Number : 0,
                ex.Message);
    }
}
