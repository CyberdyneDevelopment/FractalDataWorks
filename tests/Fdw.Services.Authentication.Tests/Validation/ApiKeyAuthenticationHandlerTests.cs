using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Authentication.Abstractions.Steps;
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
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.SignIns;
using Fdw.Services.Authentication.Tests.Flow;
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

    // The format the agent key service reports by default -- its configured prefix plus agent_.
    private const string AgentKeyFormat = "fdx_agent_";

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
    public async Task AJwtIsLeftForAnotherScheme()
    {
        var result = await Authenticate("Bearer some.jwt.value");

        result.None.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task TheKindComesFromTheAgentKeyServiceNotAFixedPrefix()
    {
        // FDW-939: the prefix is the credential service's configuration. A host configured with acme_
        // must route acme_agent_ keys as agent keys; the handler never knows the prefix.
        var result = await Authenticate(
            "Bearer acme_agent_abc123",
            agentKey: Valid(Guid.NewGuid(), "agent", 3),
            permissions: ["datasets:read"],
            agentKeyFormat: "acme_agent_");

        result.Succeeded.ShouldBeTrue();
        result.Principal!.FindFirst(ClaimDefinitions.agent.Name)!.Value.ShouldBe("true");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ACredentialWhoseKindCannotBeDeterminedIsRefused()
    {
        // The agent key service could not read its format: guessing a kind would send the credential
        // to a validator it may not belong to.
        var pat = new Mock<IPersonalAccessTokenService>(MockBehavior.Strict);

        var result = await Authenticate(
            $"Bearer {PersonalToken}",
            personalTokenMock: pat,
            agentKeyFormat: null);

        result.Succeeded.ShouldBeFalse();
        pat.VerifyNoOtherCalls();
    }

    private static readonly Guid OwnersDefaultTenant = new("5e5e5e5e-0000-0000-0000-000000000001");

    // The layered settings resolve a role's override from the standard role claim, so a key whose roles sat only under `roles`
    // would never reach its role-level settings (FDW-1209). Both are carried: readers of `roles` keep finding it.
    [Theory]
    [InlineData(AgentKey, true)]
    [InlineData(PersonalToken, false)]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheOwnersRolesAreCarriedUnderTheStandardRoleClaimAsWellAsRoles(string credential, bool agent)
    {
        var result = await Authenticate(
            $"Bearer {credential}",
            agentKey: agent ? Valid(Guid.NewGuid(), "label", 1) : null,
            personalToken: agent ? null : ValidPat(Guid.NewGuid()),
            permissions: ["connections:read"],
            roles: ["Auditor", "Operator"]);

        result.Succeeded.ShouldBeTrue();
        result.Principal!.FindAll(ClaimTypes.Role).Select(c => c.Value).ShouldBe(new[] { "Auditor", "Operator" }, ignoreOrder: true);
        result.Principal.FindAll(ClaimDefinitions.roles.Name).Select(c => c.Value).ShouldBe(new[] { "Auditor", "Operator" }, ignoreOrder: true);
    }

    // The marker the pagination reader asks for: the scheme that authenticated the caller names the identity, for both kinds.
    [Theory]
    [InlineData(AgentKey, true)]
    [InlineData(PersonalToken, false)]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheIdentityIsNamedAfterTheApiKeyScheme(string credential, bool agent)
    {
        var result = await Authenticate(
            $"Bearer {credential}",
            agentKey: agent ? Valid(Guid.NewGuid(), "label", 1) : null,
            personalToken: agent ? null : ValidPat(Guid.NewGuid()),
            permissions: ["connections:read"]);

        result.Succeeded.ShouldBeTrue();
        ApiKeyAuthenticationSchemes.Authenticated(result.Principal!).ShouldBeTrue();
    }

    [Fact]
    public async Task PersonalAccessTokenActsInItsOwnersDefaultTenant()
    {
        var userId = Guid.NewGuid();

        var result = await Authenticate($"Bearer {PersonalToken}", personalToken: ValidPat(userId), permissions: ["connections:read"]);

        result.Succeeded.ShouldBeTrue();
        result.Principal!.FindFirst(ClaimDefinitions.tenantId.Name)!.Value.ShouldBe(OwnersDefaultTenant.ToString());
        result.Principal.FindFirst(ClaimDefinitions.globalTenant.Name).ShouldBeNull();
        result.Principal.FindAll(ClaimDefinitions.perm.Name).ShouldHaveSingleItem().Value.ShouldBe("connections:read");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AnAgentKeyWhoseOwnerLacksTheAgentKindsPermissionIsRefused()
    {
        var result = await Authenticate(
            $"Bearer {AgentKey}",
            agentKey: Valid(Guid.NewGuid(), "vendor ingest", 7),
            permissions: ["datasets:read"],
            service: Service(user: null, agent: "signin:api-key"));

        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AnAgentKeyWhoseOwnerHoldsTheAgentKindsPermissionAuthenticates()
    {
        var result = await Authenticate(
            $"Bearer {AgentKey}",
            agentKey: Valid(Guid.NewGuid(), "vendor ingest", 7),
            permissions: ["datasets:read", "signin:api-key"],
            service: Service(user: null, agent: "signin:api-key"));

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task EachKindIsGatedByItsOwnRequirement()
    {
        // The user-kind requirement says nothing about agent keys: an operator may open one kind and
        // limit the other.
        var agent = await Authenticate(
            $"Bearer {AgentKey}",
            agentKey: Valid(Guid.NewGuid(), "agent", 1),
            permissions: ["datasets:read"],
            service: Service(user: "signin:api-key", agent: null));
        var personal = await Authenticate(
            $"Bearer {PersonalToken}",
            personalToken: ValidPat(Guid.NewGuid()),
            permissions: ["datasets:read"],
            service: Service(user: "signin:api-key", agent: null));

        agent.Succeeded.ShouldBeTrue();
        personal.Succeeded.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AServiceRowThatCannotBeReadRefusesTheKey()
    {
        // The requirement might be there; authenticating without knowing is the unsafe answer.
        var result = await Authenticate(
            $"Bearer {PersonalToken}",
            personalToken: ValidPat(Guid.NewGuid()),
            permissions: ["datasets:read"],
            service: GenericResult<IAuthenticationServiceImplementationConfiguration>.Failure(new GenericMessage("unreadable")));

        result.Succeeded.ShouldBeFalse();
    }

    // Mike, 2026-10-25 (ask 4f445b8c): an API key or personal access token request is a sign-in, recorded in the log of sign-ins.
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AnAgentKeyRequestRecordsASignInForItsOwner()
    {
        var userId = Guid.NewGuid();
        var signIns = new RecordingSignIns();

        var result = await Authenticate($"Bearer {AgentKey}", agentKey: Valid(userId, "agent", 1), permissions: ["connections:read"], signIns: signIns);

        result.Succeeded.ShouldBeTrue();
        var recorded = signIns.Recorded.ShouldHaveSingleItem();
        recorded.UserId.ShouldBe(userId);
        recorded.ServiceName.ShouldBe("ApiKey");
        recorded.FlowName.ShouldBe("agent key");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task APersonalAccessTokenRequestRecordsASignInForItsOwner()
    {
        var userId = Guid.NewGuid();
        var signIns = new RecordingSignIns();

        var result = await Authenticate($"Bearer {PersonalToken}", personalToken: ValidPat(userId), permissions: ["connections:read"], signIns: signIns);

        result.Succeeded.ShouldBeTrue();
        var recorded = signIns.Recorded.ShouldHaveSingleItem();
        recorded.UserId.ShouldBe(userId);
        recorded.ServiceName.ShouldBe("ApiKey");
        recorded.FlowName.ShouldBe("personal access token");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ACredentialThatIsNotValidRecordsNoSignIn()
    {
        var signIns = new RecordingSignIns();

        var result = await Authenticate(
            $"Bearer {AgentKey}",
            agentKey: GenericResult<AgentKeyValidationResult>.Success(new AgentKeyValidationResult { IsValid = false }),
            signIns: signIns);

        result.Succeeded.ShouldBeFalse();
        signIns.Recorded.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AKeyWhoseOwnerIsRefusedByTheServiceRecordsNoSignIn()
    {
        var signIns = new RecordingSignIns();

        var result = await Authenticate(
            $"Bearer {AgentKey}",
            agentKey: Valid(Guid.NewGuid(), "vendor ingest", 7),
            permissions: ["datasets:read"],
            service: Service(user: null, agent: "signin:api-key"),
            signIns: signIns);

        result.Succeeded.ShouldBeFalse();
        signIns.Recorded.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AKeyWhosePermissionsCannotBeResolvedRecordsNoSignIn()
    {
        var signIns = new RecordingSignIns();

        var result = await Authenticate($"Bearer {AgentKey}", agentKey: Valid(Guid.NewGuid(), "agent", 1), permissions: null, signIns: signIns);

        result.Succeeded.ShouldBeFalse();
        signIns.Recorded.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AuthenticatingTwiceInOneRequestRecordsOneSignIn()
    {
        var signIns = new RecordingSignIns();

        var result = await Authenticate(
            $"Bearer {AgentKey}", agentKey: Valid(Guid.NewGuid(), "agent", 1), permissions: ["connections:read"], signIns: signIns, authenticateTimes: 2);

        result.Succeeded.ShouldBeTrue();
        signIns.Recorded.ShouldHaveSingleItem();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AFailureToRecordTheSignInDoesNotRefuseTheRequest()
    {
        var signIns = new RecordingSignIns { Fails = true };

        var result = await Authenticate($"Bearer {AgentKey}", agentKey: Valid(Guid.NewGuid(), "agent", 1), permissions: ["connections:read"], signIns: signIns);

        result.Succeeded.ShouldBeTrue();
        signIns.Recorded.ShouldHaveSingleItem();
    }

    private static IGenericResult<IAuthenticationServiceImplementationConfiguration> Service(string? user, string? agent)
        => GenericResult<IAuthenticationServiceImplementationConfiguration>.Success(new ApiKeyAuthenticationConfiguration
        {
            Name = "ApiKey",
            Enabled = true,
            RequiredUserKeyPermission = user,
            RequiredAgentKeyPermission = agent,
        });

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
        Mock<IPersonalAccessTokenService>? personalTokenMock = null,
        IGenericResult<IAuthenticationServiceImplementationConfiguration>? service = null,
        string? agentKeyFormat = AgentKeyFormat,
        IReadOnlyCollection<string>? roles = null,
        ISignInLog? signIns = null,
        int authenticateTimes = 1)
    {
        var services = new ServiceCollection();

        if (signIns is not null)
        {
            services.AddSingleton(signIns);
        }

        // Always registered: it is what says which kind a credential is. A null format stands for a
        // service that cannot read its configuration.
        var agentKeys = new Mock<IAgentKeyService>();
        agentKeys.Setup(s => s.IsAgentKey(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string raw, CancellationToken _) => agentKeyFormat is null
                ? GenericResult<bool>.Failure(new GenericMessage("credential format unreadable"))
                : GenericResult<bool>.Success(raw.StartsWith(agentKeyFormat, StringComparison.Ordinal)));
        if (agentKey is not null)
        {
            agentKeys.Setup(s => s.ValidateKey(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(agentKey);
        }

        services.AddSingleton(agentKeys.Object);

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

        // An opaque credential acts in its owner's default tenant (FDW-825).
        var tenantResolver = new Mock<ITenantResolver>();
        tenantResolver.Setup(r => r.TenantFor(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Success(OwnersDefaultTenant));
        services.AddSingleton(tenantResolver.Object);
        var tenant = new Mock<ITenant>();
        tenant.SetupGet(t => t.Id).Returns(OwnersDefaultTenant);
        tenant.SetupGet(t => t.IsGlobal).Returns(false);
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.Setup(p => p.GetTenant(OwnersDefaultTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITenant>.Success(tenant.Object));
        services.AddSingleton(tenantProvider.Object);

        var resolver = new Mock<IUserPermissions>();
        resolver
            .Setup(r => r.Get(It.IsAny<string>(), OwnersDefaultTenant, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions is null
                ? GenericResult<EffectiveAuthorization>.Failure(new GenericMessage("resolver unavailable"))
                : GenericResult<EffectiveAuthorization>.Success(new EffectiveAuthorization(permissions, roles is { } held ? held : Array.Empty<string>(), validUntil: null)));

        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        context.Request.Headers.Authorization = authorizationHeader;
        context.Request.Path = "/connections";

        // The row the scheme was taken for. Ungated unless a test says otherwise.
        var configuration = new Mock<IAuthenticationServiceConfigurationProvider>();
        configuration.Setup(c => c.Get("ApiKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync(service ?? Service(user: null, agent: null));

        var handler = new ApiKeyAuthenticationHandler(
            resolver.Object, configuration.Object, NullLogger<ApiKeyAuthenticationHandler>.Instance);
        await handler.InitializeAsync(
            new AuthenticationScheme("Fdw.ApiKey.ApiKey", null, typeof(ApiKeyAuthenticationHandler)), context);

        var result = await handler.AuthenticateAsync();
        for (var again = 1; again < authenticateTimes; again++)
        {
            result = await handler.AuthenticateAsync();
        }

        return result;
    }
}
