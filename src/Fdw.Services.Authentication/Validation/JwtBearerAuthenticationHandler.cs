using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Authorization.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// Validates a bearer token a remote issuer signed, against the keys that issuer publishes.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IAuthenticationHandler"/> directly, for the same reason
/// <see cref="LocalKeyAuthenticationHandler"/> does: <c>AuthenticationHandler&lt;TOptions&gt;</c> reads
/// its configuration from <c>IOptionsMonitor&lt;TOptions&gt;</c>, a second configuration system
/// alongside this one, and nothing would check that a value had been copied across into it. This takes
/// its configuration provider the way every other service does.
/// </para>
/// <para>
/// The difference from LocalKey is where the key comes from. This host did not sign the token, so the
/// signing keys are the issuer's published ones, read from its OpenID configuration document and
/// cached by <see cref="ConfigurationManager{T}"/>, which refreshes them on its own schedule so a key
/// rotation at the issuer does not need a restart here.
/// </para>
/// </remarks>
internal sealed class JwtBearerAuthenticationHandler : IAuthenticationHandler
{
    private readonly IAuthenticationServiceConfigurationProvider _configuration;
    private readonly IRolePermissionResolver _permissionResolver;
    private readonly ILogger _log;

    // Keyed by issuer rather than by scheme: two services naming the same issuer read the same
    // document, and the manager holds the fetch and the refresh schedule for it.
    private static readonly Dictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> Documents
        = new(StringComparer.OrdinalIgnoreCase);

    private AuthenticationScheme? _scheme;
    private HttpContext? _context;

    // Read from the implementation row while building the validation parameters, used after the
    // token validates. A field rather than a return value because the roles are not part of what a
    // token is checked against; this handler is transient, one per resolution, like _scheme.
    private IReadOnlyList<string> _declaredRoles = [];

    /// <summary>Initializes a new instance of the <see cref="JwtBearerAuthenticationHandler"/> class.</summary>
    /// <param name="configuration">Reads the declared service and dispatches to its implementation.</param>
    /// <param name="permissionResolver">Expands the declared roles to the permissions they grant.</param>
    /// <param name="logger">The logger for validation outcomes.</param>
    public JwtBearerAuthenticationHandler(
        IAuthenticationServiceConfigurationProvider configuration,
        IRolePermissionResolver permissionResolver,
        ILogger<JwtBearerAuthenticationHandler>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _permissionResolver = permissionResolver ?? throw new ArgumentNullException(nameof(permissionResolver));
        _log = logger ?? NullLogger<JwtBearerAuthenticationHandler>.Instance;
    }

