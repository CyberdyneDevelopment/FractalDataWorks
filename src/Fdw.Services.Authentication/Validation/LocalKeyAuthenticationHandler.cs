using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Fdw.Results;
using Fdw.Messages;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// Validates a bearer token this host issued, against the key it signed it with.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IAuthenticationHandler"/> directly rather than deriving from
/// <c>AuthenticationHandler&lt;TOptions&gt;</c>. That base class reads its configuration from
/// <c>IOptionsMonitor&lt;TOptions&gt;</c>, which is a second configuration system alongside this one:
/// a value would have to be copied out of a provider into an options object by an adapter registered
/// under the exact service type <c>OptionsFactory</c> enumerates, with nothing checking that it was.
/// The interface asks for none of that, so this takes its configuration provider the way every other
/// service does.
/// </para>
/// <para>
/// Token validation itself is a library call — <see cref="JsonWebTokenHandler"/> against a
/// <see cref="TokenValidationParameters"/> built here — not a framework pattern, so nothing is lost
/// by leaving the options system out.
/// </para>
/// </remarks>
internal sealed class LocalKeyAuthenticationHandler : IAuthenticationHandler
{
    private readonly IAuthenticationServiceConfigurationProvider _configuration;
    private readonly ISigningCredentialProvider _credentials;
    private readonly ILogger _log;

    private AuthenticationScheme? _scheme;
    private HttpContext? _context;

    /// <summary>Initializes a new instance of the <see cref="LocalKeyAuthenticationHandler"/> class.</summary>
    /// <param name="configuration">Reads the declared service and dispatches to its implementation.</param>
    /// <param name="credentials">Holds the key this host signs with, which is the key it checks against.</param>
    /// <param name="logger">The logger for validation outcomes.</param>
    public LocalKeyAuthenticationHandler(
        IAuthenticationServiceConfigurationProvider configuration,
        ISigningCredentialProvider credentials,
        ILogger<LocalKeyAuthenticationHandler>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        _log = logger ?? NullLogger<LocalKeyAuthenticationHandler>.Instance;
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

        // The inbound claim map is not applied: JsonWebTokenHandler does not rewrite claim types the
        // way it would, so sub stays sub and roles stays roles.
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
                AuthenticationValidationLog.LocalKeyEntryUnreadable(_log, ServiceName));
        }

        // Through the same rule the binding was built with, so the string the selector matched and the
        // string checked here cannot differ - see IssuerName.
        var issuer = IssuerName.Read(header.Authority, ServiceName, _log);
        if (!issuer.IsSuccess || issuer.Value is null)
            return issuer.ToNewResult<TokenValidationParameters>();

        // The audience is on the implementation row, which the provider dispatches to by the kind the
        // domain row names.
        var implementation = await _configuration.Get(header.Id, cancellationToken).ConfigureAwait(false);
        if (!implementation.IsSuccess || implementation.Value is not ILocalKeyAuthenticationConfiguration body)
        {
            return GenericResult<TokenValidationParameters>.Failure(
                AuthenticationValidationLog.LocalKeyEntryUnreadable(_log, ServiceName));
        }

        var credentials = await _credentials.Current(cancellationToken).ConfigureAwait(false);
        if (!credentials.IsSuccess || credentials.Value is not { Key: { } key })
        {
            return GenericResult<TokenValidationParameters>.Failure(
                AuthenticationValidationLog.LocalSigningKeyUnavailable(_log, ServiceName));
        }

        return GenericResult<TokenValidationParameters>.Success(new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer.Value,
            ValidateAudience = true,
            ValidAudience = body.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            RoleClaimType = ClaimDefinitions.roles.Name,
            NameClaimType = ClaimDefinitions.sub.Name,

            // Pinned rather than read from the token's own header: an attacker who chooses the
            // algorithm can choose one this key trivially satisfies.
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        });
    }

    /// <summary>The declared entry this scheme was taken for.</summary>
    /// <remarks>One scheme is taken per declared service, so the scheme name says which one.</remarks>
    private string ServiceName
        => _scheme is not null
           && _scheme.Name.StartsWith(LocalKeyAuthenticationType.SchemePrefix, StringComparison.Ordinal)
            ? _scheme.Name[LocalKeyAuthenticationType.SchemePrefix.Length..]
            : _scheme?.Name ?? string.Empty;

    // AuthenticateResult.Fail wants a string and a message renders to one; the coalesce is the
    // permitted string.Empty, not a substituted value.
    private static string Text(IGenericMessage message) => message.ToString() ?? string.Empty;

    /// <inheritdoc />
    /// <remarks>
    /// A bare 401 with the scheme named. No <c>WWW-Authenticate</c> parameters beyond the scheme: the
    /// realm and error codes a JwtBearer challenge adds describe the token that was rejected, and this
    /// host does not tell an unauthenticated caller why.
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
