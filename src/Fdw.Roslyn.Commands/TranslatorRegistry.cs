using System;
using System.Collections.Concurrent;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands;

/// <summary>
/// Default implementation of <see cref="ITranslatorRegistry"/>.
/// </summary>
/// <remarks>
/// Registration is also where a translator gets its logger. Translators are not built by DI — the
/// cross-assembly TypeOption module initializer instantiates each with a bare <c>new()</c> — so a
/// constructor-injected ILogger would be null on every one of them. Doing it here rather than in a
/// host's wiring means it cannot be skipped: a translator that reaches the registry is executable, and
/// every executable translator passes through <see cref="Register(IRoslynCommandTranslator)"/>. An
/// earlier version decorated the static TypeOption catalogue instead, which happened to work only
/// because one host registered those same instances — anything constructed directly, registered via
/// <c>AddTranslator&lt;T&gt;</c>, or built in a test kept a NullLogger with no diagnostic.
/// </remarks>
public sealed class TranslatorRegistry : ITranslatorRegistry
{
    private readonly ConcurrentDictionary<Type, IRoslynCommandTranslator> _translators = new();
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TranslatorRegistry> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslatorRegistry"/> class.
    /// </summary>
    /// <param name="loggerFactory">The factory each registered translator's logger is created from.</param>
    /// <exception cref="ArgumentNullException">The factory is null.</exception>
    /// <remarks>
    /// Required, with no NullLoggerFactory default. A silent fallback here would reintroduce exactly the
    /// defect this class exists to close — a registry that looks wired and hands out silent translators.
    /// A caller that genuinely wants silence passes <c>NullLoggerFactory.Instance</c> and says so.
    /// </remarks>
    public TranslatorRegistry(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<TranslatorRegistry>();
    }

    /// <inheritdoc/>
    public IGenericResult<IRoslynCommandTranslator<TCommand, TResult>> GetTranslator<TCommand, TResult>()
        where TCommand : IRoslynCommand
        where TResult : IRoslynCommandResult
    {
        var commandType = typeof(TCommand);

        if (!_translators.TryGetValue(commandType, out var translator))
        {
            return GenericResult<IRoslynCommandTranslator<TCommand, TResult>>.Failure(
                RoslynResultCodes.ByName("TranslatorNotFound"),
                ResultDetails.Create().With("Message", $"No translator registered for command type '{commandType.Name}'"));
        }

        if (translator is not IRoslynCommandTranslator<TCommand, TResult> typedTranslator)
        {
            return GenericResult<IRoslynCommandTranslator<TCommand, TResult>>.Failure(
                RoslynResultCodes.ByName("TranslatorNotFound"),
                ResultDetails.Create().With("Message", $"Translator for '{commandType.Name}' does not support result type '{typeof(TResult).Name}'"));
        }

        return GenericResult<IRoslynCommandTranslator<TCommand, TResult>>.Success(typedTranslator);
    }

    /// <inheritdoc/>
    public IGenericResult<IRoslynCommandTranslator> GetTranslator(Type commandType)
    {
        if (commandType is null)
        {
            return GenericResult<IRoslynCommandTranslator>.Failure(
                RoslynResultCodes.ByName("CommandTypeCannotBeNull"));
        }

        if (!_translators.TryGetValue(commandType, out var translator))
        {
            return GenericResult<IRoslynCommandTranslator>.Failure(
                RoslynResultCodes.ByName("TranslatorNotFound"),
                ResultDetails.Create().With("Message", $"No translator registered for command type '{commandType.Name}'"));
        }

        return GenericResult<IRoslynCommandTranslator>.Success(translator);
    }

    /// <inheritdoc/>
    public void Register(IRoslynCommandTranslator translator)
    {
        if (translator is null)
        {
            throw new ArgumentNullException(nameof(translator));
        }

        Admit(translator, translator.CommandType);
    }

    /// <inheritdoc/>
    public void Register<TCommand, TResult>(IRoslynCommandTranslator<TCommand, TResult> translator)
        where TCommand : IRoslynCommand
        where TResult : IRoslynCommandResult
    {
        if (translator is null)
        {
            throw new ArgumentNullException(nameof(translator));
        }

        Admit(translator, typeof(TCommand));
    }

    /// <summary>
    /// Gives a translator its logger and files it under its command type.
    /// </summary>
    /// <param name="translator">The translator being registered.</param>
    /// <param name="commandType">The key to file it under.</param>
    private void Admit(IRoslynCommandTranslator translator, Type commandType)
    {
        if (translator is RoslynCommandTranslatorBase loggable)
        {
            loggable.UseLoggerFactory(_loggerFactory);
        }
        else
        {
            TranslatorRegistryLog.TranslatorCannotReceiveLogger(_logger, translator.GetType().Name);
        }

        if (_translators.TryGetValue(commandType, out var displaced)
            && !ReferenceEquals(displaced, translator))
        {
            TranslatorRegistryLog.TranslatorReplaced(
                _logger, commandType.Name, displaced.GetType().Name, translator.GetType().Name);
        }

        _translators[commandType] = translator;

        TranslatorRegistryLog.TranslatorRegistered(_logger, translator.GetType().Name, commandType.Name);
    }

    /// <summary>
    /// Gets the number of registered translators.
    /// </summary>
    public int Count => _translators.Count;

    /// <summary>
    /// Checks if a translator is registered for the specified command type.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <returns>True if a translator is registered; otherwise false.</returns>
    public bool HasTranslator<TCommand>() where TCommand : IRoslynCommand
    {
        return _translators.ContainsKey(typeof(TCommand));
    }

    /// <summary>
    /// Checks if a translator is registered for the specified command type.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <returns>True if a translator is registered; otherwise false.</returns>
    public bool HasTranslator(Type commandType)
    {
        return commandType is not null && _translators.ContainsKey(commandType);
    }
}