    /// <inheritdoc />
    public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
    {
        _scheme = scheme ?? throw new ArgumentNullException(nameof(scheme));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<AuthenticateResult> AuthenticateAsync()
    {
        if (_scheme is null || _context is null)
            return AuthenticateResult.NoResult();

        if (ReadBearerToken(_context) is not { Length: > 0 } token)
            return AuthenticateResult.NoResult();

        var parameters = await ResolveParameters(_context.RequestAborted).ConfigureAwait(false);
        if (!parameters.IsSuccess || parameters.Value is null)
            return AuthenticateResult.Fail(parameters.CurrentMessage ?? string.Empty);

        var validated = await new JsonWebTokenHandler()
            .ValidateTokenAsync(token, parameters.Value)
            .ConfigureAwait(false);

        if (!validated.IsValid)
            return AuthenticateResult.Fail(Text(AuthenticationValidationLog.TokenRejected(
                _log, ServiceName, validated.Exception?.Message ?? "no reason given")));

        // The token proves who called; it says nothing about what they may do here, because it
        // carries the issuer's claims and not this host's. The roles the entry declares are what
        // confer that, and they expand to permissions through the same resolver a signed-in user's
        // roles go through.
        var conferred = await ConferDeclaredRoles(
            validated.ClaimsIdentity, parameters.Value.ValidAudience, _context.RequestAborted).ConfigureAwait(false);
        if (!conferred.IsSuccess)
            return AuthenticateResult.Fail(conferred.CurrentMessage ?? string.Empty);

        AuthenticationValidationLog.TokenAccepted(_log, ServiceName, validated.ClaimsIdentity.Name ?? "(no name)");

        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(validated.ClaimsIdentity), _scheme.Name));
    }

    /// <summary>Builds what this scheme checks a token against, from the entry it was taken for.</summary>
    /// <param name="cancellationToken">A token to cancel the reads.</param>
    private async Task<IGenericResult<TokenValidationParameters>> ResolveParameters(CancellationToken cancellationToken)
    {
        var headers = await _configuration.GetHeaders(cancellationToken).ConfigureAwait(false);
        if (!headers.IsSuccess || headers.Value is null)
            return headers.ToNewResult<TokenValidationParameters>();

        // The issuer is on the domain row: every kind has one, and it is what routed the token here.
        if (headers.Value.FirstOrDefault(
                e => string.Equals(e.Name, ServiceName, StringComparison.OrdinalIgnoreCase)) is not { } header)
        {
            return GenericResult<TokenValidationParameters>.Failure(
                AuthenticationValidationLog.JwtBearerEntryUnreadable(_log, ServiceName));
        }

        // Through the same rule the binding was built with, so the string the selector matched and the
        // string checked here cannot differ - see IssuerName.
        var issuer = IssuerName.Read(header.Authority, ServiceName, _log);
        if (!issuer.IsSuccess || issuer.Value is null)
            return issuer.ToNewResult<TokenValidationParameters>();

        // The audience and roles are on the implementation row, which the provider dispatches to by
        // the kind the domain row names.
        var implementation = await _configuration.Get(header.Id, cancellationToken).ConfigureAwait(false);
        if (!implementation.IsSuccess || implementation.Value is not IJwtBearerAuthenticationConfiguration body)
        {
            return GenericResult<TokenValidationParameters>.Failure(
                AuthenticationValidationLog.JwtBearerEntryUnreadable(_log, ServiceName));
        }

        // Checked rather than assumed present. The column is nullable and the generated mapper turns
        // DBNull into string.Empty, so an unset audience arrives as "" and a null test would let it
        // through - and ValidateAudience with ValidAudience "" rejects every token for a reason the
        // log never names. Worse here than for LocalKey: without an audience the roles below would be
        // conferred on every token this issuer mints, for any client.
        if (string.IsNullOrWhiteSpace(body.Audience))
        {
            return GenericResult<TokenValidationParameters>.Failure(
                AuthenticationValidationLog.JwtBearerMissingAudience(_log, ServiceName));
        }

        if (string.IsNullOrWhiteSpace(body.Roles))
        {
            return GenericResult<TokenValidationParameters>.Failure(
                AuthenticationValidationLog.JwtBearerMissingRoles(_log, ServiceName));
        }

        // Split where it is read, so the delimited column is one column's shape rather than a
        // convention every consumer has to know.
        _declaredRoles = body.Roles
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var keys = await Keys(issuer.Value, cancellationToken).ConfigureAwait(false);
        if (!keys.IsSuccess || keys.Value is null)
            return keys.ToNewResult<TokenValidationParameters>();

        return GenericResult<TokenValidationParameters>.Success(new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer.Value,
            ValidateAudience = true,
            ValidAudience = body.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            // Every key the issuer currently publishes rather than one pinned key: it rotates on its
            // own schedule, and the manager refreshes the document without a restart here.
            IssuerSigningKeys = keys.Value,

            RoleClaimType = ClaimDefinitions.roles.Name,
            NameClaimType = ClaimDefinitions.sub.Name,

            // Pinned rather than read from the token's own header: an attacker who chooses the
            // algorithm can choose one this key trivially satisfies.
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        });
    }

    /// <summary>Adds the roles this entry declares, and the permissions they expand to.</summary>
    /// <param name="identity">The identity the validated token produced.</param>
    /// <param name="audience">The audience the token was checked against, named in failures.</param>
    /// <param name="cancellationToken">A token to cancel the resolution.</param>
    /// <remarks>
    /// A resolver failure refuses the request rather than admitting a caller with no permissions:
    /// authenticating someone into nothing means every route they reach denies them, which reads as
    /// a permissions problem rather than the resolver being down.
    /// </remarks>
    private async Task<IGenericResult> ConferDeclaredRoles(
        ClaimsIdentity identity, string? audience, CancellationToken cancellationToken)
    {
        var roles = _declaredRoles;
        var permissions = await _permissionResolver
            .Resolve(roles, cancellationToken).ConfigureAwait(false);

        if (permissions.IsFailure || permissions.Value is not { } granted)
        {
            return GenericResult.Failure(AuthenticationValidationLog.DeclaredRolesNotResolved(
                _log, ServiceName, string.Join(", ", roles),
                permissions.CurrentMessage ?? "the resolver reported success with no permission set"));
        }

        foreach (var role in roles)
            identity.AddClaim(new Claim(ClaimDefinitions.roles.Name, role));

        foreach (var permission in granted)
            identity.AddClaim(new Claim(ClaimDefinitions.perm.Name, permission));

        AuthenticationValidationLog.DeclaredRolesConferred(
            _log, ServiceName, roles.Count, granted.Count,
            identity.FindFirst(ClaimDefinitions.sub.Name)?.Value ?? "(no sub)");

        return GenericResult.Success();
    }

    /// <summary>Reads the signing keys the issuer currently publishes.</summary>
    /// <param name="issuer">The issuer, which is also where its OpenID configuration lives.</param>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <remarks>
    /// Awaited here rather than resolved inside <see cref="TokenValidationParameters"/>, whose key
    /// resolver is synchronous and would mean blocking on the fetch.
    /// </remarks>
    private async Task<IGenericResult<IEnumerable<SecurityKey>>> Keys(
        string issuer, CancellationToken cancellationToken)
    {
        ConfigurationManager<OpenIdConnectConfiguration> manager;
        lock (Documents)
        {
            if (!Documents.TryGetValue(issuer, out var existing))
            {
                existing = new ConfigurationManager<OpenIdConnectConfiguration>(
                    issuer.TrimEnd('/') + "/.well-known/openid-configuration",
                    new OpenIdConnectConfigurationRetriever());
                Documents[issuer] = existing;
            }

            manager = existing;
        }

        // The fetch reaches a host this process does not control, so a failure here is an ordinary
        // outcome to report rather than an exception to let escape into the authentication pipeline.
        try
        {
            var document = await manager.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
            return document.SigningKeys is { Count: > 0 } signing
                ? GenericResult<IEnumerable<SecurityKey>>.Success(signing)
                : GenericResult<IEnumerable<SecurityKey>>.Failure(
                    AuthenticationValidationLog.JwtBearerNoSigningKeys(_log, ServiceName, issuer));
        }
        catch (Exception ex)
        {
            return GenericResult<IEnumerable<SecurityKey>>.Failure(
                AuthenticationValidationLog.JwtBearerKeysUnreachable(_log, ServiceName, issuer, ex.Message));
        }
    }

    /// <summary>The declared entry this scheme was taken for.</summary>
    /// <remarks>One scheme is taken per declared service, so the scheme name says which one.</remarks>
    private string ServiceName
        => _scheme is not null
           && _scheme.Name.StartsWith(JwtBearerAuthenticationType.SchemePrefix, StringComparison.Ordinal)
            ? _scheme.Name[JwtBearerAuthenticationType.SchemePrefix.Length..]
            : _scheme?.Name ?? string.Empty;

    // The contract carries its text on Message, which is non-nullable; object.ToString() would give
    // whatever the implementing type happens to render and is not the contract.
    private static string Text(IGenericMessage message) => message.Message;

    /// <inheritdoc />
    /// <remarks>
    /// A bare 401 and no <c>WWW-Authenticate</c> header at all, matching LocalKey: the realm and error
    /// codes a JwtBearer challenge writes describe the token that was rejected, and this host does not
    /// tell an unauthenticated caller why it failed.
    /// </remarks>
    public Task ChallengeAsync(AuthenticationProperties? properties)
    {
        if (_context is not null)
            _context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ForbidAsync(AuthenticationProperties? properties)
    {
        if (_context is not null)
            _context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }

    private static string? ReadBearerToken(HttpContext context)
    {
        const string prefix = "Bearer ";
        var header = context.Request.Headers.Authorization.ToString();

        return header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? header[prefix.Length..].Trim()
            : null;
    }
}
