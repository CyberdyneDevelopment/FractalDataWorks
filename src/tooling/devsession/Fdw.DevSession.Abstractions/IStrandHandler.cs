using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// A handler that carries out a routed strand of work within a session.
/// </summary>
/// <remarks>
/// This is an OPEN collection (<c>[MutableTypeCollection]</c>): the coordinator routes a strand to the
/// first handler whose <see cref="CanHandle"/> returns <see langword="true"/>, and consumers register
/// their own handlers from their own assemblies with a <c>[TypeOption(typeof(StrandHandlers), "...")]</c>
/// option. The platform owns the routing mechanism (<see cref="IWorkspaceCoordinator.Route"/>); the set
/// of handlers is deliberately open and ships empty — a strand handler is domain-specific work, supplied
/// by the consumer, not the framework.
/// </remarks>
public interface IStrandHandler : ITypeOption<int, StrandHandlerBase>
{
    /// <summary>
    /// Determines whether this handler can carry out the given strand.
    /// </summary>
    /// <param name="strand">The strand to consider.</param>
    /// <returns><see langword="true"/> if this handler should run the strand; otherwise <see langword="false"/>.</returns>
    bool CanHandle(StrandInfo strand);

    /// <summary>
    /// Carries out the strand's work within the session.
    /// </summary>
    /// <param name="session">The session the strand belongs to.</param>
    /// <param name="strand">The strand to carry out.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating whether the strand's work succeeded.</returns>
    Task<IGenericResult> Handle(IDevSession session, StrandInfo strand, CancellationToken cancellationToken = default);
}
