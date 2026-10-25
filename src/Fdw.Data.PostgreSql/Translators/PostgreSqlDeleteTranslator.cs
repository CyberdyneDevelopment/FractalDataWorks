using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.Abstractions;
using Fdw.Data.PostgreSql.Logging;
using Fdw.Data.PostgreSql.Results;
using Fdw.Data.PostgreSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql;
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
/// <item>WHERE clause - from the Filter expression, when the command has one</item>
/// </list>
/// </para>
/// <para>
/// A command with no filter has no condition, so it deletes every record: the container is emptied.
/// </para>
/// <para>
/// A command that carries soft-delete options flags the rows instead (an UPDATE that sets the deleted-flag column).
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

            if (command is ISoftDeletableCommand { SoftDelete: { } soft })
                return Task.FromResult(GenericResult<NpgsqlCommand>.Success(BuildSoftDeleteStatement(dbPath, container, soft, command.Filter)));

            return Task.FromResult(GenericResult<NpgsqlCommand>.Success(
                command.Filter is { Root: not null } filter
                    ? BuildDeleteStatement(dbPath, filter)
                    : CreateCommand($"DELETE FROM {BuildQualifiedTableName(dbPath)}")));
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
    /// Builds the UPDATE that flags the rows a soft delete selects instead of removing them.
    /// </summary>
    private static NpgsqlCommand BuildSoftDeleteStatement(
        IDatabasePath dbPath,
        IStorageContainer container,
        SoftDeleteOptions soft,
        IFilterExpression? filter)
    {
        var command = CreateCommand(string.Empty);
        command.CommandText = BuildSoftDeleteUpdate(
            BuildQualifiedTableName(dbPath),
            dbPath.Dialect,
            soft,
            container,
            "now()",
            filter is { Root: not null } ? BuildWhereClause(filter, dbPath.Dialect, (n, v) => AddParameter(command, n, v)) : null,
            (n, v) => AddParameter(command, n, v));
        return command;
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
