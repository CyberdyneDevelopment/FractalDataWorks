using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Sessions;
using Fdw.Services.Authentication.Validation;
using Fdw.Services.TokenManagers;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Revocation;

/// <summary>
/// FDW-5655: a credential issued after a user-wide cut-off is accepted, even in the same wall-clock second; one issued
/// before it is still refused. A bearer token presents the instant its jti carries (milliseconds), never its iat (whole
/// seconds); a session presents its exact CreatedAt.
/// </summary>
public sealed class SameSecondRevocationTests
{
    private const string ServiceName = "ReferenceApi";
    private const string Issuer = "https://fdw.test";
    private const string Audience = "reference-api";

    private static readonly Guid UserId = Guid.Parse("0a1b2c3d-0000-0000-0000-000000005655");
    private static readonly Guid TenantId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    // A whole second: the unit iat carries. The BehaviorSuite run that found the defect signed in at 00:29:49.
    private static readonly DateTimeOffset Second = new(2026, 11, 13, 0, 29, 49, TimeSpan.Zero);

    // 310.4321 ms into the second: a cut-off written at full clock precision, as DateTimeOffset.UtcNow gives it.
    private static readonly DateTimeOffset CutOff = Second.AddTicks(3_104_321);

    private sealed class FixedKey : ISigningCredentialProvider
    {
        private readonly SigningCredentials _credentials = new(
            new RsaSecurityKey(RSA.Create(2048)) { KeyId = "test-key" },
            SecurityAlgorithms.RsaSha256);

        public Task<IGenericResult<SigningCredentials>> Current(CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<SigningCredentials>.Success(_credentials));
    }

    // The rule TokenRevocationStore.IsRevoked puts in its query: refused when a cut-off for the subject is at or after
    // the issue instant presented.
    private static bool Refused(DateTimeOffset cutOff, DateTimeOffset presented) => cutOff >= presented;

