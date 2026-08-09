using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.SecretManagers.Abstractions.Handlers;

/// <summary>
/// Non-generic base interface for secret manager command handlers.
/// Used by the TypeCollection for handler discovery and lookup.
/// </summary>
/// <remarks>
/// Deriving from <see cref="ITypeOption{TKey}"/> is what makes this a usable collection base, and the
/// key type has to be on the interface as well as the base: the collection exposes this type, so its
/// lookup dictionaries are keyed by the Id declared here. Non-generic ITypeOption declares Id as
/// object, which keys them by object and will not assign to the int-keyed fields the collection
/// generates from the base.
/// The generator reads Id, Name and Category from that contract and, given it, can build the
/// collection's NotFound sentinel itself; without it the collection has no sentinel and each backend
/// has to register a hand-written option to stand in for one — which puts a non-handler into the
/// handler set that every enumeration of All() then has to know to skip.
/// </remarks>
public interface ISecretManagerCommandHandler : ITypeOption<int, SecretManagerCommandHandlerBase>
{

    /// <summary>
    /// Gets the Type of command this handler processes.
    /// Used for command-type-based lookup.
    /// </summary>
    [TypeLookup("ByCommandType")]
    Type CommandTypeClass { get; }

    /// <summary>
    /// Gets the Type of result this handler returns.
    /// </summary>
    Type ResultType { get; }

    /// <summary>
    /// Validates the command before execution.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <returns>Validation result.</returns>
    IGenericResult Validate(ISecretManagerCommand command);

    /// <summary>
    /// Gets the execution delegate for this handler.
    /// The delegate signature is: Func&lt;TCommand, ISecretManagerExecutionContext, CancellationToken, Task&lt;IGenericResult&lt;TResult&gt;&gt;&gt;
    /// where TCommand and TResult are the handler's specific types.
    /// </summary>
    Delegate ExecuteFunc { get; }

    /// <summary>
    /// Executes the handler's command and returns the result boxed as <see cref="object"/>.
    /// </summary>
    /// <remarks>
    /// Reflection-free dispatch entry point: the typed handler base implements this against its
    /// statically-known TResult, so the caller does not <c>DynamicInvoke</c> <see cref="ExecuteFunc"/>
    /// and reflection-await the resulting <c>Task&lt;IGenericResult&lt;TResult&gt;&gt;</c>.
    /// </remarks>
    /// <param name="command">The command to execute.</param>
    /// <param name="context">The execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result with its value boxed as object.</returns>
    Task<IGenericResult<object?>> InvokeBoxed(
        ISecretManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken);
}

