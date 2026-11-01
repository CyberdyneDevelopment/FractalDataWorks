using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Sessions;
using Fdw.Services.Authentication.Validation;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Fdw.Services.Authentication.Tests.Sessions;

/// <summary>
/// The two directions between a session and its principal (FDW-903, FDW-925): the claims the
/// principal carries, and the assertion read back from them to mint a token for the same caller.
/// </summary>
public sealed class SessionPrincipalTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid SessionId = Guid.CreateVersion7();
    private static readonly DateTimeOffset SessionEnds = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds());

    [Fact]
    public void The_principal_carries_the_audience_and_the_end_of_the_session()
    {
        var identity = Identity(Session());

        identity.FindFirst("aud")!.Value.ShouldBe("reference-api");
        identity.FindFirst("exp")!.Value.ShouldBe(SessionEnds.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void The_assertion_read_back_is_the_assertion_baked_at_sign_in()
    {
        var assertion = SessionPrincipal.Assertion(Identity(Session()), SessionEnds.AddHours(1), NullLogger.Instance);

        assertion.IsSuccess.ShouldBeTrue();
        var request = assertion.Value!;
        request.PrincipalId.ShouldBe(UserId);
        request.TenantId.ShouldBe(TenantId);
        request.Audience.ShouldBe("reference-api");
        request.Scopes.ShouldBe(["api", "offline"]);
        request.AuthenticationMethods.ShouldBe(["pwd", "otp"]);
        request.Acr.ShouldBe("urn:fdw:acr:mfa");
        request.Claims.Keys.OrderBy(k => k, StringComparer.Ordinal).ShouldBe(
            [ClaimDefinitions.globalTenant.Name, ClaimDefinitions.perm.Name, ClaimDefinitions.roles.Name]);
        request.Claims[ClaimDefinitions.perm.Name].ShouldBe(["read:things", "write:things"]);
        request.Claims[ClaimDefinitions.roles.Name].ShouldBe(["Admin", "Operator"]);
        request.Claims[ClaimDefinitions.globalTenant.Name].ShouldBe(["true"]);
    }

    [Fact]
    public void The_principal_s_own_bookkeeping_is_not_asserted_as_a_claim()
    {
        var request = SessionPrincipal.Assertion(Identity(Session()), SessionEnds.AddHours(1), NullLogger.Instance).Value!;

        request.Claims.Keys.ShouldNotContain(ClaimDefinitions.sid.Name);
        request.Claims.Keys.ShouldNotContain(ClaimDefinitions.sub.Name);
        request.Claims.Keys.ShouldNotContain(ClaimTypes.NameIdentifier);
        request.Claims.Keys.ShouldNotContain("aud");
        request.Claims.Keys.ShouldNotContain("exp");
    }

    [Fact]
    public void A_token_minted_for_a_session_ends_no_later_than_asked()
    {
        var notAfter = DateTimeOffset.UtcNow.AddMinutes(5);

        SessionPrincipal.Assertion(Identity(Session()), notAfter, NullLogger.Instance).Value!.ValidUntil.ShouldBe(notAfter);
    }

    [Fact]
    public void A_token_minted_for_a_session_never_outlives_the_session()
    {
        SessionPrincipal.Assertion(Identity(Session()), SessionEnds.AddHours(1), NullLogger.Instance).Value!.ValidUntil.ShouldBe(SessionEnds);
    }

    [Fact]
    public void An_identity_without_the_session_s_end_is_not_re_asserted()
    {
        var identity = Identity(Session());
        identity.RemoveClaim(identity.FindFirst("exp")!);

        SessionPrincipal.Assertion(identity, SessionEnds, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void An_identity_without_an_audience_is_not_re_asserted()
    {
        var identity = Identity(Session());
        identity.RemoveClaim(identity.FindFirst("aud")!);

        SessionPrincipal.Assertion(identity, SessionEnds, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Only_an_authenticated_session_identity_is_found()
    {
        var bearer = new ClaimsIdentity([new Claim(ClaimDefinitions.sub.Name, UserId.ToString())], "Fdw.LocalKey.FdwAuthority");
        var anonymous = new ClaimsIdentity();

        SessionPrincipal.Identity(new ClaimsPrincipal(bearer)).ShouldBeNull();
        SessionPrincipal.Identity(new ClaimsPrincipal(anonymous)).ShouldBeNull();
        SessionPrincipal.Identity(new ClaimsPrincipal([bearer, Identity(Session())]))!.AuthenticationType
            .ShouldBe(SessionAuthenticationType.SchemeNameFor("ConsoleSession"));
    }

    private static ClaimsIdentity Identity(SessionRecord session)
        => new(SessionPrincipal.Claims(session), SessionAuthenticationType.SchemeNameFor("ConsoleSession"),
            ClaimDefinitions.sub.Name, ClaimDefinitions.roles.Name);

    private static SessionRecord Session()
        => new()
        {
            Id = SessionId,
            ServiceName = "ConsoleSession",
            UserId = UserId,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            AbsoluteExpiresAt = SessionEnds,
            Assertion = new IssuanceRequest
            {
                PrincipalId = UserId,
                TenantId = TenantId,
                Audience = "reference-api",
                Scopes = ["api", "offline"],
                AuthenticationMethods = ["pwd", "otp"],
                Acr = "urn:fdw:acr:mfa",
                ValidUntil = SessionEnds,
                Claims = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
                {
                    [ClaimDefinitions.perm.Name] = ["read:things", "write:things"],
                    [ClaimDefinitions.roles.Name] = ["Admin", "Operator"],
                    [ClaimDefinitions.globalTenant.Name] = ["true"],
                },
            },
        };
}
