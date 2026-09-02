using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Validation;
using Fdw.Services.Authorization.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Validation;

/// <summary>
/// What an opaque credential authenticates AS.
/// </summary>
/// <remarks>
/// The permission claims are the point. A JWT arrives with its permissions baked in at issuance; an
/// agent key or PAT has no issuance moment to bake them at, so a handler that authenticated the
/// caller and stopped there would leave it authenticated and unable to call anything.
/// </remarks>
public class ApiKeyAuthenticationHandlerTests
{
    private const string AgentKey = "fdx_agent_abc123";
    private const string PersonalToken = "fdx_prod_abc123";

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AgentKeyCarriesTheAgentClaimsAndThePermissionsItsOwnerHolds()
    {
        var userId = Guid.NewGuid();
        var result = await Authenticate(
            $"Bearer {AgentKey}",
            agentKey: Valid(userId, "mike - claude code", 42),
            permissions: ["connections:read", "datasets:read"]);

        result.Succeeded.ShouldBeTrue();
        var claims = result.Principal!.Claims.ToList();

        // sub stays the OWNER's: the agent acts as that person, so every permission check and RLS
        // predicate downstream must keep seeing them.
        claims.ShouldContain(c => c.Type == ClaimDefinitions.sub.Name && c.Value == userId.ToString());
        claims.ShouldContain(c => c.Type == ClaimDefinitions.agent.Name && c.Value == "true");
        claims.ShouldContain(c => c.Type == ClaimDefinitions.agentLabel.Name && c.Value == "mike - claude code");
        claims.ShouldContain(c => c.Type == ClaimDefinitions.agentKeyId.Name && c.Value == "42");

        claims.Count(c => c.Type == ClaimDefinitions.perm.Name).ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task PersonalAccessTokenCarriesPermissionsAndIsNotMarkedAsAnAgent()
    {
        var result = await Authenticate(
            $"Bearer {PersonalToken}",
            personalToken: ValidPat(Guid.NewGuid()),
            permissions: ["connections:read"]);

        result.Succeeded.ShouldBeTrue();
        result.Principal!.Claims.Count(c => c.Type == ClaimDefinitions.perm.Name).ShouldBe(1);
        result.Principal.FindFirst(ClaimDefinitions.agent.Name).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task FailedPermissionResolutionDeniesRatherThanAuthenticatingWithNone()
    {
        // The resolver's contract says callers must treat failure as deny. Succeeding with an empty
        // set would be indistinguishable from a legitimately unprivileged user, and would turn a
        // resolver outage into a silent, total loss of authorization for every key-authenticated call.
        var result = await Authenticate(
            $"Bearer {AgentKey}",
            agentKey: Valid(Guid.NewGuid(), "agent", 1),
            permissions: null);

        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AnAgentKeyIsNeverOfferedToThePersonalTokenService()
    {
        // Sending it to the wrong validator reports an unrecognised agent key as a bad token, and
        // whoever reads the log cannot tell which credential actually failed.
        var pat = new Mock<IPersonalAccessTokenService>(MockBehavior.Strict);

        var result = await Authenticate(
            $"Bearer {AgentKey}",
            agentKey: GenericResult<AgentKeyValidationResult>.Success(new AgentKeyValidationResult { IsValid = false }),
            personalTokenMock: pat);

        result.Succeeded.ShouldBeFalse();
        pat.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public async Task AHeaderThatIsNotAnFdxCredentialIsLeftForAnotherScheme()
    {
        var result = await Authenticate("Bearer some.jwt.value");

        result.None.ShouldBeTrue();
    }

    private static IGenericResult<AgentKeyValidationResult> Valid(Guid userId, string label, int keyId)
        => GenericResult<AgentKeyValidationResult>.Success(new AgentKeyValidationResult
        {
            IsValid = true, UserId = userId, KeyId = Guid.NewGuid(), AgentKeyId = keyId, Label = label,
        });

    private static IGenericResult<PersonalAccessTokenValidationResult> ValidPat(Guid userId)
        => GenericResult<PersonalAccessTokenValidationResult>.Success(
            new PersonalAccessTokenValidationResult { IsValid = true, UserId = userId });

    private static async Task<AuthenticateResult> Authenticate(
        string authorizationHeader,
        IGenericResult<AgentKeyValidationResult>? agentKey = null,
        IGenericResult<PersonalAccessTokenValidationResult>? personalToken = null,
        IReadOnlyCollection<string>? permissions = null,
        Mock<IPersonalAccessTokenService>? personalTokenMock = null)
    {
        var services = new ServiceCollection();

        if (agentKey is not null)
        {
            var m = new Mock<IAgentKeyService>();
            m.Setup(s => s.ValidateKey(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(agentKey);
            services.AddSingleton(m.Object);
        }

        if (personalTokenMock is not null)
        {
            services.AddSingleton(personalTokenMock.Object);
        }
        else if (personalToken is not null)
        {
            var m = new Mock<IPersonalAccessTokenService>();
            m.Setup(s => s.ValidateToken(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(personalToken);
            services.AddSingleton(m.Object);
        }

        var resolver = new Mock<IEffectivePermissionResolver>();
        resolver
            .Setup(r => r.Resolve(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions is null
                ? GenericResult<IReadOnlyCollection<string>>.Failure(new GenericMessage("resolver unavailable"))
                : GenericResult<IReadOnlyCollection<string>>.Success(permissions));

        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        context.Request.Headers.Authorization = authorizationHeader;
        context.Request.Path = "/connections";

        var handler = new ApiKeyAuthenticationHandler(resolver.Object, NullLogger<ApiKeyAuthenticationHandler>.Instance);
        await handler.InitializeAsync(
            new AuthenticationScheme("Fdw.ApiKey.ApiKey", null, typeof(ApiKeyAuthenticationHandler)), context);

        return await handler.AuthenticateAsync();
    }
}
