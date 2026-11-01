using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Sessions;
using Fdw.Services.Authentication.Validation;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Authentication.Tests.Sessions;

/// <summary>
/// How the Session scheme turns a cookie into a principal (FDW-903): the store decides whether the
/// session is live, user-wide revocation still ends it, writes pass CSRF protection, and the principal
/// carries what a bearer token from the same flow would.
/// </summary>
public sealed class SessionAuthenticationHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid SessionId = Guid.CreateVersion7();

    [Fact]
    public async Task A_request_without_the_cookie_is_not_this_schemes_to_answer()
    {
        var result = await Authenticate(Request(HttpMethods.Get, cookie: null), Live());

        result.None.ShouldBeTrue();
    }

    [Fact]
    public async Task A_live_session_authenticates_with_the_claims_baked_at_sign_in()
    {
        var result = await Authenticate(Request(HttpMethods.Get), Live());

        result.Succeeded.ShouldBeTrue();
        var principal = result.Principal!;
        principal.FindFirst(ClaimDefinitions.sub.Name)!.Value.ShouldBe(UserId.ToString());
        principal.FindFirst(ClaimDefinitions.tenantId.Name)!.Value.ShouldBe(TenantId.ToString());
        principal.FindFirst(ClaimDefinitions.sid.Name)!.Value.ShouldBe(SessionId.ToString());
        principal.FindAll(ClaimDefinitions.perm.Name).Select(c => c.Value).ShouldBe(["read:things", "write:things"]);
    }

    [Fact]
    public async Task A_claim_the_assertion_repeats_under_a_reserved_name_is_not_added_twice()
    {
        var result = await Authenticate(Request(HttpMethods.Get), Live());

        result.Principal!.FindAll(ClaimDefinitions.sub.Name).Count().ShouldBe(1);
    }

    [Fact]
    public async Task The_principal_reads_back_as_the_assertion_baked_at_sign_in()
    {
        var result = await Authenticate(Request(HttpMethods.Get), Live());

        var identity = SessionPrincipal.Identity(result.Principal!);
        identity.ShouldNotBeNull();
        var assertion = SessionPrincipal.Assertion(identity, DateTimeOffset.UtcNow.AddMinutes(5), NullLogger.Instance);
        assertion.IsSuccess.ShouldBeTrue();
        assertion.Value!.PrincipalId.ShouldBe(UserId);
        assertion.Value.TenantId.ShouldBe(TenantId);
        assertion.Value.Audience.ShouldBe("reference-api");
        assertion.Value.Claims[ClaimDefinitions.perm.Name].ShouldBe(["read:things", "write:things"]);
    }

    [Fact]
    public async Task A_session_the_store_refuses_does_not_authenticate()
    {
        var result = await Authenticate(Request(HttpMethods.Get), GenericResult<SessionRecord>.Failure(new GenericMessage("not live")));

        result.Succeeded.ShouldBeFalse();
        result.None.ShouldBeFalse();
    }

    [Fact]
    public async Task A_user_revoked_after_the_session_began_does_not_authenticate()
    {
        var result = await Authenticate(Request(HttpMethods.Get), Live(), revoked: true);

        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task A_write_from_a_listed_origin_authenticates()
    {
        var result = await Authenticate(Request(HttpMethods.Post, origin: "https://console.example"), Live());

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task A_write_that_fails_csrf_protection_does_not_authenticate()
    {
        var result = await Authenticate(Request(HttpMethods.Post, origin: "https://attacker.example"), Live());

        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task A_read_is_not_held_to_csrf_protection()
    {
        var result = await Authenticate(Request(HttpMethods.Get, origin: "https://attacker.example"), Live());

        result.Succeeded.ShouldBeTrue();
    }

    // The Antiforgery protection ties its token to the user it was issued to (the session's principal,
    // when GET /auth/session/csrf ran signed in) and compares that with HttpContext.User when it
    // validates. The handler runs before User is set, so it gives the check the session's principal.

    [Fact]
    public async Task An_antiforgery_write_with_a_token_issued_to_the_signed_in_session_authenticates()
    {
        var services = AntiforgeryServices();
        var context = AntiforgeryWrite(services, issuedTo: SessionUser());

        var result = await Authenticate(context, Live(), antiforgery: true);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task An_antiforgery_write_with_a_token_issued_to_another_user_does_not_authenticate()
    {
        var services = AntiforgeryServices();
        var another = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimDefinitions.sub.Name, Guid.NewGuid().ToString())], "test", ClaimDefinitions.sub.Name, ClaimDefinitions.roles.Name));
        var context = AntiforgeryWrite(services, issuedTo: another);

        var result = await Authenticate(context, Live(), antiforgery: true);

        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task The_request_user_is_the_same_after_the_check_passes_and_after_it_refuses()
    {
        var services = AntiforgeryServices();
        var before = new ClaimsPrincipal(new ClaimsIdentity());

        var accepted = AntiforgeryWrite(services, issuedTo: SessionUser());
        accepted.User = before;
        (await Authenticate(accepted, Live(), antiforgery: true)).Succeeded.ShouldBeTrue();
        accepted.User.ShouldBeSameAs(before);

        var refused = AntiforgeryWrite(services, issuedTo: SessionUser(), header: "not-the-token");
        refused.User = before;
        (await Authenticate(refused, Live(), antiforgery: true)).Succeeded.ShouldBeFalse();
        refused.User.ShouldBeSameAs(before);
    }

    [Fact]
    public async Task Authenticating_the_same_request_twice_passes_both_times()
    {
        // The framework asks a scheme more than once per request (authentication, then each policy).
        var services = AntiforgeryServices();
        var context = AntiforgeryWrite(services, issuedTo: SessionUser());

        (await Authenticate(context, Live(), antiforgery: true)).Succeeded.ShouldBeTrue();
        (await Authenticate(context, Live(), antiforgery: true)).Succeeded.ShouldBeTrue();
    }

    private static async Task<AuthenticateResult> Authenticate(
        HttpContext context, IGenericResult<SessionRecord> session, bool revoked = false, bool antiforgery = false)
    {
        var configuration = new Mock<IAuthenticationServiceConfigurationProvider>();
        configuration.Setup(p => p.Get("ConsoleSession", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IAuthenticationServiceImplementationConfiguration>.Success(new SessionAuthenticationConfiguration
            {
                Name = "ConsoleSession",
                CookieName = "fdw_session",
                SameSite = "Strict",
                CsrfProtection = antiforgery ? "Antiforgery" : "OriginCheck",
                AllowedOrigins = antiforgery ? null : "https://console.example",
            }));

        var sessions = new Mock<ISessionStore>();
        sessions.Setup(s => s.Use("raw-session-id", "ConsoleSession", It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var revocations = new Mock<ITokenRevocationStore>();
        revocations.Setup(r => r.IsRevoked(SessionId, UserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(revoked));

        // The real authenticator over the mocked store and deny-list: expiry and revocation are its
        // decisions, and these tests prove the scheme still refuses on them.
        var handler = new SessionAuthenticationHandler(
            configuration.Object, new SessionAuthenticator(sessions.Object, revocations.Object));
        await handler.InitializeAsync(
            new AuthenticationScheme(SessionAuthenticationType.SchemeNameFor("ConsoleSession"), null, typeof(SessionAuthenticationHandler)),
            context);
        return await handler.AuthenticateAsync();
    }

    private static DefaultHttpContext Request(string method, string? cookie = "fdw_session=raw-session-id", string? origin = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = "/api/v1/things";
        if (cookie is not null)
            context.Request.Headers.Cookie = cookie;
        if (origin is not null)
            context.Request.Headers.Origin = origin;
        return context;
    }

    private static ServiceProvider AntiforgeryServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddAntiforgery(o => o.HeaderName = "X-CSRF-Token");
        return services.BuildServiceProvider();
    }

    // The principal the handler builds for Live(): the caller GET /auth/session/csrf saw, and so the
    // one a token is issued to.
    private static ClaimsPrincipal SessionUser()
        => new(new ClaimsIdentity(
            SessionPrincipal.Claims(Live().Value!),
            SessionAuthenticationType.SchemeNameFor("ConsoleSession"), ClaimDefinitions.sub.Name, ClaimDefinitions.roles.Name));

    // A write carrying the session cookie and an antiforgery token pair: minted on one context signed
    // in as <paramref name="issuedTo"/>, then carried, cookie and header, onto a second context that is
    // not yet signed in -- the way a browser round-trip reaches the handler.
    private static DefaultHttpContext AntiforgeryWrite(ServiceProvider services, ClaimsPrincipal issuedTo, string? header = null)
    {
        var issuing = new DefaultHttpContext { RequestServices = services, User = issuedTo };
        var tokens = services.GetRequiredService<IAntiforgery>().GetAndStoreTokens(issuing);
        var antiforgeryCookie = issuing.Response.Headers.SetCookie.ToString().Split(';')[0];

        var context = Request(HttpMethods.Post, cookie: $"fdw_session=raw-session-id; {antiforgeryCookie}");
        context.RequestServices = services;
        context.Request.Headers["X-CSRF-Token"] = header ?? tokens.RequestToken;
        return context;
    }

    private static IGenericResult<SessionRecord> Live()
        => GenericResult<SessionRecord>.Success(new SessionRecord
        {
            Id = SessionId,
            ServiceName = "ConsoleSession",
            UserId = UserId,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            AbsoluteExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            Assertion = new IssuanceRequest
            {
                PrincipalId = UserId,
                TenantId = TenantId,
                Audience = "reference-api",
                Scopes = [],
                AuthenticationMethods = ["pwd"],
                Claims = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
                {
                    [ClaimDefinitions.perm.Name] = ["read:things", "write:things"],
                    [ClaimDefinitions.sub.Name] = ["someone-else"],
                },
            },
        });
}
