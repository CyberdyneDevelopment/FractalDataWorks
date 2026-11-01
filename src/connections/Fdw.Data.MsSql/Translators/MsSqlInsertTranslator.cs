using System;
using System.Collections;
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
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Connections.Sql;

namespace Fdw.Data.MsSql;

/// <summary>
/// Translates an <c>InsertCommand</c>, one row or many, into batched multi-row INSERT statements for SQL Server.
/// </summary>
/// <remarks>
/// The statements are what a connection runs serially, up to its configured <c>MaxSerialInsert</c> rows. Above that the connection
/// takes its bulk copy, which <see cref="Plan"/> prepares from the same rows and the same columns.
/// </remarks>
[TypeOption(typeof(MsSqlDataCommandTranslators), "Insert", RestrictToCurrentCompilation = true)]
public sealed partial class MsSqlInsertTranslator : MsSqlDataCommandTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="MsSqlInsertTranslator"/> class.</summary>
    public MsSqlInsertTranslator()
        : base("Insert")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        MsSqlInsertTranslatorLog.Translating(
            NullLogger<MsSqlInsertTranslator>.Instance, container?.Name ?? "<null>");

        try
        {
            var source = Source(command, container);
            if (source.IsFailure)
                return Task.FromResult(source.ToNewResult<SqlCommand>());

            var (dbPath, rows) = source.Value!;
            return Task.FromResult(GenericResult<SqlCommand>.Success(
                BuildBatchedInsertStatements(container!, dbPath, rows, ReturnedColumnsOf(command, container!))));
        }
        catch (Exception ex)
        {
            MsSqlInsertTranslatorLog.InsertTranslationFailed(
                NullLogger<MsSqlInsertTranslator>.Instance, ex, container?.Name ?? "<null>", ex.Message);
            return Task.FromResult(
                GenericResult<SqlCommand>.Failure(
                    MsSqlDataResultCodes.ByName("InsertTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    private static IGenericResult<InsertSource> Source(IDataCommand command, IStorageContainer? container)
    {
        if (container == null)
        {
            MsSqlInsertTranslatorLog.ContainerNull(NullLogger<MsSqlInsertTranslator>.Instance);
            return GenericResult<InsertSource>.Failure(MsSqlDataResultCodes.ByName("ContainerNull"));
        }

        if (container.Path is not IDatabasePath dbPath)
        {
            MsSqlInsertTranslatorLog.InvalidContainerPath(
                NullLogger<MsSqlInsertTranslator>.Instance, container.Name);
            return GenericResult<InsertSource>.Failure(MsSqlDataResultCodes.ByName("InvalidContainerPath"));
        }

        var dataObj = GetCommandData(command);
        if (dataObj == null)
        {
            MsSqlInsertTranslatorLog.MissingInputData(
                NullLogger<MsSqlInsertTranslator>.Instance, "InsertCommand", container.Name);
            return GenericResult<InsertSource>.Failure(
                MsSqlDataResultCodes.ByName("MissingInputData"),
                ResultDetails.Create("CommandType", "InsertCommand"));
        }

        if (dataObj is not IEnumerable rows)
        {
            MsSqlInsertTranslatorLog.InvalidDataType(
                NullLogger<MsSqlInsertTranslator>.Instance, container.Name, dataObj.GetType().Name);
            return GenericResult<InsertSource>.Failure(
                MsSqlDataResultCodes.ByName("InvalidDataType"),
                ResultDetails.Create("TranslatorName", "MsSqlInsertTranslator", "ActualType", dataObj.GetType().Name));
        }

        return GenericResult<InsertSource>.Success(new InsertSource(dbPath, rows));
    }

    private sealed record InsertSource(IDatabasePath Path, IEnumerable Rows);
}
