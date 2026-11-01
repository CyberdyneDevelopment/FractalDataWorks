using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Sessions;
using Fdw.Services.Authentication.Validation;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.AspNetCore.Http;
using Moq;
using Shouldly;

namespace Fdw.Services.Authentication.Tests.Sessions;

/// <summary>
/// What a host's outbound API clients carry to the service a request reaches through (FDW-925): the
/// caller's own bearer token, a token minted for a session caller, or nothing.
/// </summary>
public sealed class ForwardedCallerTokenProviderTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset SessionEnds = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds());

    [Fact]
    public async Task A_bearer_caller_s_token_is_forwarded_as_presented()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer caller-token";
        var issuer = Issuer();

        var token = await Provider(context, issuer).GetAccessToken(TestContext.Current.CancellationToken);

        token.ShouldBe("caller-token");
        issuer.Verify(i => i.Issue(It.IsAny<IssuanceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task An_anonymous_caller_forwards_nothing()
    {
        var issuer = Issuer();

        var token = await Provider(new DefaultHttpContext(), issuer).GetAccessToken(TestContext.Current.CancellationToken);

        token.ShouldBeNull();
        issuer.Verify(i => i.Issue(It.IsAny<IssuanceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_caller_authenticated_some_other_way_gets_nothing_minted()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimDefinitions.sub.Name, UserId.ToString())], "Fdw.ApiKey.ApiKeys")),
        };
        var issuer = Issuer();

        var token = await Provider(context, issuer).GetAccessToken(TestContext.Current.CancellationToken);

        token.ShouldBeNull();
        issuer.Verify(i => i.Issue(It.IsAny<IssuanceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_session_caller_forwards_a_short_lived_token_minted_for_the_same_principal()
    {
        IssuanceRequest? minted = null;
        var issuer = Issuer(request => minted = request);
        var before = DateTimeOffset.UtcNow;

        var token = await Provider(SessionRequest(SessionEnds), issuer).GetAccessToken(TestContext.Current.CancellationToken);

        token.ShouldBe("minted-token");
        minted.ShouldNotBeNull();
        minted.PrincipalId.ShouldBe(UserId);
        minted.TenantId.ShouldBe(TenantId);
        minted.Audience.ShouldBe("reference-api");
        minted.AuthenticationMethods.ShouldBe(["pwd"]);
        minted.Claims[ClaimDefinitions.perm.Name].ShouldBe(["read:things", "write:things"]);
        minted.Claims[ClaimDefinitions.roles.Name].ShouldBe(["Admin"]);
        minted.ValidUntil.ShouldNotBeNull();
        minted.ValidUntil.Value.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow.AddMinutes(5));
        minted.ValidUntil.Value.ShouldBeGreaterThan(before);
    }

    [Fact]
    public async Task A_session_ending_sooner_than_the_delegation_lifetime_caps_the_token()
    {
        IssuanceRequest? minted = null;
        var endsSoon = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds());

        await Provider(SessionRequest(endsSoon), Issuer(request => minted = request)).GetAccessToken(TestContext.Current.CancellationToken);

        minted!.ValidUntil.ShouldBe(endsSoon);
    }

    [Fact]
    public async Task A_request_mints_once_however_many_calls_it_makes()
    {
        // Why the gate: the mint stays in flight while every call starts, so each later call finds a
        // mint that has not finished. An issuer that completes at once would let every call see a
        // finished token and prove nothing about calls that overlap.
        var gate = new TaskCompletionSource<IGenericResult<IssuedToken>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var issuer = new Mock<ITokenIssuer>();
        issuer.Setup(i => i.Issue(It.IsAny<IssuanceRequest>(), It.IsAny<CancellationToken>())).Returns(gate.Task);
        var provider = Provider(SessionRequest(SessionEnds), issuer);

        var calls = Enumerable.Range(0, 8)
            .Select(_ => provider.GetAccessToken(TestContext.Current.CancellationToken))
            .ToArray();
        calls.ShouldAllBe(call => !call.IsCompleted);
        issuer.Verify(i => i.Issue(It.IsAny<IssuanceRequest>(), It.IsAny<CancellationToken>()), Times.Once);

        gate.SetResult(GenericResult<IssuedToken>.Success(new IssuedToken
        {
            AccessToken = "minted-token",
            TokenType = "Bearer",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
        }));
        var tokens = await Task.WhenAll(calls);
        var later = await provider.GetAccessToken(TestContext.Current.CancellationToken);

        tokens.ShouldAllBe(t => string.Equals(t, "minted-token", StringComparison.Ordinal));
        later.ShouldBe("minted-token");
        issuer.Verify(i => i.Issue(It.IsAny<IssuanceRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_session_identity_that_cannot_be_read_back_forwards_nothing()
    {
        var context = SessionRequest(SessionEnds);
        var identity = (ClaimsIdentity)context.User.Identity!;
        identity.RemoveClaim(identity.FindFirst("aud")!);
        var issuer = Issuer();

        var token = await Provider(context, issuer).GetAccessToken(TestContext.Current.CancellationToken);

        token.ShouldBeNull();
        issuer.Verify(i => i.Issue(It.IsAny<IssuanceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task The_token_is_kept_on_the_request_not_on_the_provider()
    {
        var issuer = Issuer();
        var accessor = new HttpContextAccessor { HttpContext = SessionRequest(SessionEnds) };
        var provider = new ForwardedCallerTokenProvider(accessor, issuer.Object);

        await provider.GetAccessToken(TestContext.Current.CancellationToken);
        accessor.HttpContext = SessionRequest(SessionEnds);
        await provider.GetAccessToken(TestContext.Current.CancellationToken);

        issuer.Verify(i => i.Issue(It.IsAny<IssuanceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task A_token_that_cannot_be_minted_forwards_nothing_and_is_not_retried_within_the_request()
    {
        var issuer = new Mock<ITokenIssuer>();
        issuer.Setup(i => i.Issue(It.IsAny<IssuanceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IssuedToken>.Failure(new GenericMessage("no signing key")));
        var provider = Provider(SessionRequest(SessionEnds), issuer);

        var first = await provider.GetAccessToken(TestContext.Current.CancellationToken);
        var second = await provider.GetAccessToken(TestContext.Current.CancellationToken);

        first.ShouldBeNull();
        second.ShouldBeNull();
        issuer.Verify(i => i.Issue(It.IsAny<IssuanceRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ForwardedCallerTokenProvider Provider(HttpContext context, Mock<ITokenIssuer> issuer)
        => new(new HttpContextAccessor { HttpContext = context }, issuer.Object);

    private static Mock<ITokenIssuer> Issuer(Action<IssuanceRequest>? capture = null)
    {
        var issuer = new Mock<ITokenIssuer>();
        issuer.Setup(i => i.Issue(It.IsAny<IssuanceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IssuanceRequest request, CancellationToken _) =>
            {
                capture?.Invoke(request);
                return GenericResult<IssuedToken>.Success(new IssuedToken
                {
                    AccessToken = "minted-token",
                    TokenType = "Bearer",
                    ExpiresAt = request.ValidUntil!.Value,
                });
            });
        return issuer;
    }

    private static DefaultHttpContext SessionRequest(DateTimeOffset sessionEnds)
    {
        var session = new SessionRecord
        {
            Id = Guid.CreateVersion7(),
            ServiceName = "ConsoleSession",
            UserId = UserId,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            AbsoluteExpiresAt = sessionEnds,
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
                    [ClaimDefinitions.roles.Name] = ["Admin"],
                },
            },
        };

        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                SessionPrincipal.Claims(session), SessionAuthenticationType.SchemeNameFor("ConsoleSession"),
                ClaimDefinitions.sub.Name, ClaimDefinitions.roles.Name)),
        };
    }
}
