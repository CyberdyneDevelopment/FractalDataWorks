using System;
using System.Net.Http;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Logging;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity.ClientCredentials;

/// <summary>
/// Builds <see cref="ClientCredentialsIdentityService"/> instances from a resolved
/// <see cref="IdentityServiceConfiguration"/> header whose <c>Configuration</c> property carries the
/// composed <see cref="ClientCredentialsConfiguration"/> typed body.
/// </summary>
/// <remarks>
/// This factory takes no identity provider. It is resolved from inside the scoped resolver lambda for
/// this domain's own provider, so depending on that provider here would re-enter a lambda whose cache
/// entry is not published yet — which does not throw, it hangs the host silently (FDW-615). The
/// secret-manager provider it does take belongs to a DIFFERENT domain, and is taken as a
/// <see cref="Lazy{T}"/> so that even that resolution happens after the container is built rather
/// than while this domain's own resolver lambda is still running.
/// </remarks>
internal sealed class ClientCredentialsIdentityFactory
    : IIdentityServiceFactory<IIdentityService, IdentityServiceConfiguration>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ClientCredentialsIdentityFactory> _logger;
    private readonly HttpClient _http;
    private readonly Lazy<IPlatformServiceProvider<ISecretManager, SecretManagerConfiguration>> _secretManagers;

    /// <summary>Initializes a new instance of the <see cref="ClientCredentialsIdentityFactory"/> class.</summary>
    /// <param name="loggerFactory">The logger factory for created services.</param>
    /// <param name="http">The HTTP client used to reach the authorization server's token endpoint.</param>
    /// <param name="secretManagers">Provider resolving the named secret manager holding the client secret.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="http"/> or <paramref name="secretManagers"/> is null.</exception>
    public ClientCredentialsIdentityFactory(
        ILoggerFactory? loggerFactory,
        HttpClient http,
        Lazy<IPlatformServiceProvider<ISecretManager, SecretManagerConfiguration>> secretManagers)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<ClientCredentialsIdentityFactory>();
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _secretManagers = secretManagers ?? throw new ArgumentNullException(nameof(secretManagers));
    }

    /// <inheritdoc />
    public IGenericResult<IIdentityService> Create(IdentityServiceConfiguration configuration)
    {
        if (configuration is null)
            return GenericResult<IIdentityService>.Failure(IdentityLog.ConfigurationNotFound(_logger, "(null)"));

        // Why this fails rather than constructing with an empty body: runtime dispatch reads only the
        // typed body, so a header that arrived without one would produce a service whose every field
        // is null and whose first acquisition fails somewhere far from the cause.
        if (configuration.Configuration is not ClientCredentialsConfiguration typed)
            return GenericResult<IIdentityService>.Failure(
                IdentityLog.TypedBodyMissing(_logger, configuration.Name, "ClientCredentials"));

        return GenericResult<IIdentityService>.Success(
            new ClientCredentialsIdentityService(
                _loggerFactory.CreateLogger<ClientCredentialsIdentityService>(),
                typed,
                new OAuth2TokenEndpointClient(_http, _loggerFactory.CreateLogger<OAuth2TokenEndpointClient>()),
                _secretManagers));
    }

    /// <inheritdoc />
    public IGenericResult<IIdentityService> Create(IGenericConfiguration configuration)
        => configuration is IdentityServiceConfiguration header
            ? Create(header)
            : GenericResult<IIdentityService>.Failure(
                IdentityLog.TypedBodyMissing(_logger, configuration?.Name ?? "(null)", "ClientCredentials"));

    /// <inheritdoc />
    public IGenericResult<T> Create<T>(IGenericConfiguration configuration) where T : IGenericService
    {
        var result = Create(configuration);
        if (!result.IsSuccess)
            return result.ToNewResult<T>();

        return result.Value is T typed
            ? GenericResult<T>.Success(typed)
            : GenericResult<T>.Failure(
                IdentityLog.ResultTypeMismatch(_logger, typeof(T).Name, nameof(ClientCredentialsIdentityService)));
    }

    /// <inheritdoc />
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        return result.IsSuccess
            ? GenericResult<IGenericService>.Success(result.Value!)
            : result.ToNewResult<IGenericService>();
    }
}
