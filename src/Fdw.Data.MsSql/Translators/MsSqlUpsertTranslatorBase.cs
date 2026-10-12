using System;
using System.Collections;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Upserts;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Logging;
using Fdw.Data.MsSql.Results;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// Shared translation of <see cref="IUpsertCommand"/> to one set-based T-SQL batch. The upsert and bulk upsert
/// translators differ only in the command name they answer to.
/// </summary>
public abstract class MsSqlUpsertTranslatorBase : MsSqlDataCommandTranslatorBase
{
    private readonly string _translatorName;

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlUpsertTranslatorBase"/> class.
    /// </summary>
    /// <param name="name">The command name this translator answers to (must match its <c>[TypeOption]</c>).</param>
    protected MsSqlUpsertTranslatorBase(string name)
        : base(name)
    {
        _translatorName = $"MsSql{name}Translator";
    }

    /// <summary>
    /// Translates an upsert command into a single batch: stage the rows, update or close the changed rows, insert the
    /// missing keys. Returns a failure result with a reason when the command cannot be translated.
    /// </summary>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<MsSqlUpsertTranslatorBase>.Instance;
        var containerName = container is null ? "<null>" : container.Name;
        MsSqlUpsertTranslatorLog.Translating(logger, _translatorName, containerName);

        try
        {
            return Task.FromResult(TranslateCommand(command, container, containerName));
        }
        catch (Exception ex)
        {
            MsSqlUpsertTranslatorLog.TranslationFailed(logger, ex, _translatorName, containerName, ex.Message);
            return Task.FromResult(GenericResult<SqlCommand>.Failure(
                MsSqlDataResultCodes.ByName("UpsertTranslationFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    private IGenericResult<SqlCommand> TranslateCommand(IDataCommand command, IStorageContainer? container, string containerName)
    {
        var logger = NullLogger<MsSqlUpsertTranslatorBase>.Instance;
        if (container is null)
        {
            MsSqlUpsertTranslatorLog.ContainerNull(logger, _translatorName);
            return GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("ContainerNull"));
        }

        if (container.Path is not IDatabasePath path)
        {
            MsSqlUpsertTranslatorLog.InvalidContainerPath(logger, _translatorName, containerName);
            return GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("InvalidContainerPath"));
        }

        if (command is not IUpsertCommand upsert)
        {
            return GenericResult<SqlCommand>.Failure(
                MsSqlTranslatorLog.InvalidCommandType(logger, _translatorName, nameof(IUpsertCommand), command.GetType().Name));
        }

        var rows = GetCommandData(command);
        if (rows is null)
        {
            MsSqlUpsertTranslatorLog.MissingInputData(logger, _translatorName, containerName);
            return Failed("MissingInputData", "CommandType", _translatorName);
        }

        return rows is IEnumerable collection
            ? Statement(upsert.Options, collection, container, path, containerName)
            : InvalidRows(rows, containerName);
    }

    private IGenericResult<SqlCommand> InvalidRows(object rows, string containerName)
    {
        var actual = rows.GetType().Name;
        MsSqlUpsertTranslatorLog.InvalidDataType(NullLogger<MsSqlUpsertTranslatorBase>.Instance, _translatorName, containerName, actual);
        return Failed("InvalidDataType", "ActualType", actual);
    }

    private IGenericResult<SqlCommand> Statement(
        UpsertOptions options,
        IEnumerable rows,
        IStorageContainer container,
        IDatabasePath path,
        string containerName)
    {
        var dialect = path.Dialect;
        var problem = MsSqlUpsertStatement.Problem(options, container, dialect);
        if (problem is not null)
            return Refused(containerName, problem);

        var json = MsSqlRowStaging.Serialize(rows, options.WriteColumns, out var rowsProblem);
        if (rowsProblem is not null)
            return Refused(containerName, rowsProblem);

        var command = CreateCommand(string.Empty);
        var sql = new MsSqlUpsertStatement(options, container, dialect, BuildQualifiedTableName(path)).Build(command);
        if (sql is null)
            return Refused(containerName, "a value source could not be rendered");

        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter(MsSqlRowStaging.RowsParameter, SqlDbType.NVarChar, -1) { Value = json });
        MsSqlUpsertTranslatorLog.Translated(
            NullLogger<MsSqlUpsertTranslatorBase>.Instance, _translatorName, containerName, options.KeyColumns.Count, options.Versioning is not null);
        return GenericResult<SqlCommand>.Success(command);
    }

    private IGenericResult<SqlCommand> Refused(string containerName, string reason)
    {
        MsSqlUpsertTranslatorLog.InvalidOptions(NullLogger<MsSqlUpsertTranslatorBase>.Instance, _translatorName, containerName, reason);
        return Failed("InvalidUpsertOptions", "Reason", reason);
    }

    private static IGenericResult<SqlCommand> Failed(string code, string detailName, string detail) =>
        GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName(code), ResultDetails.Create(detailName, detail));
}
