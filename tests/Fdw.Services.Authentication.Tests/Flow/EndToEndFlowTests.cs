using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Execution;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.TokenManagers;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
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
        Contributes = [ContextElement.Subject, ContextElement.Claims],
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
                new Claim { Type = "role", Value = "admin", Source = ClaimSource.External, Issuer = "https://login.microsoftonline.com/tenant/v2.0" },
                new Claim { Type = "department", Value = "platform", Source = ClaimSource.Local },
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
                Requires = [ContextElement.Subject],
                Contributes = [],
                AuthenticationMethods = ["otp"],
                Behaviour = _ => new StepOutcome.Contributed(new ContextContribution()),
            })
            .Add("ResolvePrincipal", new HostileStep
            {
                Requires = [ContextElement.Subject],
                Contributes = [ContextElement.Principal],
                Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
                {
                    Principal = new Principal { Id = PrincipalId, TenantId = TenantId },
                }),
            })
            .Add("Authorize", new HostileStep
            {
                Requires = [ContextElement.Principal],
                Contributes = [ContextElement.Decision],
                Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
                {
                    Decision = new Decision { Permitted = true, Reason = "account active" },
                }),
            });

        var flow = new AuthenticationFlow
        {
            Name = "EntraLogin",
            Steps = ["ForeignToken", "SecondFactor", "ResolvePrincipal", "Authorize"],
            Audience = "reference-api",
            MinimumAcr = StandardAcrPolicy.MultiFactor,
            ExecutionLifetime = TimeSpan.FromMinutes(5),
        };

        var runner = new AuthenticationRunner(
            new StandardAcrPolicy(),
            new JwtTokenIssuer(
                new JwtTokenIssuerConfiguration { Issuer = "https://fdw.test", Lifetime = TimeSpan.FromMinutes(15) },
                key),
            new InMemoryExecutionStore(),
            new StaticFlowProvider(flow),
            steps.Lookup);

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
        claims["acr"].ShouldContain(StandardAcrPolicy.MultiFactor);
        claims["amr"].ShouldBe(["pwd", "otp"], ignoreOrder: true);

        // the department is ours to assert; the role came from Entra and is not
        claims["department"].ShouldContain("platform");
        claims["role"].ShouldBeEmpty();
    }

    [Fact]
    public async Task SingleFactorCannotSatisfyAFlowDemandingTwo()
    {
        var steps = new NamedSteps()
            .Add("ForeignToken", Proves("pwd"))
            .Add("ResolvePrincipal", new HostileStep
            {
                Requires = [ContextElement.Subject],
                Contributes = [ContextElement.Principal],
                Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
                {
                    Principal = new Principal { Id = PrincipalId, TenantId = TenantId },
                }),
            })
            .Add("Authorize", new HostileStep
            {
                Requires = [ContextElement.Principal],
                Contributes = [ContextElement.Decision],
                Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
                {
                    Decision = new Decision { Permitted = true, Reason = "account active" },
                }),
            });

        var flow = new AuthenticationFlow
        {
            Name = "EntraLogin",
            Steps = ["ForeignToken", "ResolvePrincipal", "Authorize"],
            Audience = "reference-api",
            MinimumAcr = StandardAcrPolicy.MultiFactor,
            ExecutionLifetime = TimeSpan.FromMinutes(5),
        };

        var issuer = new RecordingIssuer();
        var runner = new AuthenticationRunner(
            new StandardAcrPolicy(), issuer, new InMemoryExecutionStore(),
            new StaticFlowProvider(flow), steps.Lookup);

        var result = await runner.Run(flow, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        issuer.IssueCount.ShouldBe(0);
    }
}
