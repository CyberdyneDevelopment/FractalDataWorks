using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Logging;
using Fdw.Data.MsSql.Results;
using Fdw.Data.MsSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Translates DeleteCommand to T-SQL DELETE statement.
/// </summary>
/// <remarks>
/// <para>
/// Builds T-SQL DELETE statements with:
/// <list type="bullet">
/// <item>DELETE FROM - container's physical name</item>
/// <item>WHERE clause - from Filter expression (REQUIRED for safety)</item>
/// </list>
/// </para>
/// <para>
/// ALWAYS requires a WHERE clause to prevent accidental deletion of all records.
/// Use Filter expression to specify which records to delete.
/// </para>
/// </remarks>
[TypeOption(typeof(MsSqlDataCommandTranslators), "Delete", RestrictToCurrentCompilation = true)]
public sealed class MsSqlDeleteTranslator : MsSqlDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlDeleteTranslator"/> class.
    /// </summary>
    public MsSqlDeleteTranslator()
        : base("Delete")
    {
    }

    /// <summary>
    /// Base Translate - dispatches to typed overload or returns error for invalid command types.
    /// </summary>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        if (command is IFilterableCommand filterableCommand)
        {
            return Translate(filterableCommand, container, cancellationToken);
        }

        return Task.FromResult(
            GenericResult<SqlCommand>.Failure(
                MsSqlTranslatorLog.InvalidCommandType(
                    NullLogger<MsSqlDeleteTranslator>.Instance,
                    "MsSqlDeleteTranslator",
                    "IFilterableCommand",
                    command.GetType().Name)));
    }

    /// <summary>
    /// Translates an IFilterableCommand to a T-SQL DELETE statement.
    /// Overload resolution ensures this method is called for DeleteCommand instances.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Cannot be static - overload resolution pattern requires instance method")]
    public Task<IGenericResult<SqlCommand>> Translate(
        IFilterableCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (container == null)
            {
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("ContainerNull")));
            }

            // Validate container path is a database path
            if (container.Path is not IDatabasePath dbPath)
            {
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("InvalidContainerPath")));
            }

            // Get filter from strongly-typed interface (no reflection!)
            var filter = command.Filter;
            if (filter == null || filter.Root == null)
            {
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("MissingDeleteFilter")));
            }

            // Build DELETE statement
            var sqlCommand = BuildDeleteStatement(dbPath, filter);

            return Task.FromResult(GenericResult<SqlCommand>.Success(sqlCommand));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                GenericResult<SqlCommand>.Failure(
                    MsSqlDataResultCodes.ByName("DeleteTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// Builds a complete T-SQL DELETE statement.
    /// </summary>
    private static SqlCommand BuildDeleteStatement(
        IDatabasePath dbPath,
        IFilterExpression filter)
    {
        var dialect = dbPath.Dialect;

        // Build DELETE statement
        var sql = $"DELETE FROM {BuildQualifiedTableName(dbPath)}";

        // Get SqlCommand
        var command = CreateCommand(sql);

        // Build WHERE clause from filter
        var whereClause = BuildWhereClause(filter, dialect, (n, v) => AddParameter(command, n, v));
        command.CommandText += $" WHERE {whereClause}";

        return command;
    }
}
