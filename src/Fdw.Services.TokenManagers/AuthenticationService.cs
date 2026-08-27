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
    // Why: optional — a host that issues no agent keys need not register the edge. The agent_key
    // grant fails loud when it is absent rather than silently falling through to another verifier.
    private readonly IAgentKeyService? _agentKeyService;
    private readonly ILogger<AuthenticationService> _logger;

    /// <summary>
    /// Registers <see cref="IAuthenticationService"/> with DI. Idempotent — safe to call from every
    /// TokenManagers option's registration cascade.
    /// </summary>
    // Why: Scoped, NOT Singleton. This ctor-injects two ServiceTypeCollection providers
    // (IPlatformServiceProvider<ITokenManager,...> and IPlatformServiceProvider<ISecretManager,...>), both
    // registered Scoped by the generator, plus IUserCredentialService. A Singleton capturing a Scoped
    // provider is a captive dependency (throws under ValidateScopes; pins one scope's providers for the
    // process lifetime). The only consumer, ConnectTokenEndpointBase, is a per-request endpoint, so
    // Scoped is the correct lifetime.
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
        // Why: resolves the username carried in TokenIssuanceRequest.Subject (password/agent_key
        // grants) to the durable user Id before calling IUserCredentialService.Verify, which is
        // keyed by Id, not username.
        _userConfigurationProvider = userConfigurationProvider ?? throw new ArgumentNullException(nameof(userConfigurationProvider));
        // Why: reserved for token managers whose Issue resolves a provider secret (e.g. a
        // client_credentials shared secret) through the same secret-manager axis every other
        // domain uses — kept here so every TokenManagerTypes option can depend on this service
        // alone rather than each re-injecting its own secret manager provider.
        _secretManagerProvider = secretManagerProvider ?? throw new ArgumentNullException(nameof(secretManagerProvider));
        _logger = logger ?? NullLogger<AuthenticationService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ClaimsPrincipal>> Authenticate(TokenIssuanceRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            return GenericResult<ClaimsPrincipal>.Failure(TokenManagerLog.RequestNull(_logger));

        TokenManagerLog.AuthenticatingGrant(_logger, request.GrantType);

        // Why no credential check here: the active token manager IS the credential seam — this service
        // is the provider-agnostic half and delegates every grant to it. Verifying first-party grants
        // here as well ran the derivation TWICE for one login: once at VerifyCredential and again inside
        // Issue, two 210,000-iteration PBKDF2 passes for the same password on the same request.
        //
        // The split is by AUTHORITY, not by layer. Either an external provider verified the caller
        // (external_identity — no password ever reaches us) or we verify it ourselves (password /
        // agent_key). One authority means one check. Removing the outer one also keeps the stricter
        // of the two: the token manager rejects an inactive user, this service never did.
        var tokenManagerResult = await ResolveActiveTokenManager(cancellationToken).ConfigureAwait(false);
        if (!tokenManagerResult.IsSuccess)
            return tokenManagerResult.ToNewResult<ClaimsPrincipal>();

        var issueResult = await tokenManagerResult.Value!.Issue(request, cancellationToken).ConfigureAwait(false); // Why: IsSuccess guarantees Value is non-null.
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

        // Why: IsSuccess guarantees Value is non-null; captured once since Value is read twice below.
        var tokenManager = tokenManagerResult.Value!;

        var validateResult = await tokenManager.Validate(token, cancellationToken).ConfigureAwait(false);
        if (!validateResult.IsSuccess)
        {
            TokenManagerLog.ValidationFailed(_logger);
            return validateResult;
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

        // Why: IsSuccess guarantees Value is non-null; captured once since it's used across three calls below.
        var tokenManager = tokenManagerResult.Value!;

        var claimsResult = await tokenManager.ExtractClaims(token, cancellationToken).ConfigureAwait(false);
        if (!claimsResult.IsSuccess)
        {
            TokenManagerLog.ClaimsExtractionFailed(_logger);
            return claimsResult;
        }

        // Why: use the exact claim OpenIdTokenManager itself reads as the subject (ClaimDefinitions.sub,
        // the standard JWT "sub") — the same claim BuildIdentityPrincipal bakes at issuance time.
        var subjectId = claimsResult.Value!.FindFirstValue(ClaimDefinitions.sub.Name);
        if (string.IsNullOrEmpty(subjectId))
            return GenericResult.Failure(TokenManagerLog.LogoutSubjectMissing(_logger));

        var logoutResult = await tokenManager.Logout(subjectId, cancellationToken).ConfigureAwait(false);
        if (!logoutResult.IsSuccess)
        {
            TokenManagerLog.LogoutFailed(_logger, subjectId);
            return logoutResult;
        }

        var invalidateResult = await tokenManager.Invalidate(token, cancellationToken).ConfigureAwait(false);
        if (!invalidateResult.IsSuccess)
        {
            TokenManagerLog.LogoutFailed(_logger, subjectId);
            return invalidateResult;
        }

        TokenManagerLog.LogoutSucceeded(_logger, subjectId);
        return GenericResult.Success();
    }



    // Why: resolves the active token manager BY CONFIGURED NAME rather than the list-all Get()
    // overload (the scaffold gap) — mirrors how the OpenIddict configurators resolve the secret
    // manager by a name carried on another already-loaded config row. TokenManagers is a "declared
    // choice" domain (Manual = true): exactly one enabled auth.TokenManager row is expected per
    // deployment. Read the config HEADERS directly (not built service instances) to find that one
    // row's Name, then resolve the SERVICE instance through the well-tested Get(name) path every
    // other domain uses.
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
