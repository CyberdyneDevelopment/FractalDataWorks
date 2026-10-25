using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Execution;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.TokenManagers;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Flow;

/// <summary>
/// Runs a whole flow against the real runner, the real assurance policy and the real issuer, and
/// checks the token that comes out — the parts only mean something composed.
/// </summary>
public sealed class EndToEndFlowTests
{
    private static readonly Guid PrincipalId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TenantId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private sealed class FixedKey : ISigningCredentialProvider
    {
        private readonly SigningCredentials _credentials = new(
            new RsaSecurityKey(RSA.Create(2048)) { KeyId = "test-key" },
            SecurityAlgorithms.RsaSha256);

        public SecurityKey Key => _credentials.Key;

        public Task<IGenericResult<SigningCredentials>> Current(CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<SigningCredentials>.Success(_credentials));
    }

    private static HostileStep Proves(string method) => new()
    {
        Contributes = [ContextElements.Subject, ContextElements.Claims],
        AuthenticationMethods = [method],
        Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
        {
            Subject = new Subject
            {
                Issuer = "https://login.microsoftonline.com/tenant/v2.0",
                SubjectId = "entra-subject-id",
                AuthenticatedAt = DateTimeOffset.UtcNow,
            },
            Claims =
            [
                new Claim { Type = "role", Value = "admin", Source = ClaimSources.External, Issuer = "https://login.microsoftonline.com/tenant/v2.0" },
                new Claim { Type = "department", Value = "platform", Source = ClaimSources.Local },
            ],
        }),
    };

    [Fact]
    public async Task ForeignSubjectBecomesOurTokenCarryingOnlyWhatWeVouchFor()
    {
        var key = new FixedKey();
        var steps = new NamedSteps()
            .Add("ForeignToken", Proves("pwd"))
            .Add("SecondFactor", new HostileStep
            {
                Requires = [ContextElements.Subject],
                Contributes = [],
                AuthenticationMethods = ["otp"],
                Behaviour = _ => new StepOutcome.Contributed(new ContextContribution()),
            })
            .Add("ResolvePrincipal", new HostileStep
            {
                Requires = [ContextElements.Subject],
                Contributes = [ContextElements.Principal],
                Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
                {
                    Principal = new Principal { Id = PrincipalId, TenantId = TenantId },
                }),
            })
            .Add("Authorize", new HostileStep
            {
                Requires = [ContextElements.Principal],
                Contributes = [ContextElements.Decision],
                Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
                {
                    Decision = new Decision { Permitted = true, Reason = "account active" },
                }),
            });

        var flow = new AuthenticationFlow
        {
            Name = "EntraLogin",
            Steps = FlowSteps.Named("ForeignToken", "SecondFactor", "ResolvePrincipal", "Authorize"),
            RequiredPermissions = [],
            Audience = "reference-api",
            MinimumAcr = TestAssuranceLevels.MultiFactor.AcrValue,
            ExecutionLifetime = TimeSpan.FromMinutes(5),
            SecretManagerName = "TestSecrets",
            SecretKeyName = "TEST_EXECUTION_KEY",
        };

        var runner = new AuthenticationRunner(
            TestAssuranceLevels.Policy(),
            new JwtTokenIssuer(
                new JwtTokenIssuerConfiguration { Issuer = "https://fdw.test", Lifetime = TimeSpan.FromMinutes(15) },
                key),
            new Mock<ISessionStore>(MockBehavior.Strict).Object,
            new InMemoryExecutionStore(),
            new StaticFlowProvider(flow),
            steps.Lookup,
            new Mock<ITenantProvider>().Object,
            new RecordingSignIns());

        var result = await runner.Run(flow, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var issued = ((FlowResult.Completed)result.Value!).Token;

        var validated = await new JsonWebTokenHandler().ValidateTokenAsync(issued.AccessToken,
            new TokenValidationParameters
            {
                ValidIssuer = "https://fdw.test",
                ValidAudience = "reference-api",
                IssuerSigningKey = key.Key,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ClockSkew = TimeSpan.FromSeconds(30),
            });

        validated.IsValid.ShouldBeTrue();

        var claims = validated.ClaimsIdentity.Claims.ToLookup(c => c.Type, c => c.Value);
        claims[ClaimDefinitions.sub.Name].ShouldContain(PrincipalId.ToString());

        // asserted through the definition, not a literal: a token minting one name while the
        // session-context builder reads another is how a request reaches the database with no
        // tenant scoping, and a literal here would let that pass
        claims[ClaimDefinitions.tenantId.Name].ShouldContain(TenantId.ToString());
        claims["acr"].ShouldContain(TestAssuranceLevels.MultiFactor.AcrValue);
        claims["amr"].ShouldBe(["pwd", "otp"], ignoreOrder: true);

        // the department is ours to assert; the role came from Entra and is not
        claims["department"].ShouldContain("platform");
        claims["role"].ShouldBeEmpty();
    }

    [Fact]
    public async Task TokenExpiresNoLaterThanTheEarliestDeclaredClaimsEnd()
    {
        // FDW-862: two steps gather claims that stop being true in 10 and 5 minutes, and the issuer's
        // lifetime is 15. A third step reports an end even earlier without declaring it contributes
        // claims, and the runner must ignore it (I1).
        var fiveMinutes = DateTimeOffset.UtcNow.AddMinutes(5);
        var steps = new NamedSteps()
            .Add("ForeignToken", Proves("pwd"))
            .Add("BakeLater", new HostileStep
            {
                Contributes = [ContextElements.Claims],
                Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
                {
                    Claims = [new Claim { Type = "perm", Value = "a:read", Source = ClaimSources.Local }],
                    ClaimsValidUntil = DateTimeOffset.UtcNow.AddMinutes(10),
                }),
            })
            .Add("BakeSooner", new HostileStep
            {
                Contributes = [ContextElements.Claims],
                Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
                {
                    Claims = [new Claim { Type = "perm", Value = "b:read", Source = ClaimSources.Local }],
                    ClaimsValidUntil = fiveMinutes,
                }),
            })
            .Add("Undeclared", new HostileStep
            {
                Contributes = [],
                Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
                {
                    ClaimsValidUntil = DateTimeOffset.UtcNow.AddMinutes(1),
                }),
            })
            .Add("ResolvePrincipal", new HostileStep
            {
                Requires = [ContextElements.Subject],
                Contributes = [ContextElements.Principal],
                Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
                {
                    Principal = new Principal { Id = PrincipalId, TenantId = TenantId },
                }),
            })
            .Add("Authorize", new HostileStep
            {
                Requires = [ContextElements.Principal],
                Contributes = [ContextElements.Decision],
                Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
                {
                    Decision = new Decision { Permitted = true, Reason = "account active" },
                }),
            });

        var flow = new AuthenticationFlow
        {
            Name = "CappedLogin",
            Steps = FlowSteps.Named("ForeignToken", "BakeLater", "BakeSooner", "Undeclared", "ResolvePrincipal", "Authorize"),
            RequiredPermissions = [],
            Audience = "reference-api",
            ExecutionLifetime = TimeSpan.FromMinutes(5),
            SecretManagerName = "TestSecrets",
            SecretKeyName = "TEST_EXECUTION_KEY",
        };

        var runner = new AuthenticationRunner(
            TestAssuranceLevels.Policy(),
            new JwtTokenIssuer(
                new JwtTokenIssuerConfiguration { Issuer = "https://fdw.test", Lifetime = TimeSpan.FromMinutes(15) },
                new FixedKey()),
            new Mock<ISessionStore>(MockBehavior.Strict).Object,
            new InMemoryExecutionStore(),
            new StaticFlowProvider(flow),
            steps.Lookup,
            new Mock<ITenantProvider>().Object,
            new RecordingSignIns());

        var result = await runner.Run(flow, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        ((FlowResult.Completed)result.Value!).Token.ExpiresAt.ShouldBe(fiveMinutes);
    }

    [Fact]
    public async Task IssuerRefusesClaimsThatHaveAlreadyStoppedBeingTrue()
    {
        var issuer = new JwtTokenIssuer(
            new JwtTokenIssuerConfiguration { Issuer = "https://fdw.test", Lifetime = TimeSpan.FromMinutes(15) },
            new FixedKey());

        var issued = await issuer.Issue(new IssuanceRequest
        {
            PrincipalId = PrincipalId,
            TenantId = TenantId,
            Audience = "reference-api",
            Scopes = [],
            AuthenticationMethods = ["pwd"],
            ValidUntil = DateTimeOffset.UtcNow.AddSeconds(-1),
        }, TestContext.Current.CancellationToken);

        issued.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task SingleFactorCannotSatisfyAFlowDemandingTwo()
    {
        var steps = new NamedSteps()
            .Add("ForeignToken", Proves("pwd"))
            .Add("ResolvePrincipal", new HostileStep
            {
                Requires = [ContextElements.Subject],
                Contributes = [ContextElements.Principal],
                Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
                {
                    Principal = new Principal { Id = PrincipalId, TenantId = TenantId },
                }),
            })
            .Add("Authorize", new HostileStep
            {
                Requires = [ContextElements.Principal],
                Contributes = [ContextElements.Decision],
                Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
                {
                    Decision = new Decision { Permitted = true, Reason = "account active" },
                }),
            });

        var flow = new AuthenticationFlow
        {
            Name = "EntraLogin",
            Steps = FlowSteps.Named("ForeignToken", "ResolvePrincipal", "Authorize"),
            RequiredPermissions = [],
            Audience = "reference-api",
            MinimumAcr = TestAssuranceLevels.MultiFactor.AcrValue,
            ExecutionLifetime = TimeSpan.FromMinutes(5),
            SecretManagerName = "TestSecrets",
            SecretKeyName = "TEST_EXECUTION_KEY",
        };

        var issuer = new RecordingIssuer();
        var runner = new AuthenticationRunner(
            TestAssuranceLevels.Policy(), issuer, new Mock<ISessionStore>(MockBehavior.Strict).Object, new InMemoryExecutionStore(),
            new StaticFlowProvider(flow), steps.Lookup, new Mock<ITenantProvider>().Object, new RecordingSignIns());

        var result = await runner.Run(flow, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        issuer.IssueCount.ShouldBe(0);
    }
}
