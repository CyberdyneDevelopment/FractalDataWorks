using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Conventions;
using Fdw.Data.Abstractions;
using Fdw.Data.PostgreSql.Results;
using Fdw.Data.PostgreSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Npgsql;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Translates BulkInsertCommand to batched multi-row INSERT statements, and plans the COPY the connection runs when the command
/// is rerouted to bulk (FDW-1152).
/// </summary>
/// <remarks>
/// A bulk insert runs one of two ways, chosen by the connection before it translates: up to the connection's configured
/// <c>MaxSerialInsert</c> rows it runs serially, translated to the same batched INSERT statements a batch insert gets; above that
/// the command answers <see cref="IDatabaseConnectionCommand.ReroutesToBulk"/> and the connection takes the bulk route with
/// <see cref="Plan"/>, which builds the COPY statement, the column order and the rows. No SQL is faked: the translator never
/// returns a command that is not SQL.
/// </remarks>
[TypeOption(typeof(PostgreSqlDataCommandTranslators), "BulkInsert", RestrictToCurrentCompilation = true)]
public sealed class PostgreSqlBulkInsertTranslator : PostgreSqlDataCommandTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="PostgreSqlBulkInsertTranslator"/> class.</summary>
    public PostgreSqlBulkInsertTranslator()
        : base("BulkInsert")
    {
    }

    /// <summary>Translates a BulkInsertCommand to batched INSERT statements, the serial route.</summary>
    public override Task<IGenericResult<NpgsqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var source = Source(command, container);
            if (source.IsFailure)
                return Task.FromResult(source.ToNewResult<NpgsqlCommand>());

            var (dbPath, collection) = source.Value!;
            return Task.FromResult(GenericResult<NpgsqlCommand>.Success(
                PostgreSqlBatchInsertTranslator.BuildBatchedInsertStatements(container, dbPath, collection)));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<NpgsqlCommand>.Failure(
                PostgreSqlDataResultCodes.ByName("BulkInsertTranslationFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>Plans the COPY of a BulkInsertCommand: the statement, the column order and the rows.</summary>
    /// <param name="command">The bulk insert command, carrying its rows as input data.</param>
    /// <param name="container">The container the rows are copied into.</param>
    /// <returns>The plan, or the coded failure that stopped it.</returns>
    public static IGenericResult<PostgreSqlBulkCopyPlan> Plan(IDataCommand command, IStorageContainer container)
    {
        try
        {
            var source = Source(command, container);
            if (source.IsFailure)
                return source.ToNewResult<PostgreSqlBulkCopyPlan>();

            var (dbPath, collection) = source.Value!;
            var dialect = dbPath.Dialect;
            var fields = PostgreSqlBatchInsertTranslator.InsertableFields(container);
            if (fields.Count == 0)
                throw new InvalidOperationException($"Container {container.Name} has no insertable fields");

            // COPY "schema"."table" ("col1", "col2") FROM STDIN BINARY
            var columnList = string.Join(", ", fields.Select(f => dialect.QuoteIdentifier(f.Name)));
            var copySql = $"COPY {BuildQualifiedTableName(dbPath)} ({columnList}) FROM STDIN BINARY";
            return GenericResult<PostgreSqlBulkCopyPlan>.Success(
                new PostgreSqlBulkCopyPlan(copySql, [.. fields.Select(f => f.Name)], [.. collection.Cast<object>()]));
        }
        catch (Exception ex)
        {
            return GenericResult<PostgreSqlBulkCopyPlan>.Failure(
                PostgreSqlDataResultCodes.ByName("BulkInsertTranslationFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message));
        }
    }

    // What both routes need first: the database path of the container and the rows the command carries, each refused with its
    // own coded failure when it is missing or not what a bulk insert takes.
    private static IGenericResult<BulkSource> Source(IDataCommand command, IStorageContainer? container)
    {
        if (container == null)
            return GenericResult<BulkSource>.Failure(PostgreSqlDataResultCodes.ByName("ContainerNull"));

        if (container.Path is not IDatabasePath dbPath)
            return GenericResult<BulkSource>.Failure(PostgreSqlDataResultCodes.ByName("InvalidContainerPath"));

        var dataObj = GetCommandData(command);
        if (dataObj == null)
        {
            return GenericResult<BulkSource>.Failure(
                PostgreSqlDataResultCodes.ByName("MissingInputData"),
                ResultDetails.Create("CommandType", "BulkInsertCommand"));
        }

        return dataObj is IEnumerable collection
            ? GenericResult<BulkSource>.Success(new BulkSource(dbPath, collection))
            : GenericResult<BulkSource>.Failure(
                PostgreSqlDataResultCodes.ByName("InvalidDataType"),
                ResultDetails.Create("TranslatorName", "PostgreSqlBulkInsertTranslator", "ActualType", dataObj.GetType().Name));
    }

    // The container's database path and the rows the command carries.
    private sealed record BulkSource(IDatabasePath Path, IEnumerable Collection);
}
