using Fdw.Data.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.FileSystem.Abstractions;
using Fdw.Services.Connections.FileSystem.Abstractions.Logging;
using Fdw.Services.Connections.Logging;
using Fdw.Services.Connections.RowQuery;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.FileSystem;

/// <summary>
/// FileSystem connection implementation.
/// Wraps a <see cref="FileSystemClient"/> and enforces Root path isolation.
/// </summary>
/// <remarks>
/// Inherits <c>ConnectionBase&lt;IFileSystemCommand, ...&gt;</c> so the DataGateway-routed
/// command path remains wirable in 1.2.0 if the canary experiment succeeds.
/// No concrete <c>IFileSystemCommand</c> types ship in 1.1.1 — connectors call
/// <see cref="Client"/> directly per the §1.1 canary experiment.
/// </remarks>
public sealed class FileSystemConnection
    : ConnectionBase<IFileSystemCommand, FileSystemConnectionConfiguration, FileSystemConnection>,
      IFileSystemConnection, ISupportsHealthProbe
{
    private readonly FileSystemClient _client;
    private readonly FileSystemCommandTranslator _translator;
    private readonly FileSystemRecordConnector _recordConnector;
    private readonly FileSystemConfigurationWriter _configurationWriter;

    /// <inheritdoc />
    public string Root { get; }

    /// <inheritdoc />
    public IFileSystemClient Client => _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemConnection"/> class.
    /// </summary>
    /// <param name="configuration">The validated FileSystem connection configuration.</param>
    /// <param name="logger">Logger instance; falls back to NullLogger if null.</param>
    public FileSystemConnection(
        FileSystemConnectionConfiguration configuration,
        ILogger<FileSystemConnection>? logger)
        : base(logger, configuration)
    {
        // Why: Root is canonicalized once at construction. All relative paths from the client
        // are validated against this canonicalized root before any I/O occurs.
        Root = Path.GetFullPath(configuration.Root);
        _client = new FileSystemClient(Root, Name, logger);
        // Why: the translator computes path + operation from the command/container (no I/O); the record
        // connector runs the config-driven record source/writer over the file client. Both share the
        // connection name + logger so structured logging traces back to this connection.
        _translator = new FileSystemCommandTranslator(Name, logger);
        _recordConnector = new FileSystemRecordConnector(_client, Name, logger);
        // Why: the config write verbs (ConfigurationSave/Update/ConfigurationDelete) read the container's
        // current rows through the record connector, mutate them per version-on-write semantics, and rewrite
        // the whole file — this writer owns that read-modify-write, keeping Execute a thin dispatcher.
        _configurationWriter = new FileSystemConfigurationWriter(_recordConnector, Name, Root, logger);
        // Why: ConnectionBase already stored the NullLogger-fallbacked logger in Logger; use it so the
        // message actually emits (a message built on a throwaway NullLogger never logs).
        FileSystemConnectionLog.Created(Logger, Name, Root);
    }

    /// <inheritdoc />
    protected override IDataCommandTranslator<IFileSystemCommand> GetTranslator(string commandType)
    {
        // Why: the FileSystem domain has one translator that handles every command type (it maps the
        // command to a read/write file operation), mirroring the HTTP connection's single protocol
        // translator adapter. ConnectionBase only treats the "_Empty" sentinel as "not found", so this
        // real translator drives the unified Execute(IDataCommand, IDataContainer) path.
        return _translator;
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<T>> Execute<T>(
        IFileSystemCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken)
    {
        // Why: the native command is always a read/write FileSystemRecordCommand built by the translator;
        // dispatch by the command type (no direction flag), and fail loud on any other IFileSystemCommand
        // (a wiring defect — NO FALLBACKS).
        return command switch
        {
            FileSystemWriteCommand write => await ExecuteWrite<T>(write, cancellationToken).ConfigureAwait(false),
            FileSystemReadCommand read => await ExecuteRead<T>(read, cancellationToken).ConfigureAwait(false),
            FileSystemConfigurationSaveCommand save =>
                WrapCount<T>(await _configurationWriter.Save(save, cancellationToken).ConfigureAwait(false)),
            FileSystemUpdateCommand update =>
                WrapCount<T>(await _configurationWriter.Update(update, cancellationToken).ConfigureAwait(false)),
            FileSystemConfigurationDeleteCommand delete =>
                WrapCount<T>(await _configurationWriter.Delete(delete, cancellationToken).ConfigureAwait(false)),
            _ => GenericResult<T>.Failure(
                FileSystemConnectionLog.FactoryValidationFailed(
                    Logger,
                    Name,
                    $"unsupported native command '{command.GetType().Name}'"))
        };
    }

    // Why: the config write verbs return an affected-row count (int). Materialize it into T when T can hold
    // it (int/long/bool/object); for any other T — e.g. the provider's Execute<TConfig> header save, which
    // checks IsSuccess and returns its own record — the write produced no typed value, so return
    // Success(default). The write already succeeded; this NEVER fails on materialization. Mirrors
    // MsSqlConnection.ConvertScalarResult, so the version-on-write save behaves identically across transports.
    private static IGenericResult<T> WrapCount<T>(IGenericResult<int> writeResult)
    {
        if (!writeResult.IsSuccess)
            return writeResult.ToNewResult<T>();

        var count = writeResult.Value;
        var targetType = typeof(T);

        if (targetType == typeof(int))
            return GenericResult<T>.Success((T)(object)count);
        if (targetType == typeof(long))
            return GenericResult<T>.Success((T)(object)(long)count);
        if (targetType == typeof(bool))
            return GenericResult<T>.Success((T)(object)(count > 0));
        if (targetType == typeof(object))
            return GenericResult<T>.Success((T)(object)count);

        return GenericResult<T>.Success(default!);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult> Execute(
        IFileSystemCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken)
    {
        var result = await Execute<object>(command, container, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? GenericResult.Success() : result;
    }

    private async Task<IGenericResult<T>> ExecuteRead<T>(
        FileSystemReadCommand command,
        CancellationToken cancellationToken)
    {
        var readResult = await _recordConnector
            .Read(command.Container, command.RelativePath, cancellationToken)
            .ConfigureAwait(false);
        if (!readResult.IsSuccess)
        {
            return readResult.ToNewResult<T>();
        }

        // Why: the translator drops the QueryCommand's Filter/Joins into the native command unchanged;
        // apply them here (over the connector's decoded, unfiltered rows) via the shared,
        // format/transport-agnostic evaluator so a whole-file read never masquerades as a filtered one.
        var matchResult = await RecordQueryEvaluator.Evaluate(
            readResult.Value!,
            command.Container,
            command.Filter,
            command.Joins,
            (joinedContainerName, ct) => LoadJoinedRows(command.Container, joinedContainerName, ct),
            Logger,
            cancellationToken).ConfigureAwait(false);
        if (!matchResult.IsSuccess)
        {
            return matchResult.ToNewResult<T>();
        }

        // Why: the no-DTO read yields rows as flat name→value maps. Materialize into the requested T when
        // it is the rows collection itself; otherwise materialize through the shared PocoMapper-based
        // path (F6 Stage 1 typed-T materialization).
        if (matchResult.Value is T typed)
        {
            return GenericResult<T>.Success(typed);
        }

        if (typeof(T) == typeof(IEnumerable<IReadOnlyDictionary<string, object?>>))
        {
            return GenericResult<T>.Success((T)(object)matchResult.Value!);
        }

        return RecordRowMaterializer.Materialize<T>(matchResult.Value!, command.Container, Logger);
    }

    // Why: resolves the JOIN target container from the primary container's tree-navigation parent
    // (the same schema/path — ConfigurationCommandBase.GetByParentJoin always joins within one path)
    // and reads its rows through the same record connector used for the primary container. Kept on the
    // connection (not the shared evaluator) because resolving a sibling container and reading it are
    // transport-specific — the evaluator only knows how to filter/join already-decoded rows. Returns
    // the resolved container alongside its rows (JoinedRowsResult) so RecordQueryEvaluator can validate
    // the join/filter columns and the loaded rows against the container's DECLARED field schema.
    private async Task<IGenericResult<JoinedRowsResult>> LoadJoinedRows(
        IDataContainer primaryContainer, string joinedContainerName, CancellationToken cancellationToken)
    {
        var joinedContainerResult = primaryContainer.Parent.Container(joinedContainerName);
        if (!joinedContainerResult.IsSuccess || joinedContainerResult.Value is null)
        {
            return GenericResult<JoinedRowsResult>.Failure(
                RecordQueryLog.JoinedContainerNotFound(Logger, joinedContainerName, primaryContainer.Parent.Name));
        }

        var joinedContainer = joinedContainerResult.Value;
        var readResult = await _recordConnector
            .Read(joinedContainer, joinedContainer.Path.PathValue, cancellationToken)
            .ConfigureAwait(false);
        if (!readResult.IsSuccess)
        {
            return readResult.ToNewResult<JoinedRowsResult>();
        }

        return GenericResult<JoinedRowsResult>.Success(new JoinedRowsResult(joinedContainer, readResult.Value!));
    }

    private async Task<IGenericResult<T>> ExecuteWrite<T>(
        FileSystemWriteCommand command,
        CancellationToken cancellationToken)
    {
        var writeResult = await _recordConnector
            .Write(command.Container, command.RelativePath, command.Rows, cancellationToken)
            .ConfigureAwait(false);
        if (!writeResult.IsSuccess)
        {
            return writeResult.ToNewResult<T>();
        }

        // Why: the write returns the affected record count (int), the same shape an Insert command's
        // result carries; convert to T when T is int/object, otherwise fail loud.
        if (writeResult.Value is T typed)
        {
            return GenericResult<T>.Success(typed);
        }

        if (typeof(T) == typeof(object))
        {
            return GenericResult<T>.Success((T)(object)writeResult.Value);
        }

        return GenericResult<T>.Failure(
            FileSystemConnectionLog.FactoryValidationFailed(
                Logger,
                Name,
                $"write result of type 'int' cannot be materialized as '{typeof(T).Name}'"));
    }

    /// <inheritdoc />
    public override Task<IGenericResult> TestConnection(CancellationToken cancellationToken = default)
    {
        return Directory.Exists(Root)
            ? Task.FromResult<IGenericResult>(GenericResult.Success())
            : Task.FromResult<IGenericResult>(
                GenericResult.Failure(
                    FileSystemConnectionLog.RootDoesNotExist(
                        Logger,
                        Name, Root)));
    }

    /// <summary>
    /// Performs a cheap liveness probe by verifying that the configured root path exists.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating whether the configured root exists.</returns>
    public Task<IGenericResult> Probe(CancellationToken cancellationToken = default)
    {
        FileSystemConnectionLog.ProbeStarting(Logger, Name, Root);

        if (!Directory.Exists(Root))
        {
            return Task.FromResult<IGenericResult>(
                GenericResult.Failure(
                    FileSystemConnectionLog.RootDoesNotExist(Logger, Name, Root)));
        }

        FileSystemConnectionLog.ProbeSucceeded(Logger, Name, Root);
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
