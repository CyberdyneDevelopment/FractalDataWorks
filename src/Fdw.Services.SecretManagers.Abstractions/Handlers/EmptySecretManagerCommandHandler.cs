using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;

namespace Fdw.Services.SecretManagers.Abstractions.Handlers;

/// <summary>
/// Empty/NotFound handler implementation.
/// Each per-implementation TypeCollection creates its own NotFound sentinel that
/// delegates to this class or follows the same pattern.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EmptySecretManagerCommandHandler : ISecretManagerCommandHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmptySecretManagerCommandHandler"/> class.
    /// </summary>
    public EmptySecretManagerCommandHandler()
    {
        ExecuteFunc = new Func<ISecretManagerCommand, ISecretManagerExecutionContext, CancellationToken, Task<IGenericResult<object?>>>(
            (cmd, ctx, ct) => Task.FromResult(
                GenericResult<object?>.Failure(new ErrorMessage($"No handler found for command type '{cmd?.CommandType ?? "null"}'"))));
    }

    /// <inheritdoc />
    public int Id => 0;

    /// <inheritdoc />
    public string Name => "NotFound";

    /// <inheritdoc />
    public Type CommandTypeClass => typeof(void);

    /// <inheritdoc />
    public Type ResultType => typeof(void);

    /// <inheritdoc />
    public Delegate ExecuteFunc { get; }

    /// <inheritdoc />
    public Task<IGenericResult<object?>> InvokeBoxed(
        ISecretManagerCommand command,
        ISecretManagerExecutionContext context,
        CancellationToken cancellationToken)
        => ((Func<ISecretManagerCommand, ISecretManagerExecutionContext, CancellationToken, Task<IGenericResult<object?>>>)ExecuteFunc)(command, context, cancellationToken);

    /// <inheritdoc />
    public IGenericResult Validate(ISecretManagerCommand command)
    {
        return GenericResult.Failure(new ErrorMessage("Cannot execute Empty handler"));
    }
}
