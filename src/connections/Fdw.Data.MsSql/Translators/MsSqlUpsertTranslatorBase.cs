using System;
using System.Collections;
using System.Linq;
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
public abstract class MsSqlUpsertTranslatorBase : MsSqlDataCommandTranslatorBase, IMsSqlStagingTranslator
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
        var batch = Batch(options, rows, container, path, containerName);
        if (batch.IsFailure)
            return batch.ToNewResult<SqlCommand>();

        var command = batch.Value!.Inline(out var rowsProblem);
        if (command is null)
            return Refused(containerName, rowsProblem ?? string.Empty);

        MsSqlUpsertTranslatorLog.Translated(
            NullLogger<MsSqlUpsertTranslatorBase>.Instance, _translatorName, containerName, options.KeyColumns.Count, options.Versioning is not null);
        return GenericResult<SqlCommand>.Success(command);
    }

    /// <inheritdoc/>
    /// <remarks>The same write as the inline translation, with the rows bulk copied into the temporary table instead of sent as JSON (FDW-4958).</remarks>
    public IGenericResult<MsSqlStagedWrite> Stage(IDataCommand command, IStorageContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        try
        {
            if (container.Path is not IDatabasePath path)
                return GenericResult<MsSqlStagedWrite>.Failure(MsSqlDataResultCodes.ByName("InvalidContainerPath"));
            if (command is not IUpsertCommand upsert)
                return GenericResult<MsSqlStagedWrite>.Failure(
                    MsSqlTranslatorLog.InvalidCommandType(NullLogger<MsSqlUpsertTranslatorBase>.Instance, _translatorName, nameof(IUpsertCommand), command.GetType().Name));
            if (GetCommandData(command) is not IEnumerable rows)
                return GenericResult<MsSqlStagedWrite>.Failure(MsSqlDataResultCodes.ByName("MissingInputData"), ResultDetails.Create("CommandType", _translatorName));

            var batch = Batch(upsert.Options, rows, container, path, container.Name);
            return batch.IsFailure ? batch.ToNewResult<MsSqlStagedWrite>() : batch.Value!.AsStaged(container);
        }
        catch (Exception ex)
        {
            MsSqlUpsertTranslatorLog.TranslationFailed(NullLogger<MsSqlUpsertTranslatorBase>.Instance, ex, _translatorName, container.Name, ex.Message);
            return GenericResult<MsSqlStagedWrite>.Failure(MsSqlDataResultCodes.ByName("UpsertTranslationFailed"), ResultDetails.Create("ErrorMessage", ex.Message));
        }
    }

    private IGenericResult<MsSqlSetBasedBatch> Batch(
        UpsertOptions options,
        IEnumerable rows,
        IStorageContainer container,
        IDatabasePath path,
        string containerName)
    {
        var dialect = path.Dialect;
        var problem = MsSqlUpsertStatement.Problem(options, container, dialect);
        if (problem is not null)
            return Refused(containerName, problem.Message).ToNewResult<MsSqlSetBasedBatch>();

        // The rows are read once; a value a date column cannot take is refused here, with MSSQL-21030, not left for the database to fail converting the staged rows (FDW-4726).
        var all = rows.Cast<object>().ToList();
        var refused = MsSqlDateValues.Refusal(container, options.WriteColumns, all);
        if (refused is not null)
            return refused.ToNewResult<MsSqlSetBasedBatch>();

        var batch = new MsSqlUpsertStatement(options, container, dialect, BuildQualifiedTableName(path)).Batch(CreateCommand(string.Empty), all);
        return batch is null
            ? Refused(containerName, "a value source could not be rendered").ToNewResult<MsSqlSetBasedBatch>()
            : GenericResult<MsSqlSetBasedBatch>.Success(batch);
    }

    private IGenericResult<SqlCommand> Refused(string containerName, string reason)
    {
        MsSqlUpsertTranslatorLog.InvalidOptions(NullLogger<MsSqlUpsertTranslatorBase>.Instance, _translatorName, containerName, reason);
        return Failed("InvalidUpsertOptions", "Reason", reason);
    }

    private static IGenericResult<SqlCommand> Failed(string code, string detailName, string detail) =>
        GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName(code), ResultDetails.Create(detailName, detail));
}
