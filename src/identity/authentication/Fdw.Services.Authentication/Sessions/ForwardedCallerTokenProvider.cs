using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Authentication.Validation;
using Fdw.Services.TokenManagers.Abstractions;
using Fdw.Web.Http.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Sessions;

/// <summary>
/// The <see cref="IAccessTokenProvider"/> a host's outbound API clients attach: the caller's own
/// identity, carried to the service the request reaches through (FDW-925).
/// </summary>
/// <remarks>
/// <para>
/// Delegation, not a service identity. The downstream validates a token naming the caller and enforces
/// the caller's own permissions, so a proxy never reaches further than a direct call would.
/// </para>
/// <list type="bullet">
/// <item>A caller with a bearer token: that token, as presented.</item>
/// <item>A caller a Session scheme authenticated (FDW-903): the browser holds only a cookie, which
/// means nothing to another host, so this host mints a short-lived token for the same principal
/// asserting exactly what the session asserts.</item>
/// <item>Anyone else: nothing, and the downstream refuses as it would a direct call.</item>
/// </list>
/// <para>
/// A token is minted at most once per request and kept on the request, not on this instance. An
/// HttpClient's handler pipeline resolves its providers from a scope of its own that outlives one
/// request and serves several, so a field here would hand one caller's token to the next.
/// </para>
/// </remarks>
internal sealed class ForwardedCallerTokenProvider : IAccessTokenProvider
{
    private const string BearerPrefix = "Bearer ";

    private static readonly object MintedKey = new();

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenIssuer _issuer;
    private readonly ILogger<ForwardedCallerTokenProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="ForwardedCallerTokenProvider"/> class.</summary>
    /// <param name="httpContextAccessor">The request whose caller is forwarded.</param>
    /// <param name="issuer">This host's issuer, which mints for a session caller.</param>
    /// <param name="logger">The logger.</param>
    public ForwardedCallerTokenProvider(
        IHttpContextAccessor httpContextAccessor,
        ITokenIssuer issuer,
        ILogger<ForwardedCallerTokenProvider>? logger = null)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        _logger = logger ?? NullLogger<ForwardedCallerTokenProvider>.Instance;
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessToken(CancellationToken cancellationToken = default)
    {
        // Outside a request there is no caller, and so nothing of anyone's to forward.
        if (_httpContextAccessor.HttpContext is not { } context)
            return null;

        var header = context.Request.Headers.Authorization.ToString();
        if (header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            SessionLog.CallerTokenForwarded(_logger, context.Request.Path.ToString());
            return header[BearerPrefix.Length..].Trim();
        }

        if (SessionPrincipal.Identity(context.User) is not { } session)
        {
            SessionLog.NoCallerCredential(_logger, context.Request.Path.ToString());
            return null;
        }

        var minted = await Minted(context, session).WaitAsync(cancellationToken).ConfigureAwait(false);
        return minted.IsSuccess ? minted.Value!.AccessToken : null;
    }

    // The request's one mint, started by whichever outbound call needs it first. A failed mint is kept
    // too: a request that could not be given a token is not retried call by call.
    private Task<IGenericResult<IssuedToken>> Minted(HttpContext context, ClaimsIdentity session)
    {
        // Why the lock: a request may fan its outbound calls out in parallel, and Items is a plain
        // dictionary. Without it two calls could each find nothing there and mint twice.
        lock (context.Items)
        {
            if (context.Items.TryGetValue(MintedKey, out var existing)
                && existing is Task<IGenericResult<IssuedToken>> minting)
            {
                SessionLog.DelegationTokenReused(_logger, context.Request.Path.ToString());
                return minting;
            }

            // The request's own cancellation, not the first caller's: every call of the request shares
            // this mint, and one of them giving up must not cancel it for the rest.
            var started = Mint(session, context.Request.Path.ToString(), context.RequestAborted);
            context.Items[MintedKey] = started;
            return started;
        }
    }

    private async Task<IGenericResult<IssuedToken>> Mint(ClaimsIdentity session, string path, CancellationToken cancellationToken)
    {
        var assertion = SessionPrincipal.Assertion(session, DateTimeOffset.UtcNow.Add(AuthenticationServiceTypes.DelegationTokenLifetime), _logger);
        if (assertion.IsFailure)
        {
            SessionLog.DelegationTokenNotIssued(_logger, path, "the session's claims could not be read back");
            return assertion.ToNewResult<IssuedToken>();
        }

        var issued = await _issuer.Issue(assertion.Value!, cancellationToken).ConfigureAwait(false);
        if (issued.IsFailure)
        {
            SessionLog.DelegationTokenNotIssued(_logger, path, "this host's issuer refused to mint it");
            return issued;
        }

        SessionLog.DelegationTokenMinted(_logger, assertion.Value!.PrincipalId, assertion.Value.TenantId,
            assertion.Value.Audience, issued.Value!.ExpiresAt);
        return issued;
    }
}
