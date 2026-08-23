using System;
using System.Net.Http;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity.JwtAssertion;

/// <summary>
/// Builds <see cref="JwtAssertionIdentityService"/> instances from a resolved
/// <see cref="IdentityServiceConfiguration"/> header whose <c>Configuration</c> property carries the
/// composed <see cref="JwtAssertionConfiguration"/> typed body.
/// </summary>
/// <remarks>
/// Takes no secret manager: this mechanism has no secret to resolve, which is the whole point of it.
/// It also takes no identity provider, for the re-entrancy reason described on
/// <c>ClientCredentialsIdentityFactory</c>.
/// </remarks>
internal sealed class JwtAssertionIdentityFactory
    : IIdentityServiceFactory<IIdentityService, IdentityServiceConfiguration>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<JwtAssertionIdentityFactory> _logger;
    private readonly HttpClient _http;

    /// <summary>Initializes a new instance of the <see cref="JwtAssertionIdentityFactory"/> class.</summary>
    /// <param name="loggerFactory">The logger factory for created services.</param>
    /// <param name="http">The HTTP client used to reach the token endpoint.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="http"/> is null.</exception>
    public JwtAssertionIdentityFactory(ILoggerFactory? loggerFactory, HttpClient http)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<JwtAssertionIdentityFactory>();
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    /// <inheritdoc />
    public IGenericResult<IIdentityService> Create(IdentityServiceConfiguration configuration)
    {
        if (configuration is null)
            return GenericResult<IIdentityService>.Failure(IdentityLog.ConfigurationNotFound(_logger, "(null)"));

        // Why this fails rather than constructing with an empty body: runtime dispatch reads only the
        // typed body, so a header that arrived without one would produce a service whose every field
        // is null and whose first acquisition fails somewhere far from the cause.
        if (configuration.Configuration is not JwtAssertionConfiguration typed)
            return GenericResult<IIdentityService>.Failure(
                IdentityLog.TypedBodyMissing(_logger, configuration.Name, "JwtAssertion"));

        return GenericResult<IIdentityService>.Success(
            new JwtAssertionIdentityService(
                _loggerFactory.CreateLogger<JwtAssertionIdentityService>(),
                typed,
                new OAuth2TokenEndpointClient(_http, _loggerFactory.CreateLogger<OAuth2TokenEndpointClient>())));
    }

    /// <inheritdoc />
    public IGenericResult<IIdentityService> Create(IGenericConfiguration configuration)
        => configuration is IdentityServiceConfiguration header
            ? Create(header)
            : GenericResult<IIdentityService>.Failure(
                IdentityLog.TypedBodyMissing(_logger, configuration?.Name ?? "(null)", "JwtAssertion"));

    /// <inheritdoc />
    public IGenericResult<T> Create<T>(IGenericConfiguration configuration) where T : IGenericService
    {
        var result = Create(configuration);
        if (!result.IsSuccess)
            return result.ToNewResult<T>();

        return result.Value is T typed
            ? GenericResult<T>.Success(typed)
            : GenericResult<T>.Failure(
                IdentityLog.ResultTypeMismatch(_logger, typeof(T).Name, nameof(JwtAssertionIdentityService)));
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
