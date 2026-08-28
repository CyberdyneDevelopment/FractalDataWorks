using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fdw.Commands.Data.Abstractions;
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
    where TConfiguration : class, IGenericConfiguration
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

    /// <summary>
    /// Gets a value indicating whether this connection is stale and should be recreated.
    /// Derived classes override this to check their underlying connection resource.
    /// </summary>
    public virtual bool IsStale => false;

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
    /// Executes a data command against the unified container by translating to TCommand.
    /// ServiceBase already handled IGenericCommand → IDataCommand.
    /// Container is passed by DataGateway (not looked up internally).
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="command">The data command (already cast from IGenericCommand by ServiceBase).</param>
    /// <param name="container">The unified container (passed by DataGateway).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the typed execution outcome.</returns>
    public async Task<IGenericResult<T>> Execute<T>(IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default)
    {
        ConnectionLogger.TraceExecuteWithContainerEntry(Logger, command.CommandType);

        // Get translator for this command type via TypeCollection lookup
        var translator = GetTranslator(command.CommandType);

        if (string.Equals(translator.Name, "_Empty", StringComparison.Ordinal))
        {
            var message = ConnectionLogger.TranslatorNotFound(Logger, command.CommandType);
            return GenericResult<T>.Failure(message);
        }

        var translationResult = await translator.Translate(command, container, cancellationToken).ConfigureAwait(false);
        if (!translationResult.IsSuccess || translationResult.Value == null)
        {
            return translationResult.Messages.Any()
                ? translationResult.ToNewResult<T>()
                : GenericResult<T>.Failure(ConnectionLogger.TranslationFailed(Logger));
        }

        // Execute the native command type (SqlCommand, HttpRequestMessage, etc.)
        return await Execute<T>(translationResult.Value, container, cancellationToken).ConfigureAwait(false);
    }

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
        if (string.Equals(translator.Name, "_Empty", StringComparison.Ordinal))
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
    /// Executes a native command (SqlCommand, HttpRequestMessage, etc.).
    /// Derived classes implement this to handle their specific command type.
    /// </summary>
    /// <param name="command">The native command to execute.</param>
    /// <param name="container">The data container context for the operation.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the execution outcome.</returns>
    protected abstract Task<IGenericResult> Execute(TCommand command, IStorageContainer container, CancellationToken cancellationToken);
}
