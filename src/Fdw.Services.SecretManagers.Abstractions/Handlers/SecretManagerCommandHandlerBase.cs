using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Results;

namespace Fdw.Services.SecretManagers.Abstractions.Handlers;

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
// Why no Justification argument: this package targets netstandard2.0, where the attribute has no
// such property — it arrived in .NET 5.
[ExcludeFromCodeCoverage]
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

