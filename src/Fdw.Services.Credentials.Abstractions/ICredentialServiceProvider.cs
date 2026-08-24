using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.ServiceTypes;

namespace Fdw.Services.Credentials.Abstractions;

/// <summary>
/// Provider for configured credential service instances.
/// </summary>
// Why: credential service providers provide SERVICES, not configurations — configuration comes
// from the credential domain's configuration provider. Consumers resolve a credential service by
// name (the connections→secret-managers pattern), then execute vault commands through it.
public interface ICredentialServiceProvider : IPlatformServiceProvider<ICredentialService>
{
    /// <summary>
    /// Gets a credential service matching the supplied typed request (Id and/or Name).
    /// An empty request (neither Id nor Name) is a structured failure.
    /// </summary>
    /// <param name="request">The typed credential service lookup request.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<ICredentialService>> Get(CredentialServiceRequest request, CancellationToken cancellationToken = default);
}