    // What a bearer token issued at issuedAt presents: the instant its jti carries, as the issuer mints it.
    private static DateTimeOffset Presented(DateTimeOffset issuedAt)
    {
        TokenIssueSequence.TryReadIssuedAt(Guid.CreateVersion7(issuedAt), out var presented).ShouldBeTrue();
        TokenIssueSequence.AgreesWithIssuedAt(presented, Second).ShouldBeTrue();
        return presented;
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void A_token_issued_after_the_cut_off_in_the_same_second_is_accepted()
    {
        Refused(CutOff, Presented(Second.AddMilliseconds(620))).ShouldBeFalse();

        // What it was refused on before: its iat, the start of the same second.
        Refused(CutOff, Second).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void A_token_issued_before_the_cut_off_in_the_same_second_is_still_refused()
        => Refused(CutOff, Presented(Second.AddMilliseconds(120))).ShouldBeTrue();

    // The boundary of the precision a jti carries. The cut-off falls in millisecond 310 (ticks 3_100_000 to 3_109_999):
    // a token issued in that millisecond cannot say whether it came first, so it is refused; from the next one it is not.
    [Theory]
    [Trait("Priority", "P1")]
    [InlineData(3_099_999, true)]
    [InlineData(3_100_000, true)]
    [InlineData(3_104_321, true)]
    [InlineData(3_104_322, true)]
    [InlineData(3_109_999, true)]
    [InlineData(3_110_000, false)]
    public void At_the_millisecond_boundary_only_the_cut_offs_own_millisecond_is_refused(long ticksIntoTheSecond, bool refused)
        => Refused(CutOff, Presented(Second.AddTicks(ticksIntoTheSecond))).ShouldBe(refused);

    [Fact]
    public void A_jti_that_is_not_version_7_carries_no_issue_instant()
    {
        TokenIssueSequence.TryReadIssuedAt(Guid.NewGuid(), out _).ShouldBeFalse();
        TokenIssueSequence.TryReadIssuedAt(Guid.Empty, out _).ShouldBeFalse();
    }

    [Fact]
    public void A_jti_whose_instant_is_outside_the_iat_second_does_not_agree_with_it()
    {
        TokenIssueSequence.TryReadIssuedAt(Guid.CreateVersion7(Second.AddSeconds(1)), out var later).ShouldBeTrue();
        TokenIssueSequence.AgreesWithIssuedAt(later, Second).ShouldBeFalse();

        TokenIssueSequence.TryReadIssuedAt(Guid.CreateVersion7(Second.AddMilliseconds(-1)), out var earlier).ShouldBeTrue();
        TokenIssueSequence.AgreesWithIssuedAt(earlier, Second).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task The_issuer_mints_the_jti_from_the_issue_instant_and_the_handler_presents_it()
    {
        var key = new FixedKey();
        var token = await Issue(key);
        var issued = new JsonWebToken(token);
        TokenIssueSequence.TryReadIssuedAt(Guid.Parse(issued.Id), out var carried).ShouldBeTrue();
        TokenIssueSequence.AgreesWithIssuedAt(carried, new DateTimeOffset(issued.IssuedAt, TimeSpan.Zero)).ShouldBeTrue();

        var presented = new List<DateTimeOffset>();
        var result = await Authenticate(key, token, (_, issuedAt) =>
        {
            presented.Add(issuedAt);
            return false;
        });

        result.Succeeded.ShouldBeTrue();
        presented.ShouldBe([carried]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task A_bearer_token_issued_after_a_cut_off_in_the_same_second_authenticates()
    {
        var key = new FixedKey();
        var token = await Issue(key);
        TokenIssueSequence.TryReadIssuedAt(Guid.Parse(new JsonWebToken(token).Id), out var carried).ShouldBeTrue();

        // One tick before the millisecond the token was issued in: the cut-off came first, in the same second unless
        // the token happened to be issued in the second's very first millisecond.
        (await Authenticate(key, token, (_, issuedAt) => Refused(carried.AddTicks(-1), issuedAt)))
            .Succeeded.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task A_bearer_token_issued_before_a_cut_off_is_refused()
    {
        var key = new FixedKey();
        var token = await Issue(key);

        (await Authenticate(key, token, (_, issuedAt) => Refused(DateTimeOffset.UtcNow, issuedAt)))
            .Succeeded.ShouldBeFalse();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public async Task A_session_presents_its_exact_creation_instant(long cutOffTicksFromCreation, bool refused)
    {
        var createdAt = CutOff.AddTicks(-cutOffTicksFromCreation);
        var presented = new List<DateTimeOffset>();
        var sessions = new Mock<ISessionStore>();
        sessions.Setup(s => s.Use("raw-session-id", "ConsoleSession", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<SessionRecord>.Success(new SessionRecord
            {
                Id = Guid.CreateVersion7(createdAt),
                ServiceName = "ConsoleSession",
                UserId = UserId,
                CreatedAt = createdAt,
                AbsoluteExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                Assertion = new IssuanceRequest
                {
                    PrincipalId = UserId,
                    TenantId = TenantId,
                    Audience = Audience,
                    Scopes = [],
                    AuthenticationMethods = ["pwd"],
                },
            }));
        var revocations = new Mock<ITokenRevocationStore>();
        revocations.Setup(r => r.IsRevoked(It.IsAny<Guid>(), UserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid _, DateTimeOffset issuedAt, CancellationToken _) =>
            {
                presented.Add(issuedAt);
                return GenericResult<bool>.Success(Refused(CutOff, issuedAt));
            });

        var result = await new SessionAuthenticator(sessions.Object, revocations.Object)
            .Authenticate("raw-session-id", "ConsoleSession", "Session", TestContext.Current.CancellationToken);

        presented.ShouldBe([createdAt]);
        result.IsFailure.ShouldBe(refused);
    }

    private static async Task<string> Issue(ISigningCredentialProvider key)
    {
        var issued = await new JwtTokenIssuer(
                new JwtTokenIssuerConfiguration { Issuer = Issuer, Lifetime = TimeSpan.FromMinutes(15) },
                key)
            .Issue(new IssuanceRequest
            {
                PrincipalId = UserId,
                TenantId = TenantId,
                Audience = Audience,
                Scopes = [],
                AuthenticationMethods = ["pwd"],
            }, TestContext.Current.CancellationToken);

        issued.IsSuccess.ShouldBeTrue();
        return issued.Value!.AccessToken;
    }

    // Authenticates a bearer token through the LocalKey scheme; revoked answers the deny-list's question from the user
    // and the issue instant the handler presents.
    private static async Task<AuthenticateResult> Authenticate(
        ISigningCredentialProvider key, string token, Func<Guid, DateTimeOffset, bool> revoked)
    {
        var configuration = new Mock<IAuthenticationServiceConfigurationProvider>();
        configuration.Setup(p => p.Get(ServiceName, ConfigurationLevels.RecordOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IAuthenticationServiceImplementationConfiguration>.Success(new LocalKeyAuthenticationConfiguration
            {
                Name = ServiceName,
                Authority = Issuer,
                Audience = Audience,
            }));

        var revocations = new Mock<ITokenRevocationStore>();
        revocations.Setup(r => r.IsRevoked(It.IsAny<Guid>(), UserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid userId, DateTimeOffset issuedAt, CancellationToken _) =>
                GenericResult<bool>.Success(revoked(userId, issuedAt)));

        var handler = new LocalKeyAuthenticationHandler(configuration.Object, key, revocations.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer " + token;
        await handler.InitializeAsync(
            new AuthenticationScheme(LocalKeyAuthenticationType.SchemeNameFor(ServiceName), null, typeof(LocalKeyAuthenticationHandler)),
            context);

        return await handler.AuthenticateAsync();
    }
}
