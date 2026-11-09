using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Results;
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
    private static Dictionary<int, ISqlErrorHandler>? _errorClassMap;

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
    /// Looks up a handler by the severity class (<c>SqlException.Class</c>) of an error (FDW-4361).
    /// Returns the matching handler, or the NotFound sentinel for a class no handler claims.
    /// </summary>
    /// <param name="sqlErrorClass">The severity class of the error (11 to 16 are errors the statement or its data caused; 20 and above end the connection).</param>
    /// <returns>The handler for the class, or the NotFound sentinel.</returns>
    public static ISqlErrorHandler ByErrorClass(int sqlErrorClass)
    {
        if (_errorClassMap is null)
        {
            var map = new Dictionary<int, ISqlErrorHandler>();
            foreach (var handler in All())
            {
                foreach (var errorClass in handler.SqlErrorClasses)
                {
                    map.TryAdd(errorClass, handler);
                }
            }

            _errorClassMap = map;
        }

        return _errorClassMap.GetValueOrDefault(sqlErrorClass) ?? NotFound;
    }

    /// <summary>
    /// Looks up the handler for an error: the handler of its number when one claims it, otherwise the handler of its severity class (FDW-4361).
    /// A number always wins over a class, because the class cannot tell a lost login (4060, class 11) from a statement's own error.
    /// </summary>
    /// <param name="sqlErrorNumber">The SQL Server error number.</param>
    /// <param name="sqlErrorClass">The severity class of the error.</param>
    /// <returns>The handler, or the NotFound sentinel when neither claims the error.</returns>
    public static ISqlErrorHandler ByError(int sqlErrorNumber, int sqlErrorClass)
    {
        var byNumber = ByErrorNumber(sqlErrorNumber);
        return ReferenceEquals(byNumber, NotFound) ? ByErrorClass(sqlErrorClass) : byNumber;
    }

    /// <summary>
    /// The messages a failure for a SQL error carries, through the handler for its error number, or its severity class when no handler claims the number. This is the
    /// one place a SQL error becomes a failure: a database error is a fact about the database, not a fault in
    /// the application, so it is returned and never thrown.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The SQL exception the driver raised.</param>
    /// <param name="commandText">The SQL command text that failed.</param>
    /// <returns>The messages for the failure; the handler's structured message is first.</returns>
    public static IReadOnlyList<IGenericMessage> FailureMessages(
        ILogger logger, Microsoft.Data.SqlClient.SqlException exception, string commandText)
        => ByError(exception.Number, exception.Class).CreateFailureMessages(logger, exception, commandText);

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

        /// <inheritdoc />
        /// <remarks>
        /// No handler was written for this error, so the failure carries the exception chain as well: it is
        /// still a SQL error, and the database's own words are the only thing left to say about it.
        /// </remarks>
        public override IReadOnlyList<IGenericMessage> CreateFailureMessages(ILogger logger, Exception ex, string commandText)
            => [CreateFailureMessage(logger, ex, commandText), .. ExceptionResultExtensions.FlattenException(ex)];
    }
}
