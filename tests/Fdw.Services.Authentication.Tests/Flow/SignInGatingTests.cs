using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Flow;

/// <summary>
/// Per-flow sign-in gating (FDW-910): a configured step is handed its flow's settings, a refusal
/// stands against every later step, and a flow can be switched off without deleting it.
/// </summary>
public class SignInGatingTests
{
    private static readonly Guid PrincipalId = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid TenantId = new("22222222-0000-0000-0000-000000000002");

    // ---------------------------------------------------------------- the runner

    [Fact]
    [Trait("Category", "Security")]
    public async Task ARefusalStandsAgainstALaterStepThatPermits()
    {
        var steps = new NamedSteps()
            .Add("resolves", Resolves())
            .Add("gate", new PermissionGate())
            .Add("authorizes", Permits());
        var (runner, issuer) = Build(steps);

        var result = await runner.Run(
            Flow(Step("resolves"), Step("gate", Gate("signin:password")), Step("authorizes")),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        issuer.IssueCount.ShouldBe(0);
    }

    [Fact]
    public async Task AGatePassesWhenThePermissionWasBaked()
    {
        var steps = new NamedSteps()
            .Add("resolves", Resolves("signin:password"))
            .Add("gate", new PermissionGate())
            .Add("authorizes", Permits());
        var (runner, issuer) = Build(steps);

        var result = await runner.Run(
            Flow(Step("resolves"), Step("gate", Gate("signin:password")), Step("authorizes")),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        issuer.IssueCount.ShouldBe(1);
    }

    [Fact]
    public async Task AFlowWithoutAGateAdmitsEveryActiveUser()
    {
        var steps = new NamedSteps().Add("resolves", Resolves()).Add("authorizes", Permits());
        var (runner, issuer) = Build(steps);

        var result = await runner.Run(Flow(Step("resolves"), Step("authorizes")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        issuer.IssueCount.ShouldBe(1);
    }

    [Fact]
    public async Task AConfiguredStepIsHandedThisFlowsSettings()
    {
        var gate = new PermissionGate();
        var steps = new NamedSteps().Add("resolves", Resolves("x:one")).Add("gate", gate).Add("authorizes", Permits());
        var (runner, _) = Build(steps);

        await runner.Run(Flow(Step("resolves"), Step("gate", Gate("x:one")), Step("authorizes")), TestContext.Current.CancellationToken);
        await runner.Run(Flow(Step("resolves"), Step("gate", Gate("x:two")), Step("authorizes")), TestContext.Current.CancellationToken);

        gate.Seen.ShouldBe([Gate("x:one"), Gate("x:two")]);
    }

    // ---------------------------------------------------------------- the provider

    [Fact]
    public async Task AMissingFlowFailsAsNotFound()
    {
        var provider = Provider(Row("PasswordLogin", isEnabled: true, ("resolves", null), ("authorizes", null)));

        var result = await provider.Get("NoSuchFlow", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CodeChain.ShouldContain(AuthenticationResultCodes.ByName("FlowNotFound"));
    }

    [Fact]
    public async Task ASwitchedOffFlowIsRefusedAsDisabledAndIsNotOffered()
    {
        var provider = Provider(
            Row("PasswordLogin", isEnabled: false, ("resolves", null), ("authorizes", null)),
            Row("TenantSwitch", isEnabled: true, ("resolves", null), ("authorizes", null)));

        var selected = await provider.Get("PasswordLogin", TestContext.Current.CancellationToken);
        var offered = await provider.Get(TestContext.Current.CancellationToken);

        selected.IsFailure.ShouldBeTrue();
        selected.CodeChain.ShouldContain(AuthenticationResultCodes.ByName("FlowDisabled"));
        offered.Value!.Select(f => f.Name).ShouldBe(["TenantSwitch"]);
    }

    [Fact]
    public async Task MalformedStepSettingsMakeTheFlowKnownInvalid()
    {
        var provider = Provider(Row("PasswordLogin", isEnabled: true,
            ("resolves", null), ("gate", "{ not json"), ("authorizes", null)));

        var result = await provider.Get("PasswordLogin", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CodeChain.ShouldNotContain(AuthenticationResultCodes.ByName("FlowNotFound"));
        result.CodeChain.ShouldNotContain(AuthenticationResultCodes.ByName("FlowDisabled"));
    }

    [Fact]
    public async Task AGateOrderedBeforeThePermissionsExistMakesTheFlowKnownInvalid()
    {
        var provider = Provider(Row("PasswordLogin", isEnabled: true,
            ("gate", Gate("signin:password")), ("resolves", null), ("authorizes", null)));

        var result = await provider.Get("PasswordLogin", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task AGatedFlowStatesWhatItRequires()
    {
        var provider = Provider(Row("PasswordLogin", isEnabled: true,
            ("resolves", null), ("gate", Gate("signin:password")), ("authorizes", null)));

        var result = await provider.Get("PasswordLogin", TestContext.Current.CancellationToken);

        result.Value!.RequiredPermissions.ShouldBe(["signin:password"]);
        result.Value.Steps.Single(s => s.Name == "gate").Configuration.ShouldBe(Gate("signin:password"));
    }

    // ---------------------------------------------------------------- admission

    [Fact]
    public async Task AdmissionFollowsTheGateOfTheFlowsProvingTheMethod()
    {
        var gated = Provider(Row("PasswordLogin", isEnabled: true,
            ("password", null), ("gate", Gate("signin:password")), ("authorizes", null)));

        (await Admission(gated, ["signin:password"]).MayUse(PrincipalId, "pwd", TestContext.Current.CancellationToken))
            .Value.ShouldBeTrue();
        (await Admission(gated, ["datasets:read"]).MayUse(PrincipalId, "pwd", TestContext.Current.CancellationToken))
            .Value.ShouldBeFalse();
    }

    [Fact]
    public async Task AdmissionIsOpenWhenAnyProvingFlowIsUngated()
    {
        var open = Provider(Row("PasswordLogin", isEnabled: true, ("password", null), ("authorizes", null)));

        var result = await Admission(open, []).MayUse(PrincipalId, "pwd", TestContext.Current.CancellationToken);

        result.Value.ShouldBeTrue();
    }

    [Fact]
    public async Task AdmissionIsRefusedWhenEveryProvingFlowIsSwitchedOff()
    {
        var off = Provider(Row("PasswordLogin", isEnabled: false, ("password", null), ("authorizes", null)));

        var result = await Admission(off, []).MayUse(PrincipalId, "pwd", TestContext.Current.CancellationToken);

        result.Value.ShouldBeFalse();
    }

    [Fact]
    public async Task AdmissionForAnAccountNotYetCreatedReadsTheRolesPermissions()
    {
        var gated = Provider(Row("PasswordLogin", isEnabled: true,
            ("password", null), ("gate", Gate("signin:password")), ("authorizes", null)));
        var admission = Admission(gated, []);

        (await admission.MayUse(["datasets:read", "SIGNIN:PASSWORD"], "pwd", TestContext.Current.CancellationToken))
            .Value.ShouldBeTrue();
        (await admission.MayUse(["datasets:read"], "pwd", TestContext.Current.CancellationToken))
            .Value.ShouldBeFalse();
    }

    // ---------------------------------------------------------------- doubles and builders

    private static string Gate(string permission) => JsonSerializer.Serialize(new { RequiredPermission = permission });

    private static AuthenticationFlowStep Step(string name, string? configuration = null)
        => new() { Name = name, Configuration = configuration };

    private static AuthenticationFlow Flow(params AuthenticationFlowStep[] steps) => new()
    {
        Name = "gated-flow",
        Steps = steps,
        RequiredPermissions = [],
        Audience = "test-audience",
        ExecutionLifetime = TimeSpan.FromMinutes(5),
    };

    // Proves a subject, resolves the principal and bakes what it is told, in one step.
    private static HostileStep Resolves(params string[] permissions) => new()
    {
        Contributes = [ContextElements.Subject, ContextElements.Principal, ContextElements.Claims],
        AuthenticationMethods = ["pwd"],
        Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
        {
            Subject = new Subject { Issuer = "local", SubjectId = PrincipalId.ToString(), AuthenticatedAt = DateTimeOffset.UtcNow },
            Principal = new Principal { Id = PrincipalId, TenantId = TenantId },
            Claims = [.. permissions.Select(p => new Claim { Type = ClaimDefinitions.perm.Name, Value = p, Source = ClaimSources.Local })],
        }),
    };

    private static HostileStep Permits() => new()
    {
        Requires = [ContextElements.Principal],
        Contributes = [ContextElements.Decision],
        Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
        {
            Decision = new Decision { Permitted = true, Reason = "the account is active" },
        }),
    };

    private static (AuthenticationRunner Runner, RecordingIssuer Issuer) Build(NamedSteps steps)
    {
        var issuer = new RecordingIssuer();
        var runner = new AuthenticationRunner(
            new CountingAcrPolicy(), issuer, new Mock<ISessionStore>(MockBehavior.Strict).Object,
            new InMemoryExecutions(), new StaticFlowProvider(Flow()), steps.Lookup, new Mock<ITenantProvider>().Object, new RecordingSignIns());
        return (runner, issuer);
    }

    private static AuthenticationFlowImplementationConfiguration Row(
        string name, bool isEnabled, params (string Step, string? Configuration)[] steps) => new()
    {
        Name = name,
        Audience = "test-audience",
        ExecutionLifetime = "PT5M",
        IsEnabled = isEnabled,
        Steps = [.. steps.Select((s, i) => new AuthenticationFlowStepConfiguration
        {
            Name = s.Step, StepName = s.Step, StepOrder = i + 1, Configuration = s.Configuration,
        })],
    };

    private static NamedSteps KnownSteps() => new NamedSteps()
        .Add("resolves", Resolves())
        .Add("password", Resolves())
        .Add("gate", new PermissionGate())
        .Add("authorizes", Permits());

    private static AuthenticationFlowProvider Provider(params AuthenticationFlowImplementationConfiguration[] rows)
    {
        var configuration = new Mock<IAuthenticationFlowConfigurationProvider>();
        configuration.Setup(c => c.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IAuthenticationFlowImplementationConfiguration>>.Success(rows));
        return new AuthenticationFlowProvider(configuration.Object, new AuthenticationContextAccessor(), KnownSteps().Lookup);
    }

    private static SignInMethodAdmission Admission(AuthenticationFlowProvider flows, string[] held)
    {
        var tenants = new Mock<ITenantResolver>();
        tenants.Setup(t => t.TenantFor(PrincipalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Success(TenantId));
        var permissions = new Mock<IUserPermissions>();
        permissions.Setup(p => p.Get(PrincipalId.ToString(), TenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<EffectiveAuthorization>.Success(new EffectiveAuthorization(held, [], validUntil: null)));
        return new SignInMethodAdmission(
            flows, tenants.Object, permissions.Object, new AuthenticationContextAccessor(), KnownSteps().Lookup);
    }

    /// <summary>
    /// The shape of a gating step: reads <c>RequiredPermission</c> from its settings and refuses a
    /// principal whose baked permissions lack it. It only ever refuses; permitting is left to the
    /// flow's eligibility step.
    /// </summary>
    private sealed class PermissionGate : IPermissionGatedAuthenticationStep
    {
        public List<string?> Seen { get; } = [];

        public string Name => "gate";

        public IReadOnlyList<IContextElement> Requires => [ContextElements.Principal, ContextElements.Claims];

        public IReadOnlyList<IContextElement> Contributes => [ContextElements.Decision];

        public IReadOnlyList<string> AuthenticationMethods => [];

        public IGenericResult ValidateConfiguration(string? configuration)
            => RequiredPermission(configuration) is { IsSuccess: true } ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("bad settings"));

        public IGenericResult<string> RequiredPermission(string? configuration)
        {
            try
            {
                return configuration is not null
                    && JsonDocument.Parse(configuration).RootElement.TryGetProperty("RequiredPermission", out var p)
                    && p.GetString() is { Length: > 0 } name
                    ? GenericResult<string>.Success(name)
                    : GenericResult<string>.Failure(new GenericMessage("no RequiredPermission"));
            }
            catch (JsonException)
            {
                return GenericResult<string>.Failure(new GenericMessage("not JSON"));
            }
        }

        public Task<IGenericResult<StepOutcome>> Execute(
            AuthenticationContext context, ITenantProvider tenants, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("a configured step is always handed its settings");

        public Task<IGenericResult<StepOutcome>> Execute(
            AuthenticationContext context, string? configuration, ITenantProvider tenants, CancellationToken cancellationToken = default)
        {
            Seen.Add(configuration);
            var required = RequiredPermission(configuration).Value!;
            var held = context.Claims.Claims.Any(c => c.Type == ClaimDefinitions.perm.Name && c.Value == required);
            StepOutcome outcome = held
                ? new StepOutcome.Contributed(new ContextContribution())
                : new StepOutcome.Contributed(new ContextContribution
                {
                    Decision = new Decision { Permitted = false, Reason = $"lacks {required}" },
                });
            return Task.FromResult(GenericResult<StepOutcome>.Success(outcome));
        }
    }
}
