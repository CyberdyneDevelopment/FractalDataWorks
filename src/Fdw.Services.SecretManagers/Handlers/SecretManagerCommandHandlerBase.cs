using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Fdw.Services.SecretManagers.Results;

namespace Fdw.Services.SecretManagers.Handlers;

/// <summary>
/// The type a handler collection is built on: one concrete base every handler shares, carrying the
/// Id, Name and Category the collection indexes by.
/// </summary>
/// <remarks>
/// <para>
/// A TypeCollection needs a single non-generic base to key on. Handlers are generic over their
/// command and result, so that generic form cannot be the collection's base — this is what stands in
/// its place, and what lets the generator build the collection's NotFound sentinel by deriving from
/// it. A collection keyed directly on the interface gets no sentinel, and each backend ends up
/// registering a hand-written stand-in as a member of its own handler set.
/// </para>
/// <para>
/// Each secret manager implementation defines its own TypeCollection (e.g.,
/// <c>AzureKeyVaultCommandHandlers</c>, <c>MsSqlCommandHandlers</c>) so that
/// handler discovery is assembly-local and requires no cross-assembly wiring.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Abstract base class")]
public abstract class SecretManagerCommandHandlerBase
    : TypeOptionBase<int, SecretManagerCommandHandlerBase>, ISecretManagerCommandHandler
{
    /// <summary>
    /// Initializes the handler with the identity the collection indexes it by.
    /// </summary>
    /// <param name="id">The unique identifier for this handler.</param>
    /// <param name="name">The command type name (e.g., "GetSecret").</param>
    protected SecretManagerCommandHandlerBase(int id, string name) : base(id, name)
    {
    }

    /// <inheritdoc />
    public abstract Type CommandTypeClass { get; }

    /// <inheritdoc />
    public abstract Type ResultType { get; }

    /// <inheritdoc />
    public abstract Delegate ExecuteFunc { get; }

    /// <inheritdoc />
    public abstract Task<IGenericResult<object?>> InvokeBoxed(
        ISecretManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract IGenericResult Validate(ISecretManagerCommand command);
}

/// <summary>
/// Strongly-typed handler base: knows its command and result types, so the manager never has to
/// DynamicInvoke the delegate or reflect over the returned task.
/// </summary>
/// <typeparam name="TCommand">The specific command type this handler processes.</typeparam>
/// <typeparam name="TResult">The result type returned by this handler.</typeparam>
/// <remarks>
/// Inherit from this class to create new command handlers. Each handler should be decorated with
/// <c>[TypeOption(typeof(YourImplCommandHandlers), "CommandTypeName")]</c> to register with the
/// per-implementation TypeCollection.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Abstract base class")]
public abstract class SecretManagerCommandHandlerBase<TCommand, TResult>
    : SecretManagerCommandHandlerBase
    where TCommand : ISecretManagerCommand<TResult>
{
    /// <summary>
    /// Initializes the base handler with identifier, name, and execution delegate.
    /// </summary>
    /// <param name="id">The unique identifier for this handler.</param>
    /// <param name="name">The command type name (e.g., "GetSecret").</param>
    protected SecretManagerCommandHandlerBase(int id, string name) : base(id, name)
    {
        ExecuteFunc = new Func<TCommand, ISecretManagerExecutionContext, CancellationToken, Task<IGenericResult<TResult>>>(Execute);
    }

    /// <inheritdoc />
    public override Type CommandTypeClass => typeof(TCommand);

    /// <inheritdoc />
    public override Type ResultType => typeof(TResult);

    /// <summary>
    /// Gets the command type name (alias for Name).
    /// </summary>
    public string CommandType => Name;

    /// <inheritdoc />
    public override Delegate ExecuteFunc { get; }

    /// <inheritdoc />
    // Why: TResult is statically known here, so the typed Execute is called directly and its value
    // boxed — replacing the manager's DynamicInvoke(ExecuteFunc) + reflection-await on Result/Value.
    public override async Task<IGenericResult<object?>> InvokeBoxed(
        ISecretManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
    {
        var result = await Execute((TCommand)command, context, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? GenericResult<object?>.Success(result.Value)
            : result.ToNewResult<object?>();
    }

    /// <summary>
    /// Executes the strongly-typed command. Override this in derived classes.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="context">The execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The strongly-typed result.</returns>
    protected abstract Task<IGenericResult<TResult>> Execute(
        TCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    public override IGenericResult Validate(ISecretManagerCommand command)
    {
        if (command is null)
        {
            return GenericResult.Failure(SecretManagerResultCodes.ByName("CommandNull"));
        }

        if (command is not TCommand)
        {
            return GenericResult.Failure(
                SecretManagerResultCodes.ByName("CommandTypeMismatch"),
                ResultDetails.Create()
                    .With("ExpectedType", typeof(TCommand).Name)
                    .With("ActualType", command.GetType().Name));
        }

        if (!string.Equals(command.CommandType, CommandType, StringComparison.Ordinal))
        {
            return GenericResult.Failure(
                SecretManagerResultCodes.ByName("CommandTypeNameMismatch"),
                ResultDetails.Create()
                    .With("ExpectedType", CommandType)
                    .With("ActualType", command.CommandType));
        }

        return ValidateTypedCommand((TCommand)command);
    }

    /// <summary>
    /// Validates the strongly-typed command. Override to add command-specific validation.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <returns>Validation result.</returns>
    protected virtual IGenericResult ValidateTypedCommand(TCommand command)
    {
        return GenericResult.Success();
    }
}
