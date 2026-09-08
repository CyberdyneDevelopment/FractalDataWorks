using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.ServiceTypes;

namespace Fdw.Services.Credentials.Abstractions;

/// <summary>
/// Provider for configured credential service instances.
/// </summary>
public interface ICredentialServiceProvider : IPlatformServiceProvider<ICredentialService, ICredentialServiceImplementationConfiguration>
{
    /// <summary>
    /// Gets a credential service matching the supplied typed request (Id and/or Name).
    /// An empty request (neither Id nor Name) is a structured failure.
    /// </summary>
    /// <param name="request">The typed credential service lookup request.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<ICredentialService>> Get(CredentialServiceRequest request, CancellationToken cancellationToken = default);
}
