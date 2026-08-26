using System;
using System.Collections.Concurrent;
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
/// Wraps <see cref="PlatformServiceProviderBase{TService,TConfiguration,TFactory,TConfigurationProvider}"/>
/// and adds credential-service-specific cache-by-name lookup and the typed
/// <see cref="Get(CredentialServiceRequest, CancellationToken)"/> entry point.
/// </summary>
public sealed class CredentialServiceProvider
    : PlatformServiceProviderBase<ICredentialService, CredentialServiceConfiguration, ICredentialServiceFactory<ICredentialService, CredentialServiceConfiguration>, IServiceConfigurationProvider<CredentialServiceConfiguration>>,
      ICredentialServiceProvider
{
    private readonly ILogger<CredentialServiceProvider> _logger;

    // Why: credential service instances cache their resolved vault, so we cache the services
    // by name. ConcurrentDictionary<string, Lazy<...>> mirrors the pattern used by
    // DefaultDataVaultProvider — the Lazy ensures a single initialization per name even under
    // concurrent first-access. No stale-eviction: services are long-lived system objects;
    // configuration changes restart the host.
    private readonly ConcurrentDictionary<string, Lazy<Task<IGenericResult<ICredentialService>>>> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialServiceProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="services">The container this provider resolves factories from.</param>
    public CredentialServiceProvider(IServiceProvider services, ILogger<CredentialServiceProvider> logger)
        : base(services, logger ?? NullLogger<CredentialServiceProvider>.Instance)
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

    /// <inheritdoc />
    public override Task<IGenericResult<ICredentialService>> Get(string name, CancellationToken cancellationToken = default)
    {
        // Why: VSTHRD011 + VSTHRD002 fire on Lazy<Task<T>> value factories that block on async.
        // The Lazy here stores the Task itself (not the result), so .Value just returns the Task
        // without blocking — the caller awaits the Task. Suppression is safe.
        // LazyThreadSafetyMode.ExecutionAndPublication ensures only one Task is created per name.
#pragma warning disable VSTHRD011, VSTHRD002
        var lazy = _cache.GetOrAdd(name, static (key, provider) =>
            new Lazy<Task<IGenericResult<ICredentialService>>>(
                () => provider.GetFromBase(key, CancellationToken.None),
                LazyThreadSafetyMode.ExecutionAndPublication),
            this);
#pragma warning restore VSTHRD011, VSTHRD002

        return lazy.Value;
    }

    // Why: delegates to the base class Get(string) which resolves the configuration and
    // creates the credential service via the registered factory. Wrapped in a Lazy so the base
    // call only happens once per name regardless of concurrent callers.
    private Task<IGenericResult<ICredentialService>> GetFromBase(string name, CancellationToken ct)
        => base.Get(name, ct);
}
