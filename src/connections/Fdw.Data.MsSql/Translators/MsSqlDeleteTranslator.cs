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
/// <item>WHERE clause - from the Filter expression, when the command has one</item>
/// </list>
/// </para>
/// <para>
/// A command with no filter and no soft delete has no condition, so it deletes every record: the container is emptied.
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

            // A soft delete flags rows instead of removing them (FDW-1107).
            var soft = MsSqlSoftDeleteSql.For(command, container, dbPath.Dialect, allowAbsentSet: true);
            if (soft.Applies || soft.Problem is not null)
            {
                return Task.FromResult(SoftDelete(command, dbPath, soft));
            }

            // A command with no filter has no condition, so it deletes every row
            return Task.FromResult(GenericResult<SqlCommand>.Success(
                command.Filter is { Root: not null } filter
                    ? BuildDeleteStatement(dbPath, filter)
                    : CreateCommand($"DELETE FROM {BuildQualifiedTableName(dbPath)}")));
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
    /// Translates a delete that carries soft-delete options: the rows the filter matches and the mode selects (the marker
    /// matches, or the keys are absent from the source set) are flagged deleted instead of removed.
    /// </summary>
    private static IGenericResult<SqlCommand> SoftDelete(IFilterableCommand command, IDatabasePath dbPath, MsSqlSoftDeleteFragments soft)
    {
        if (soft.Problem is not null)
            return MsSqlSoftDeleteSql.Refusal(soft);

        // The soft-delete mode is itself the row selector (a marker or an absent-key set), so no filter is required.
        var filter = command.Filter?.Root is null ? null : command.Filter;
        var dialect = dbPath.Dialect;
        var built = MsSqlSoftDeleteSql.BuildDelete(
            BuildQualifiedTableName(dbPath),
            dialect,
            soft,
            statement => filter is null ? null : BuildWhereClause(filter, dialect, (n, v) => AddParameter(statement, n, v), "t"),
            out var problem);
        return built is null
            ? GenericResult<SqlCommand>.Failure(
                MsSqlDataResultCodes.ByName("InvalidSoftDeleteOptions"),
                ResultDetails.Create("Reason", problem))
            : GenericResult<SqlCommand>.Success(built);
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
