using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Development.Abstractions;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Base class for Roslyn command translators.
/// Extends <see cref="DevelopmentCommandTranslatorBase"/> for Roslyn-specific translation.
/// </summary>
public abstract class RoslynCommandTranslatorBase : DevelopmentCommandTranslatorBase, IRoslynCommandTranslator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynCommandTranslatorBase"/> class.
    /// </summary>
    /// <param name="name">The name of the translator.</param>
    /// <param name="description">The description of the translator.</param>
    protected RoslynCommandTranslatorBase(string name, string description)
        : base(name, description)
    {
    }

    /// <summary>
    /// Executes the command against the solution.
    /// </summary>
    /// <summary>
    /// Gets the logger for this translator.
    /// </summary>
    /// <remarks>
    /// Set after construction rather than injected, because these translators are NOT built by DI. The
    /// cross-assembly TypeOption module initializer instantiates each one with a bare <c>new()</c> and
    /// hands the instance to the registry, so a constructor-injected ILogger would be null on every
    /// translator in the process — logging that looks wired and emits nothing. See
    /// <see cref="UseLoggerFactory"/>, which the host calls while hydrating the registry, at the point
    /// where DI is actually available.
    ///
    /// Defaults to NullLogger so a translator constructed directly — in a test, or before the host wires
    /// anything — stays functional and silent rather than throwing.
    /// </remarks>
    protected internal ILogger Logger { get; private set; } = NullLogger.Instance;

    /// <summary>
    /// Gives this translator a real logger.
    /// </summary>
    /// <param name="loggerFactory">The factory to create the logger from.</param>
    /// <remarks>
    /// Virtual, not hidden: the host applies this through a base-typed reference, so a <c>new</c> method
    /// on the typed layer would be skipped entirely and those translators would keep a NullLogger while
    /// appearing wired.
    /// </remarks>
    public virtual void UseLoggerFactory(ILoggerFactory loggerFactory)
    {
        if (loggerFactory is null) throw new ArgumentNullException(nameof(loggerFactory));

        Logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>
    /// Executes the command against the given solution.
    /// </summary>
    /// <param name="command">The command to run.</param>
    /// <param name="solution">The solution to run it against.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The command result.</returns>
    public abstract Task<IGenericResult<IRoslynCommandResult>> Execute(
        IRoslynCommand command,
        Solution solution,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Strongly-typed base class for Roslyn command translators.
/// </summary>
/// <typeparam name="TCommand">The type of command.</typeparam>
/// <typeparam name="TResult">The type of result.</typeparam>
public abstract class RoslynCommandTranslatorBase<TCommand, TResult> : RoslynCommandTranslatorBase, IRoslynCommandTranslator<TCommand, TResult>
    where TCommand : IRoslynCommand
    where TResult : IRoslynCommandResult
{
    /// <inheritdoc/>
    public override Type CommandType => typeof(TCommand);

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynCommandTranslatorBase{TCommand, TResult}"/> class.
    /// </summary>
    /// <param name="name">The name of the translator.</param>
    /// <param name="description">The description of the translator.</param>
    protected RoslynCommandTranslatorBase(string name, string description)
        : base(name, description)
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<IRoslynCommandResult>> Execute(
        IRoslynCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (command is not TCommand typedCommand)
        {
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynCommandLog.CommandTypeMismatch(
                    Logger,
                    typeof(TCommand).Name,
                    command.GetType().Name));
        }

        RoslynCommandLog.TranslatorExecuting(Logger, GetType().Name, typeof(TCommand).Name);

        var result = await Translate(typedCommand, solution, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            RoslynCommandLog.CommandExecutionFailed(Logger, result.CurrentMessage);
            return result.ToNewResult<IRoslynCommandResult>();
        }

        RoslynCommandLog.TranslatorProduced(
            Logger,
            GetType().Name,
            result.Value!.IsMutation ? "mutation" : "query",
            result.Value is Results.MutationResult produced ? produced.ChangedFiles.Count : 0,
            result.Value.Summary);

        return GenericResult<IRoslynCommandResult>.Success(result.Value!);
    }

    /// <summary>
    /// Translates and executes the command against the solution.
    /// </summary>
    public abstract Task<IGenericResult<TResult>> Translate(
        TCommand command,
        Solution solution,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A translator that receives a logger typed to itself.
/// </summary>
/// <typeparam name="TSelf">The concrete translator type.</typeparam>
/// <typeparam name="TCommand">The command this translator handles.</typeparam>
/// <typeparam name="TResult">The result type.</typeparam>
/// <remarks>
/// TSelf exists so <c>Logger</c> is <c>ILogger&lt;TheConcreteTranslator&gt;</c> rather than a bare
/// ILogger — the category then matches the type doing the work, which is what makes per-command log
/// filtering possible, and it matches the <c>NullLogger&lt;T&gt;.Instance</c> convention used everywhere
/// else in this codebase.
///
/// It is a separate layer rather than a change to the two-parameter base so translators can adopt it one
/// at a time; the untyped base keeps working unchanged.
/// </remarks>
public abstract class RoslynCommandTranslatorBase<TSelf, TCommand, TResult>
    : RoslynCommandTranslatorBase<TCommand, TResult>
    where TSelf : RoslynCommandTranslatorBase<TSelf, TCommand, TResult>
    where TCommand : IRoslynCommand
    where TResult : IRoslynCommandResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynCommandTranslatorBase{TSelf, TCommand, TResult}"/> class.
    /// </summary>
    /// <param name="name">The translator name.</param>
    /// <param name="description">The translator description.</param>
    protected RoslynCommandTranslatorBase(string name, string description)
        : base(name, description)
    {
    }

    /// <summary>
    /// Gets the logger, typed to the concrete translator.
    /// </summary>
    protected new ILogger<TSelf> Logger { get; private set; } = NullLogger<TSelf>.Instance;

    /// <summary>
    /// Gives this translator a logger typed to itself.
    /// </summary>
    /// <param name="loggerFactory">The factory to create the logger from.</param>
    /// <inheritdoc/>
    public override void UseLoggerFactory(ILoggerFactory loggerFactory)
    {
        if (loggerFactory is null) throw new ArgumentNullException(nameof(loggerFactory));

        Logger = loggerFactory.CreateLogger<TSelf>();
        base.UseLoggerFactory(loggerFactory);
    }
}
