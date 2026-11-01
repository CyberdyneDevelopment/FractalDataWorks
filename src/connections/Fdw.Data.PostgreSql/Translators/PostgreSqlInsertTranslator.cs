using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.PostgreSql.Results;
using Fdw.Data.PostgreSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Npgsql;
using Fdw.Services.Connections.Sql;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Translates an <c>InsertCommand</c>, one row or many, into batched multi-row INSERT statements for PostgreSQL.
/// </summary>
/// <remarks>
/// The statements are what a connection runs serially, up to its configured <c>MaxSerialInsert</c> rows. Above that the connection
/// streams the rows through a binary COPY, which <see cref="Plan"/> prepares from the same rows and the same columns.
/// </remarks>
[TypeOption(typeof(PostgreSqlDataCommandTranslators), "Insert", RestrictToCurrentCompilation = true)]
public sealed partial class PostgreSqlInsertTranslator : PostgreSqlDataCommandTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="PostgreSqlInsertTranslator"/> class.</summary>
    public PostgreSqlInsertTranslator()
        : base("Insert")
    {
    }

    /// <inheritdoc/>
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

            var (dbPath, rows) = source.Value!;
            return Task.FromResult(GenericResult<NpgsqlCommand>.Success(
                BuildBatchedInsertStatements(container, dbPath, rows, ReturnedColumnsOf(command, container))));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                GenericResult<NpgsqlCommand>.Failure(
                    PostgreSqlDataResultCodes.ByName("InsertTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    private static IGenericResult<InsertSource> Source(IDataCommand command, IStorageContainer? container)
    {
        if (container == null)
            return GenericResult<InsertSource>.Failure(PostgreSqlDataResultCodes.ByName("ContainerNull"));

        if (container.Path is not IDatabasePath dbPath)
            return GenericResult<InsertSource>.Failure(PostgreSqlDataResultCodes.ByName("InvalidContainerPath"));

        var dataObj = GetCommandData(command);
        if (dataObj == null)
        {
            return GenericResult<InsertSource>.Failure(
                PostgreSqlDataResultCodes.ByName("MissingInputData"),
                ResultDetails.Create("CommandType", "InsertCommand"));
        }

        return dataObj is IEnumerable rows
            ? GenericResult<InsertSource>.Success(new InsertSource(dbPath, rows))
            : GenericResult<InsertSource>.Failure(
                PostgreSqlDataResultCodes.ByName("InvalidDataType"),
                ResultDetails.Create("TranslatorName", "PostgreSqlInsertTranslator", "ActualType", dataObj.GetType().Name));
    }

    private sealed record InsertSource(IDatabasePath Path, IEnumerable Rows);
}
