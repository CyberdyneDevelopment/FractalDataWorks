using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messaging.Abstractions;

/// <summary>
/// Resolves configured messaging services and routes each to the implementation provider that owns it.
/// </summary>
/// <remarks>
/// Named rather than a bare closed generic: a constructor asking for this states which rows it reads.
/// Two providers over different tables that share a shape are interchangeable at a call site when both
/// are spelled <c>IDomainConfigurationProvider&lt;T&gt;</c>, and nothing catches the swap.
/// </remarks>
public interface IMessagingConfigurationProvider
    : IDomainConfigurationProvider<IMessagingImplementationConfiguration>
{
    /// <summary>Reads a configured messaging service's domain row, without dispatching.</summary>
    /// <param name="name">The declared service name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// <c>MessageService</c> and <c>AccessRequestService</c> need only the store and the path on the
    /// domain row — where this deployment keeps its messages — not what a particular implementation
    /// kind needs to deliver one. Reading the header directly is what lets them resolve a location
    /// without a <c>ServiceOptionType</c> ever having been registered.
    /// </remarks>
    Task<IGenericResult<IMessagingConfiguration>> GetHeader(
        string name,
        CancellationToken cancellationToken = default);
}
