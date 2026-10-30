using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Abstractions.Timeouts;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Configuration;
using Fdw.Services.Connections.Abstractions.Logging;


namespace Fdw.Services.Connections;

/// <summary>
/// Abstract base class for all data connection service implementations.
/// Handles IDataCommand → TCommand translation and execution.
/// </summary>
/// <typeparam name="TCommand">The native command type returned by the translator (e.g., SqlCommand, HttpRequestMessage).</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the connection service.</typeparam>
/// <typeparam name="TService">The concrete service type for logging and identification purposes.</typeparam>
/// <remarks>
/// <para>
/// Pattern matching hierarchy:
/// 1. ServiceBase: IGenericCommand → IDataCommand (TCommand = IDataCommand)
/// 2. ConnectionBase: IDataCommand → TCommand via translator (this class)
/// 3. Derived class: Implements Execute(TCommand) methods
/// </para>
/// <para>
/// For connections that support IDataCommand:
/// - Translator converts IDataCommand → TCommand (SqlCommand for MsSql, HttpRequestMessage for REST, etc.)
/// - Derived classes execute the native command type
/// </para>
/// </remarks>
public abstract class ConnectionBase<TCommand, TConfiguration, TService>
    : ServiceBase<IDataCommand, TConfiguration, TService>, IDataConnection
    where TConfiguration : class, IImplementationConfiguration
    where TService : class
{
    private IConnectionState _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionBase{TCommand,TConfiguration,TService}"/> class.
    /// </summary>
    /// <param name="logger">The logger for this connection service.</param>
    /// <param name="configuration">The configuration for this connection service.</param>
    protected ConnectionBase(
        ILogger<TService>? logger,
        TConfiguration configuration)
        : base(logger, configuration)
    {
        _state = ConnectionStates.Created;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// False here, so a connection whose translator has no grouping never silently returns ungrouped
    /// rows for a grouped query. A connection whose translator builds a GROUP BY overrides it.
    /// </remarks>
    public virtual bool AggregatesInStore => false;

    /// <summary>
    /// Tests connectivity to the underlying resource.
    /// Derived classes override this to perform actual connectivity testing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating whether the connection test succeeded.</returns>
    public virtual Task<IGenericResult> TestConnection(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }

    /// <summary>
    /// Gets the translator for a given command type using TypeCollection lookup.
    /// Derived classes implement this to return the appropriate translator from their TypeCollection.
    /// </summary>
    /// <param name="commandType">The command type (e.g., "Query", "Insert").</param>
    /// <returns>The translator for the command type.</returns>
    protected abstract IDataCommandTranslator<TCommand> GetTranslator(string commandType);

    /// <summary>
    /// Executes a data command without container metadata.
    /// Connections require container metadata for translation and materialization.
    /// Use the container-aware overload via DataGateway instead.
    /// </summary>
    /// <param name="command">The data command to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A failure result indicating that a container is required.</returns>
    public override Task<IGenericResult> Execute(IDataCommand command, CancellationToken cancellationToken = default)
    {
        ConnectionLogger.TraceExecuteNoContainerEntry(Logger);
        return Task.FromResult<IGenericResult>(
            GenericResult.Failure(ConnectionLogger.ExecutionFailed(Logger)));
    }

    /// <summary>
    /// Executes a data command without container metadata.
    /// Connections require container metadata for translation and materialization.
    /// Use the container-aware overload via DataGateway instead.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="command">The data command to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A failure result indicating that a container is required.</returns>
    public override Task<IGenericResult<T>> Execute<T>(IDataCommand command, CancellationToken cancellationToken = default)
    {
        ConnectionLogger.TraceExecuteNoContainerEntry(Logger);
        return Task.FromResult(
            GenericResult<T>.Failure(ConnectionLogger.ExecutionFailed(Logger)));
    }

    /// <summary>
    /// Gives the native command the timeout its data command carries (FDW-3324).
    /// </summary>
    /// <param name="native">The native command the translator made.</param>
    /// <param name="timeout">The timeout the data command carries, longer than zero.</param>
    /// <param name="owner">Who set the limit, in the words a failure uses; empty when the command does not say.</param>
    /// <returns>A success once the native command carries the timeout; a failure for a kind of connection that cannot apply one.</returns>
    /// <remarks>
    /// Why the base refuses: a kind that does not override this cannot honour a command timeout, and running the command without it would
    /// ignore a limit someone set, so the command is refused and the failure says so. SQL Server, PostgreSQL and SQLite override it.
    /// </remarks>
    protected virtual IGenericResult ApplyCommandTimeout(TCommand native, TimeSpan timeout, string owner)
        => GenericResult.Failure(ConnectionLogger.CommandTimeoutNotApplied(Logger, owner));

    /// <summary>
    /// Executes a data command against the unified container by translating to TCommand.
    /// ServiceBase already handled IGenericCommand → IDataCommand.
    /// Container is passed by DataGateway (not looked up internally).
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="command">The data command (already cast from IGenericCommand by ServiceBase).</param>
    /// <param name="container">The unified container (passed by DataGateway).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the typed execution outcome.</returns>
    public virtual async Task<IGenericResult<T>> Execute<T>(IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default)
    {
        ConnectionLogger.TraceExecuteWithContainerEntry(Logger, command.CommandType);

        // Get translator for this command type via TypeCollection lookup
        var translator = GetTranslator(command.CommandType);

        if (string.Equals(translator.Name, "NotFound", StringComparison.Ordinal))
            return await ExecuteWithoutTranslator<T>(command, container, cancellationToken).ConfigureAwait(false);

        var translationResult = await translator.Translate(command, container, cancellationToken).ConfigureAwait(false);
        if (!translationResult.IsSuccess || translationResult.Value == null)
        {
            return translationResult.Messages.Any()
                ? translationResult.ToNewResult<T>()
                : GenericResult<T>.Failure(ConnectionLogger.TranslationFailed(Logger));
        }

        // A command that carries a timeout of its own (FDW-3324) is given it before it runs, or refused by a kind that cannot apply one: a
        // limit someone set is never ignored.
        if (CommandTimeoutPolicy.TryGet(command, out var commandTimeout, out var commandTimeoutOwner))
        {
            var timed = ApplyCommandTimeout(translationResult.Value, commandTimeout, commandTimeoutOwner);
            if (timed.IsFailure)
                return timed.ToNewResult<T>();
        }

        // A command that names columns to return answers the rows it wrote, and a result of rows asks for the rows a command
        // reads: the native command is run for rows, not for a count.
        if (command is IReturningCommand { ReturnColumns.Count: > 0 } || typeof(T) == typeof(global::System.Collections.Generic.IReadOnlyList<global::System.Collections.Generic.IReadOnlyDictionary<string, object?>>))
            return await ExecuteReturning<T>(command, translationResult.Value, container, cancellationToken).ConfigureAwait(false);

        // Execute the native command type (SqlCommand, HttpRequestMessage, etc.)
        return await Execute<T>(translationResult.Value, container, cancellationToken).ConfigureAwait(false);
    }

    // The columns come back as one dictionary for each row written, through the kind's own row reader (ExecuteRowsByType).
    private async Task<IGenericResult<T>> ExecuteReturning<T>(IDataCommand command, TCommand native, IStorageContainer container, CancellationToken cancellationToken)
    {
        if (typeof(T) != typeof(global::System.Collections.Generic.IReadOnlyList<global::System.Collections.Generic.IReadOnlyDictionary<string, object?>>))
            return GenericResult<T>.Failure(ConnectionLogger.ReturnedColumnsResultNotRows(Logger, command.CommandType, typeof(T).Name));

        var rows = await ExecuteRowsByType(native, container, typeof(global::System.Collections.Generic.Dictionary<string, object?>), cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure)
            return rows.ToNewResult<T>();

        return GenericResult<T>.Success((T)(object)rows.Value!
            .Cast<global::System.Collections.Generic.IReadOnlyDictionary<string, object?>>()
            .ToList());
    }

    // A kind with no translator for a command runs the simpler commands it expands into (and, for a command that assembles
    // its own result, answers that result); a command that cannot expand fails by name.
    private async Task<IGenericResult<T>> ExecuteWithoutTranslator<T>(IDataCommand command, IDataContainer container, CancellationToken cancellationToken)
    {
        if (command is IAssembledExpandableDataCommand<T> assembled)
            return await ExecuteAssembledExpansion(assembled, container, cancellationToken).ConfigureAwait(false);

        return command is IExpandableDataCommand expandable
            ? await ExecuteExpansion<T>(expandable, container, cancellationToken).ConfigureAwait(false)
            : GenericResult<T>.Failure(ConnectionLogger.TranslatorNotFound(Logger, command.CommandType));
    }

    // A command that assembles its own result runs its steps in one transaction when this connection can open one, so
    // either every step is done or none is; a connection that cannot, or that already runs inside a unit of work whose
    // transaction the steps belong to, runs them in order.
    private async Task<IGenericResult<T>> ExecuteAssembledExpansion<T>(IAssembledExpandableDataCommand<T> command, IDataContainer container, CancellationToken cancellationToken)
    {
        if (InUnitOfWork || this is not ITransactionalDataConnection transactional)
        {
            ConnectionLogger.TraceExpansionTransaction(Logger, command.CommandType, false);
            return await CommandExpansion.RunAssembled(
                command,
                container,
                (step, token) => Execute<int>(step, container, token),
                (step, token) => Execute<global::System.Collections.Generic.IReadOnlyList<global::System.Collections.Generic.IReadOnlyDictionary<string, object?>>>(step, container, token),
                Logger,
                cancellationToken).ConfigureAwait(false);
        }

        var begun = await BeginAssembledUnit(transactional, cancellationToken).ConfigureAwait(false);
        if (begun.IsFailure)
            return begun.ToNewResult<T>();

        ConnectionLogger.TraceExpansionTransaction(Logger, command.CommandType, true);
        var scope = begun.Value!;
        await using (scope.ConfigureAwait(false))
        {
            var result = await CommandExpansion.RunAssembled(
                command,
                container,
                (step, token) => scope.Execute<int>(step, container, token),
                (step, token) => scope.Execute<global::System.Collections.Generic.IReadOnlyList<global::System.Collections.Generic.IReadOnlyDictionary<string, object?>>>(step, container, token),
                Logger,
                cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
            {
                await scope.Rollback(cancellationToken).ConfigureAwait(false);
                return result;
            }

            var committed = await scope.Commit(cancellationToken).ConfigureAwait(false);
            return committed.IsFailure ? committed.ToNewResult<T>() : result;
        }
    }

    // A kind with no translator for a command that declares an expansion runs the simpler commands it expands into,
    // each through the full lookup (so one may expand again). CommandExpansion holds the loop; a unit of work uses it too.
    private Task<IGenericResult<T>> ExecuteExpansion<T>(IExpandableDataCommand command, IDataContainer container, CancellationToken cancellationToken)
        => CommandExpansion.Run<T>(
            command,
            container,
            (step, token) => Execute<int>(step, container, token),
            Logger,
            Configuration is IDatabaseConnectionConfiguration database ? database.MaxSerialInsert : null,
            cancellationToken);

    /// <summary>
    /// Executes a data command against the unified container.
    /// ServiceBase already handled IGenericCommand → IDataCommand.
    /// </summary>
    /// <param name="command">The data command (already cast from IGenericCommand by ServiceBase).</param>
    /// <param name="container">The unified container (passed by DataGateway).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the execution outcome.</returns>
    public async Task<IGenericResult> Execute(IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default)
    {
        ConnectionLogger.TraceExecuteEntry(Logger, command.CommandType);

        var result = await Execute<object>(command, container, cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            return GenericResult.Success();
        }

        return result.Messages.Any()
            ? result
            : GenericResult.Failure(ConnectionLogger.ExecutionFailed(Logger));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<global::System.Collections.Generic.IEnumerable<object>>> Execute(
        IDataCommand command, IDataContainer container, global::System.Type elementType, CancellationToken cancellationToken = default)
    {
        ConnectionLogger.TraceExecuteWithContainerEntry(Logger, command.CommandType);

        var translator = GetTranslator(command.CommandType);
        if (string.Equals(translator.Name, "NotFound", StringComparison.Ordinal))
            return GenericResult<global::System.Collections.Generic.IEnumerable<object>>.Failure(
                ConnectionLogger.TranslatorNotFound(Logger, command.CommandType));

        var translationResult = await translator.Translate(command, container, cancellationToken).ConfigureAwait(false);
        if (!translationResult.IsSuccess || translationResult.Value == null)
        {
            return translationResult.Messages.Any()
                ? translationResult.ToNewResult<global::System.Collections.Generic.IEnumerable<object>>()
                : GenericResult<global::System.Collections.Generic.IEnumerable<object>>.Failure(ConnectionLogger.TranslationFailed(Logger));
        }

        return await ExecuteRowsByType(translationResult.Value, container, elementType, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a native command (SqlCommand, HttpRequestMessage, etc.) with container metadata.
    /// Derived classes implement this to handle their specific command type.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="command">The native command to execute.</param>
    /// <param name="container">The container with schema metadata for result materialization.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the typed execution outcome.</returns>
    protected abstract Task<IGenericResult<T>> Execute<T>(TCommand command, IStorageContainer container, CancellationToken cancellationToken);

    /// <summary>
    /// Executes a native command and materializes each result row as an object of
    /// <paramref name="elementType"/> using the element's generated mapper (no runtime reflection —
    /// the mapper is resolved by type name). The base implementation fails loud; transports that
    /// support row results (e.g. SQL) override it.
    /// </summary>
    /// <param name="command">The native command to execute.</param>
    /// <param name="container">The container with schema metadata for row materialization.</param>
    /// <param name="elementType">The element type whose generated mapper materializes each row.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the materialized rows as objects.</returns>
    protected virtual Task<IGenericResult<global::System.Collections.Generic.IEnumerable<object>>> ExecuteRowsByType(
        TCommand command, IStorageContainer container, global::System.Type elementType, CancellationToken cancellationToken)
        => Task.FromResult(GenericResult<global::System.Collections.Generic.IEnumerable<object>>.Failure(ConnectionLogger.ExecutionFailed(Logger)));

    /// <summary>
    /// Opens the unit of work an assembled command runs its steps in. A connection whose units cannot overlap overrides it to
    /// wait its turn; the default opens one at once.
    /// </summary>
    /// <param name="transactional">This connection, as the one that opens units of work.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The unit; or why it could not be opened.</returns>
    protected virtual Task<IGenericResult<IDataConnectionTransaction>> BeginAssembledUnit(
        ITransactionalDataConnection transactional, CancellationToken cancellationToken)
        => transactional.BeginTransaction(cancellationToken);

    /// <summary>
    /// Gets a value indicating whether this connection already runs inside a unit of work, so every command it runs
    /// belongs to that unit's transaction. A command that assembles its own result then runs its steps on this
    /// connection as they are, and does not open a transaction of its own that would commit apart from the unit's.
    /// A connection that is created for a unit of work overrides it; the default is <see langword="false"/>.
    /// </summary>
    protected virtual bool InUnitOfWork => false;

    /// <summary>
    /// Executes a native command (SqlCommand, HttpRequestMessage, etc.).
    /// Derived classes implement this to handle their specific command type.
    /// </summary>
    /// <param name="command">The native command to execute.</param>
    /// <param name="container">The data container context for the operation.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the execution outcome.</returns>
    protected abstract Task<IGenericResult> Execute(TCommand command, IStorageContainer container, CancellationToken cancellationToken);
}
