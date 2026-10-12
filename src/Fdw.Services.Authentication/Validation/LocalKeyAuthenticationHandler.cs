using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Services.Abstractions;
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
    private readonly ITokenRevocationStore _revocations;
    private readonly ILogger _log;

    private AuthenticationScheme? _scheme;
    private HttpContext? _context;

    /// <summary>Initializes a new instance of the <see cref="LocalKeyAuthenticationHandler"/> class.</summary>
    /// <param name="configuration">Reads the declared service and dispatches to its implementation.</param>
    /// <param name="credentials">Holds the key this host signs with, which is the key it checks against.</param>
    /// <param name="revocations">Checked after signature validation — this host's own deny-list.</param>
    /// <param name="logger">The logger for validation outcomes.</param>
    public LocalKeyAuthenticationHandler(
        IAuthenticationServiceConfigurationProvider configuration,
        ISigningCredentialProvider credentials,
        ITokenRevocationStore revocations,
        ILogger<LocalKeyAuthenticationHandler>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        _revocations = revocations ?? throw new ArgumentNullException(nameof(revocations));
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

        if (BearerTokenReader.Read(_context) is not { Length: > 0 } token)
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

        if (await CheckRevocation(validated, _context.RequestAborted).ConfigureAwait(false) is { } rejected)
            return rejected;

        // The inbound claim map is not applied: JsonWebTokenHandler does not rewrite claim types the
        // way it would, so sub stays sub and roles stays roles.
        AuthenticationValidationLog.TokenAccepted(_log, ServiceName, validated.ClaimsIdentity.Name ?? "(no name)");

        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(validated.ClaimsIdentity), _scheme.Name));
    }

    /// <summary>
    /// Rejects a presented token whose <c>jti</c>, <c>sub</c> or <c>iat</c> is missing or unparseable, whose
    /// <c>jti</c> is on this host's deny-list, or whose subject's sessions were ended after it was issued.
    /// </summary>
    /// <param name="validated">The already signature-validated token and its claims.</param>
    /// <param name="cancellationToken">A token to cancel the check.</param>
    /// <returns>A failure to return from <see cref="AuthenticateAsync"/>, or <see langword="null"/> to continue.</returns>
    /// <remarks>
    /// <c>JwtTokenIssuer</c> mints <c>jti</c>, <c>sub</c> and <c>iat</c> unconditionally, so a token missing
    /// any of them did not come from this host's own issuer — fail loud rather than let it through unchecked.
    /// </remarks>
    private async Task<AuthenticateResult?> CheckRevocation(TokenValidationResult validated, CancellationToken cancellationToken)
    {
        if (validated.ClaimsIdentity.FindFirst(ClaimDefinitions.jti.Name)?.Value is not { Length: > 0 } jtiClaim
            || !Guid.TryParse(jtiClaim, out var jti))
        {
            return AuthenticateResult.Fail(Text(AuthenticationValidationLog.TokenRejected(
                _log, ServiceName, "missing or unparseable jti claim")));
        }

        if (validated.ClaimsIdentity.FindFirst(ClaimDefinitions.sub.Name)?.Value is not { Length: > 0 } subClaim
            || !Guid.TryParse(subClaim, out var userId))
        {
            return AuthenticateResult.Fail(Text(AuthenticationValidationLog.TokenRejected(
                _log, ServiceName, "missing or unparseable sub claim")));
        }

        if (validated.SecurityToken is not JsonWebToken { IssuedAt: var issuedAt } || issuedAt == DateTime.MinValue)
        {
            return AuthenticateResult.Fail(Text(AuthenticationValidationLog.TokenRejected(
                _log, ServiceName, "missing iat claim")));
        }

        var revoked = await _revocations
            .IsRevoked(jti, userId, new DateTimeOffset(DateTime.SpecifyKind(issuedAt, DateTimeKind.Utc)), cancellationToken)
            .ConfigureAwait(false);
        if (revoked.IsFailure)
            return AuthenticateResult.Fail(Text(AuthenticationValidationLog.TokenRejected(
                _log, ServiceName, "revocation check failed")));

        return revoked.Value
            ? AuthenticateResult.Fail(Text(AuthenticationValidationLog.TokenRejected(_log, ServiceName, "token revoked")))
            : null;
    }

    /// <summary>Builds what this scheme checks a token against, from the entry it was taken for.</summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <remarks>
    /// This runs on every authenticated request, so it reads one entry: the one the scheme was taken
    /// for, by the name the scheme carries, and only its row. The issuer and the audience are both
    /// fields of that row, and nothing built here hangs below it. It used to read every declared
    /// service whole, then this one again by Id (FDW-970).
    /// </remarks>
    private async Task<IGenericResult<TokenValidationParameters>> ResolveParameters(CancellationToken cancellationToken)
    {
        var record = await _configuration.Get(ServiceName, ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
        if (!record.IsSuccess)
            return record.ToNewResult<TokenValidationParameters>();

        // Get returns the implementation the domain row names, so the LocalKey contract is what tells
        // this entry from one of another kind. A name with no entry is a success carrying no value.
        if (record.Value is not ILocalKeyAuthenticationConfiguration body)
        {
            return GenericResult<TokenValidationParameters>.Failure(
                AuthenticationValidationLog.LocalKeyEntryUnreadable(_log, ServiceName));
        }

        // Through the same rule the binding was built with, so the string the selector matched and the
        // string checked here cannot differ - see IssuerName.
        var issuer = IssuerName.Read(body.Authority, ServiceName, _log);
        if (!issuer.IsSuccess || issuer.Value is null)
            return issuer.ToNewResult<TokenValidationParameters>();

        // Checked like every other input this method needs. A blank audience is not a null: the
        // column is nullable and the generated mapper converts DBNull to string.Empty, so an unset
        // audience arrives as "" and a null test would pass it through. Left unchecked it becomes
        // ValidateAudience = true with ValidAudience = "", which rejects every token for a reason
        // the log does not name.
        if (string.IsNullOrWhiteSpace(body.Audience))
        {
            return GenericResult<TokenValidationParameters>.Failure(
                AuthenticationValidationLog.LocalKeyMissingAudience(_log, ServiceName));
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

            // Both spellings — see JwtBearerAuthenticationHandler. The comparison is the JWT
            // handler's and ordinal, so the issuer is expressed in both forms rather than rewritten.
            ValidIssuers = IssuerName.Spellings(issuer.Value),
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

    // The contract carries its text on Message, which is non-nullable; object.ToString() would give
    // whatever the implementing type happens to render and is not the contract.
    private static string Text(IGenericMessage message) => message.Message;

    /// <inheritdoc />
    /// <remarks>
    /// A bare 401 and no <c>WWW-Authenticate</c> header at all. The realm and error codes a JwtBearer
    /// challenge writes describe the token that was rejected, and this host does not tell an
    /// unauthenticated caller why it failed.
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

}
