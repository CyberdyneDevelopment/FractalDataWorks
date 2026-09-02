using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Flow;

/// <summary>
/// Each test is an attack. An invariant nobody tried to violate is an assertion, not a guarantee.
/// </summary>
public sealed class AuthenticationRunnerInvariantTests
{
    private static readonly Guid PrincipalId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static ContextContribution Proves() => new()
    {
        Subject = new Subject { Issuer = "test", SubjectId = "abc", AuthenticatedAt = DateTimeOffset.UtcNow },
    };

    private static ContextContribution Resolves() => new()
    {
        Principal = new Principal { Id = PrincipalId, TenantId = TenantId },
    };

    private static ContextContribution Permits() => new()
    {
        Decision = new Decision { Permitted = true, Reason = "test" },
    };

    private static (AuthenticationRunner Runner, RecordingIssuer Issuer, InMemoryExecutions Store) Build(
        NamedSteps steps, AuthenticationFlow? flowForResume = null)
    {
        var issuer = new RecordingIssuer();
        var store = new InMemoryExecutions();
        var flows = new StaticFlowProvider(flowForResume ?? Flow());
        return (new AuthenticationRunner(new CountingAcrPolicy(), issuer, store, flows, steps.Lookup),
                issuer, store);
    }

    private static AuthenticationFlow Flow(params string[] steps) => new()
    {
        Name = "test-flow",
        Steps = steps,
        Audience = "test-audience",
        ExecutionLifetime = TimeSpan.FromMinutes(5),
    };

    /// <summary>I1 — a step cannot affect what it did not declare.</summary>
    [Fact]
    public async Task StepContributingAnUndeclaredElementHasItDiscarded()
    {
        // declares only Subject, but tries to hand back a Principal and a Permit as well
        var overreaching = new HostileStep
        {
            Contributes = [ContextElements.Subject],
            AuthenticationMethods = ["pwd"],
            Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
            {
                Subject = Proves().Subject,
                Principal = Resolves().Principal,
                Decision = Permits().Decision,
            }),
        };

        var (runner, issuer, _) = Build(new NamedSteps().Add("overreaching", overreaching));

        var result = await runner.Run(Flow("overreaching"), TestContext.Current.CancellationToken);

