using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.PostgreSql.Logging;
using Fdw.Data.PostgreSql.Results;
using Fdw.Data.PostgreSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Translates DeleteCommand to PostgreSQL DELETE statement.
/// </summary>
/// <remarks>
/// <para>
/// Builds PostgreSQL DELETE statements with:
/// <list type="bullet">
/// <item>DELETE FROM - container's physical name with double-quoted identifiers</item>
/// <item>WHERE clause - from Filter expression (REQUIRED for safety)</item>
/// </list>
/// </para>
/// <para>
/// ALWAYS requires a WHERE clause to prevent accidental deletion of all records.
/// </para>
/// </remarks>
[TypeOption(typeof(PostgreSqlDataCommandTranslators), "Delete", RestrictToCurrentCompilation = true)]
public sealed class PostgreSqlDeleteTranslator : PostgreSqlDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlDeleteTranslator"/> class.
    /// </summary>
    public PostgreSqlDeleteTranslator()
        : base("Delete")
    {
    }

    /// <summary>
    /// Base Translate - dispatches to typed overload or returns error for invalid command types.
    /// </summary>
    public override Task<IGenericResult<NpgsqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        if (command is IFilterableCommand filterableCommand)
        {
            return Translate(filterableCommand, container, cancellationToken);
        }

        return Task.FromResult(
            GenericResult<NpgsqlCommand>.Failure(
                PostgreSqlTranslatorLog.InvalidCommandType(
                    NullLogger<PostgreSqlDeleteTranslator>.Instance,
                    "PostgreSqlDeleteTranslator",
                    "IFilterableCommand",
                    command.GetType().Name)));
    }

    /// <summary>
    /// Translates an IFilterableCommand to a PostgreSQL DELETE statement.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Cannot be static - overload resolution pattern requires instance method")]
    public Task<IGenericResult<NpgsqlCommand>> Translate(
        IFilterableCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (container == null)
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("ContainerNull")));
            }

            if (container.Path is not IDatabasePath dbPath)
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("InvalidContainerPath")));
            }

            var filter = command.Filter;
            if (filter == null || filter.Root == null)
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("MissingDeleteFilter")));
            }

            var npgsqlCommand = BuildDeleteStatement(dbPath, filter);

            return Task.FromResult(GenericResult<NpgsqlCommand>.Success(npgsqlCommand));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                GenericResult<NpgsqlCommand>.Failure(
                    PostgreSqlDataResultCodes.ByName("DeleteTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// Builds a complete PostgreSQL DELETE statement.
    /// </summary>
    private static NpgsqlCommand BuildDeleteStatement(
        IDatabasePath dbPath,
        IFilterExpression filter)
    {
        var dialect = dbPath.Dialect;
        var sql = $"DELETE FROM {BuildQualifiedTableName(dbPath)}";

        var command = CreateCommand(sql);

        var whereClause = BuildWhereClause(filter, dialect, (n, v) => AddParameter(command, n, v));
        command.CommandText += $" WHERE {whereClause}";

        return command;
    }
}
