using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Multitenancy.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Flow;

/// <summary>
/// Tests that the methods a sign-in proved are its own when two sign-ins run through one shared step at once (FDW-4697): the step keeps nothing per login, and the methods its
/// authority permits travel with the contribution of the execution that read them.
/// </summary>
[Trait("Category", "Security")]
public sealed class AssertedMethodsConcurrencyTests
{
    private static readonly Guid PrincipalId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid TenantId = Guid.Parse("22222222-2222-4222-8222-222222222222");

    // One instance for the process, as in production. It declares no methods of its own; each execution answers with the methods its authority permits, the first call with
    // password only and the second with password and a one-time code. The first call waits until the second has finished, so the two executions overlap.
    private sealed class SharedStep : IAuthenticationStep
    {
        private int _calls;
        private readonly TaskCompletionSource _firstMayFinish = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task FirstStarted => _firstStarted.Task;

        public string Name => "Shared";

        public IReadOnlyList<IContextElement> Requires => [];

        public IReadOnlyList<IContextElement> Contributes => [ContextElements.Subject, ContextElements.Principal, ContextElements.Decision];

        public IReadOnlyList<string> AuthenticationMethods => [];

        public async Task<IGenericResult<StepOutcome>> Execute(AuthenticationContext context, ITenantProvider tenants, CancellationToken cancellationToken = default)
        {
            var call = Interlocked.Increment(ref _calls);
            if (call == 1)
            {
                _firstStarted.SetResult();
                await _firstMayFinish.Task.ConfigureAwait(false);
            }

            string[] methods = call == 1 ? ["pwd"] : ["pwd", "otp"];
            var contribution = new ContextContribution
            {
                Subject = new Subject { Issuer = "test", SubjectId = "login-" + call, AuthenticatedAt = DateTimeOffset.UtcNow },
                Principal = new Principal { Id = PrincipalId, TenantId = TenantId },
                Decision = new Decision { Permitted = true, Reason = "test" },
                AssertableMethods = methods,
            };

            if (call == 2)
                _firstMayFinish.SetResult();

            return GenericResult<StepOutcome>.Success(new StepOutcome.Contributed(contribution));
        }
    }

    private static AuthenticationFlow Flow() => new()
    {
        Name = "test-flow",
        Steps = FlowSteps.Named("shared"),
        RequiredPermissions = [],
        Audience = "test-audience",
        ExecutionLifetime = TimeSpan.FromMinutes(5),
        SecretManagerName = "TestSecrets",
        SecretKeyName = "TEST_EXECUTION_KEY",
    };

    private static (AuthenticationRunner Runner, RecordingIssuer Issuer) Runner(SharedStep step)
    {
        var issuer = new RecordingIssuer();
        var flows = new StaticFlowProvider(Flow());
        return (new AuthenticationRunner(
                new CountingAcrPolicy(), issuer, new Mock<ISessionStore>(MockBehavior.Strict).Object, new InMemoryExecutions(), flows,
                new NamedSteps().Add("shared", step).Lookup, new Mock<ITenantProvider>().Object, new RecordingSignIns()),
            issuer);
    }

    [Fact]
    public async Task TwoSignInsOverlappingOnOneSharedStepEachRecordTheirOwnMethods()
    {
        var step = new SharedStep();
        var (firstRunner, firstIssuer) = Runner(step);
        var (secondRunner, secondIssuer) = Runner(step);

        var first = firstRunner.Run(Flow(), TestContext.Current.CancellationToken);
        await step.FirstStarted;
        var second = secondRunner.Run(Flow(), TestContext.Current.CancellationToken);

        (await first).IsSuccess.ShouldBeTrue();
        (await second).IsSuccess.ShouldBeTrue();
        firstIssuer.LastRequest!.AuthenticationMethods.ShouldBe(["pwd"]);
        secondIssuer.LastRequest!.AuthenticationMethods.ShouldBe(["pwd", "otp"]);
    }

    [Fact]
    public async Task AStepThatDeclaresNothingAndContributesNothingRecordsNothing()
    {
        var silent = new HostileStep
        {
            Contributes = [ContextElements.Subject, ContextElements.Principal, ContextElements.Decision],
            Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
            {
                Subject = new Subject { Issuer = "test", SubjectId = "abc", AuthenticatedAt = DateTimeOffset.UtcNow },
                Principal = new Principal { Id = PrincipalId, TenantId = TenantId },
                Decision = new Decision { Permitted = true, Reason = "test" },
            }),
        };
        var issuer = new RecordingIssuer();
        var runner = new AuthenticationRunner(
            new CountingAcrPolicy(), issuer, new Mock<ISessionStore>(MockBehavior.Strict).Object, new InMemoryExecutions(), new StaticFlowProvider(Flow()),
            new NamedSteps().Add("shared", silent).Lookup, new Mock<ITenantProvider>().Object, new RecordingSignIns());

        await runner.Run(Flow(), TestContext.Current.CancellationToken);

        issuer.LastRequest!.AuthenticationMethods.ShouldBeEmpty();
    }

    [Fact]
    public async Task ObservedMethodsAreKeptOnlyWhereTheContributionPermitsThem()
    {
        var relays = new HostileStep
        {
            Contributes = [ContextElements.Subject, ContextElements.Principal, ContextElements.Decision],
            Behaviour = _ => new StepOutcome.Contributed(new ContextContribution
            {
                Subject = new Subject { Issuer = "test", SubjectId = "abc", AuthenticatedAt = DateTimeOffset.UtcNow },
                Principal = new Principal { Id = PrincipalId, TenantId = TenantId },
                Decision = new Decision { Permitted = true, Reason = "test" },
                AssertableMethods = ["pwd"],
                ObservedMethods = ["pwd", "hwk"],
            }),
        };
        var issuer = new RecordingIssuer();
        var runner = new AuthenticationRunner(
            new CountingAcrPolicy(), issuer, new Mock<ISessionStore>(MockBehavior.Strict).Object, new InMemoryExecutions(), new StaticFlowProvider(Flow()),
            new NamedSteps().Add("shared", relays).Lookup, new Mock<ITenantProvider>().Object, new RecordingSignIns());

        await runner.Run(Flow(), TestContext.Current.CancellationToken);

        issuer.LastRequest!.AuthenticationMethods.ShouldBe(["pwd"]);
    }
}