        // the smuggled Principal and Decision never landed, so the terminal check refuses
        result.IsSuccess.ShouldBeFalse();
        issuer.IssueCount.ShouldBe(0);
    }

    /// <summary>I2 — a step cannot claim a factor it never checked.</summary>
    [Fact]
    public async Task StepCannotRaiseItsOwnAssuranceLevel()
    {
        // one step, one declared method — it has no way to say it achieved more
        var single = new HostileStep
        {
            Contributes = [ContextElements.Subject, ContextElements.Principal, ContextElements.Decision],
            AuthenticationMethods = ["pwd"],
            Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
            {
                Subject = Proves().Subject,
                Principal = Resolves().Principal,
                Decision = Permits().Decision,
            }),
        };

        var (runner, issuer, _) = Build(new NamedSteps().Add("single", single));

        var flow = Flow("single") with { MinimumAcr = "strong" };
        var result = await runner.Run(flow, TestContext.Current.CancellationToken);

        // one method is "weak"; the flow demands "strong" and nothing the step did can change that
        result.IsSuccess.ShouldBeFalse();
        issuer.IssueCount.ShouldBe(0);
    }

    /// <summary>I3 — declining to act is not a way to skip a requirement.</summary>
    [Fact]
    public async Task StepReturningNotApplicableStillLeavesItsRequirementUnmet()
    {
        var optedOut = new HostileStep
        {
            Contributes = [ContextElements.Principal],
            Behaviour = _ => new StepOutcome.NotApplicable("declining"),
        };

        var needsPrincipal = new HostileStep
        {
            Requires = [ContextElements.Principal],
            Contributes = [ContextElements.Decision],
            Behaviour = _ => new StepOutcome.Contributed(Permits()),
        };

        var (runner, issuer, _) = Build(new NamedSteps()
            .Add("optedOut", optedOut)
            .Add("needsPrincipal", needsPrincipal));

        var result = await runner.Run(Flow("optedOut", "needsPrincipal"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        issuer.IssueCount.ShouldBe(0);
    }

    /// <summary>I4 — a flow cannot be configured to skip authorization.</summary>
    [Fact]
    public async Task FlowWithNoDecisionIssuesNothing()
    {
        var authenticates = new HostileStep
        {
            Contributes = [ContextElements.Subject, ContextElements.Principal],
            AuthenticationMethods = ["pwd"],
            Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
            {
                Subject = Proves().Subject,
                Principal = Resolves().Principal,
            }),
        };

        var (runner, issuer, _) = Build(new NamedSteps().Add("authenticates", authenticates));

        // a subject and a principal, and no step that decides anything
        var result = await runner.Run(Flow("authenticates"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        issuer.IssueCount.ShouldBe(0);
    }

    /// <summary>I5 — a resume token works exactly once.</summary>
    [Fact]
    public async Task ReplayedResumeTokenIsRejected()
    {
        var suspends = new HostileStep
        {
            Contributes = [ContextElements.Subject],
            AuthenticationMethods = ["pwd"],
            Behaviour = _ => new StepOutcome.Challenge(new Uri("https://idp.test/authorize"), "unused"),
        };

        var flow = Flow("suspends");
        var (runner, _, _) = Build(new NamedSteps().Add("suspends", suspends), flow);

        var first = await runner.Run(flow, TestContext.Current.CancellationToken);
        first.IsSuccess.ShouldBeTrue();
        var token = ((FlowResult.Suspended)first.Value!).ResumeToken;

        var resumedOnce = await runner.Resume(token, TestContext.Current.CancellationToken);
        var resumedTwice = await runner.Resume(token, TestContext.Current.CancellationToken);

        // whatever the first resume did, the second must find nothing to consume
        resumedTwice.IsSuccess.ShouldBeFalse();
        resumedOnce.ShouldNotBeNull();
    }

    /// <summary>I6 — a federated provider naming a role does not grant one.</summary>
    [Fact]
    public async Task ExternalClaimsDoNotReachTheIssuedToken()
    {
        var federated = new HostileStep
        {
            Contributes = [ContextElements.Subject, ContextElements.Principal, ContextElements.Decision, ContextElements.Claims],
            AuthenticationMethods = ["pwd"],
            Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
            {
                Subject = Proves().Subject,
                Principal = Resolves().Principal,
                Decision = Permits().Decision,
                Claims =
                [
                    new Claim { Type = "role", Value = "admin", Source = ClaimSources.External, Issuer = "https://idp.test" },
                    new Claim { Type = "department", Value = "finance", Source = ClaimSources.Local },
                ],
            }),
        };

        var (runner, issuer, _) = Build(new NamedSteps().Add("federated", federated));

        var result = await runner.Run(Flow("federated"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        issuer.LastRequest.ShouldNotBeNull();
        issuer.LastRequest!.Claims.ShouldNotContainKey("role");
        issuer.LastRequest!.Claims.ShouldContainKey("department");
    }

    /// <summary>The happy path, so the invariants are shown to permit as well as refuse.</summary>
    [Fact]
    public async Task CompleteFlowIssuesWithTheMethodsActuallyProved()
    {
        var proves = new HostileStep
        {
            Contributes = [ContextElements.Subject],
            AuthenticationMethods = ["pwd"],
            Behaviour = _ => new StepOutcome.Contributed(Proves()),
        };

        var second = new HostileStep
        {
            Requires = [ContextElements.Subject],
            Contributes = [ContextElements.Principal],
            AuthenticationMethods = ["otp"],
            Behaviour = _ => new StepOutcome.Contributed(Resolves()),
        };

        var authorizes = new HostileStep
        {
            Requires = [ContextElements.Principal],
            Contributes = [ContextElements.Decision],
            Behaviour = _ => new StepOutcome.Contributed(Permits()),
        };

        var (runner, issuer, _) = Build(new NamedSteps()
            .Add("proves", proves).Add("second", second).Add("authorizes", authorizes));

        var flow = Flow("proves", "second", "authorizes") with { MinimumAcr = "strong" };
        var result = await runner.Run(flow, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        issuer.LastRequest!.AuthenticationMethods.ShouldBe(["pwd", "otp"]);
        issuer.LastRequest!.Acr.ShouldBe("strong");
        issuer.LastRequest!.Audience.ShouldBe("test-audience");
    }
}
