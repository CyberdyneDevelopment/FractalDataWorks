using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.TokenManagers.Abstractions;
using Fdw.Services.TokenManagers.Abstractions.Tokens;
using Fdw.Services.TokenManagers.Logging;
using Fdw.Services.Users;
using Fdw.Services.Users.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// The generic, provider-agnostic authN service. Holds the active <see cref="ITokenManager"/> (resolved
/// through <see cref="IPlatformServiceProvider{TService, TConfiguration}"/> by configured name) plus the
/// credential vault (<see cref="IUserCredentialService"/>) and the secret manager provider a concrete
/// token manager may need to resolve provider secrets. OpenIddict-free by design — this class knows
/// nothing about any specific token provider.
/// </summary>
public sealed class AuthenticationService : IAuthenticationService
{
    private readonly ITokenManagerProvider _tokenManagerProvider;
    private readonly TokenManagerConfigurationProvider _tokenManagerConfigurationProvider;
    private readonly IUserCredentialService _userCredentialService;
    private readonly UserConfigurationProvider _userConfigurationProvider;
    private readonly ISecretManagerProvider _secretManagerProvider;
    private readonly IAgentKeyService? _agentKeyService;
    private readonly ILogger<AuthenticationService> _logger;

    /// <summary>
    /// Registers <see cref="IAuthenticationService"/> with DI. Idempotent — safe to call from every
    /// TokenManagers option's registration cascade.
    /// </summary>
    public static void RegisterDomainServices(IServiceCollection services)
    {
        services.TryAddScoped<IAuthenticationService, AuthenticationService>();
    }

    /// <summary>Initializes a new instance of the <see cref="AuthenticationService"/> class.</summary>
    public AuthenticationService(
        ITokenManagerProvider tokenManagerProvider,
        TokenManagerConfigurationProvider tokenManagerConfigurationProvider,
        IUserCredentialService userCredentialService,
        UserConfigurationProvider userConfigurationProvider,
        ISecretManagerProvider secretManagerProvider,
        ILogger<AuthenticationService>? logger,
        IAgentKeyService? agentKeyService = null)
    {
        _tokenManagerProvider = tokenManagerProvider ?? throw new ArgumentNullException(nameof(tokenManagerProvider));
        _tokenManagerConfigurationProvider = tokenManagerConfigurationProvider ?? throw new ArgumentNullException(nameof(tokenManagerConfigurationProvider));
        _userCredentialService = userCredentialService ?? throw new ArgumentNullException(nameof(userCredentialService));
        _agentKeyService = agentKeyService;
        _userConfigurationProvider = userConfigurationProvider ?? throw new ArgumentNullException(nameof(userConfigurationProvider));
        _secretManagerProvider = secretManagerProvider ?? throw new ArgumentNullException(nameof(secretManagerProvider));
        _logger = logger ?? NullLogger<AuthenticationService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ClaimsPrincipal>> Authenticate(TokenIssuanceRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            return GenericResult<ClaimsPrincipal>.Failure(TokenManagerLog.RequestNull(_logger));

        TokenManagerLog.AuthenticatingGrant(_logger, request.GrantType);

        var tokenManagerResult = await ResolveActiveTokenManager(cancellationToken).ConfigureAwait(false);
        if (!tokenManagerResult.IsSuccess)
            return tokenManagerResult.ToNewResult<ClaimsPrincipal>();

        var issueResult = await tokenManagerResult.Value!.AuthenticateAndIssue(request, cancellationToken).ConfigureAwait(false); // Why: IsSuccess guarantees Value is non-null.
        if (!issueResult.IsSuccess)
        {
            TokenManagerLog.IssuanceFailed(_logger, request.GrantType);
            return issueResult;
        }

        TokenManagerLog.IssuanceSucceeded(_logger, request.GrantType);
        return issueResult;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ClaimsPrincipal>> Authenticate(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
            return GenericResult<ClaimsPrincipal>.Failure(TokenManagerLog.TokenMissing(_logger));

        TokenManagerLog.ValidatingBearerToken(_logger);

        var tokenManagerResult = await ResolveActiveTokenManager(cancellationToken).ConfigureAwait(false);
        if (!tokenManagerResult.IsSuccess)
            return tokenManagerResult.ToNewResult<ClaimsPrincipal>();

        var tokenManager = tokenManagerResult.Value!;

        var validateResult = await tokenManager.Validate(token, cancellationToken).ConfigureAwait(false);
        if (!validateResult.IsSuccess)
        {
            TokenManagerLog.ValidationFailed(_logger);
            return validateResult.ToNewResult<ClaimsPrincipal>();
        }

        var claimsResult = await tokenManager.ExtractClaims(token, cancellationToken).ConfigureAwait(false);
        if (!claimsResult.IsSuccess)
        {
            TokenManagerLog.ClaimsExtractionFailed(_logger);
            return claimsResult;
        }

        TokenManagerLog.ValidationSucceeded(_logger);
        return claimsResult;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Logout(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
            return GenericResult.Failure(TokenManagerLog.TokenMissing(_logger));

        TokenManagerLog.LogoutStarted(_logger);

        var tokenManagerResult = await ResolveActiveTokenManager(cancellationToken).ConfigureAwait(false);
        if (!tokenManagerResult.IsSuccess)
            return tokenManagerResult;

        var tokenManager = tokenManagerResult.Value!;

        var claimsResult = await tokenManager.ExtractClaims(token, cancellationToken).ConfigureAwait(false);
        if (!claimsResult.IsSuccess)
        {
            TokenManagerLog.ClaimsExtractionFailed(_logger);
            return claimsResult;
        }

        var subjectId = claimsResult.Value!.FindFirstValue(ClaimDefinitions.sub.Name);
        if (string.IsNullOrEmpty(subjectId))
            return GenericResult.Failure(TokenManagerLog.LogoutSubjectMissing(_logger));

        if (!Guid.TryParse(subjectId, out var principalId))
            return GenericResult.Failure(TokenManagerLog.LogoutSubjectMissing(_logger));

        var logoutResult = await tokenManager.Logout(principalId, cancellationToken).ConfigureAwait(false);
        if (!logoutResult.IsSuccess)
        {
            TokenManagerLog.LogoutFailed(_logger, subjectId);
            return logoutResult;
        }

        var invalidateResult = await tokenManager.Revoke(token, cancellationToken).ConfigureAwait(false);
        if (!invalidateResult.IsSuccess)
        {
            TokenManagerLog.LogoutFailed(_logger, subjectId);
            return invalidateResult;
        }

        TokenManagerLog.LogoutSucceeded(_logger, subjectId);
        return GenericResult.Success();
    }



    private async Task<IGenericResult<ITokenManager>> ResolveActiveTokenManager(CancellationToken cancellationToken)
    {
        var headersResult = await _tokenManagerConfigurationProvider.Get(cancellationToken).ConfigureAwait(false);
        if (!headersResult.IsSuccess)
            return headersResult.ToNewResult<ITokenManager>();

        var headers = headersResult.Value;
        if (headers is null || headers.Count == 0)
            return GenericResult<ITokenManager>.Failure(TokenManagerLog.NoActiveTokenManager(_logger));

        if (headers.Count > 1)
            return GenericResult<ITokenManager>.Failure(TokenManagerLog.MultipleActiveTokenManagers(_logger, headers.Count));

        return await _tokenManagerProvider.Get(headers[0].Name, cancellationToken).ConfigureAwait(false);
    }
}
