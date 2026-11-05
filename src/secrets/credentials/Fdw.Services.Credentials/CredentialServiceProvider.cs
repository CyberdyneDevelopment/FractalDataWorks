using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Credentials.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Credentials;

/// <summary>
/// Default implementation of <see cref="ICredentialServiceProvider"/>.
/// Wraps <see cref="DomainServiceProviderBase{TService,TConfiguration,TFactory,TConfigurationProvider}"/>
/// and adds the typed <see cref="Get(CredentialServiceRequest, CancellationToken)"/> entry point.
/// </summary>
/// <remarks>
/// Every lookup, by name, by id or all, goes through the base, which holds no cache of its own: the
/// configuration read is cached by the configuration gateway, and a built service lives as long as its
/// implementation provider's registration says. A per-name cache here once kept failures for the life
/// of the scope and ignored the caller's token (FDW-3609).
/// </remarks>
public sealed class CredentialServiceProvider
    : DomainServiceProviderBase<
          ICredentialService,
          ICredentialServiceImplementationConfiguration,
          ICredentialServiceFactory<ICredentialService, ICredentialServiceImplementationConfiguration>,
          ICredentialServiceConfigurationProvider>,
      ICredentialServiceProvider
{
    private readonly ILogger<CredentialServiceProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialServiceProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public CredentialServiceProvider(ILogger<CredentialServiceProvider> logger)
        : base(logger ?? NullLogger<CredentialServiceProvider>.Instance)
    {
        _logger = logger ?? NullLogger<CredentialServiceProvider>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult<ICredentialService>> Get(CredentialServiceRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || (request.Id is null && string.IsNullOrWhiteSpace(request.Name)))
            return Task.FromResult<IGenericResult<ICredentialService>>(
                GenericResult<ICredentialService>.Failure(CredentialServiceLog.EmptyCredentialServiceRequest(_logger)));

        if (request.Id.HasValue)
            return Get(request.Id.Value, cancellationToken);

        return Get(request.Name!, cancellationToken);
    }
}
