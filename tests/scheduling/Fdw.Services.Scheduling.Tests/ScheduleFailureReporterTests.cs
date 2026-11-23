using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Operations.Thresholds;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Tests;

/// <summary>
/// The failed fires of a schedule are judged by the thresholds of the schedule and of its tenant (FDW-6144), each schedule as itself: a tenant-wide rule pauses exactly the schedule whose fires failed, one
/// schedule's success never ends another's count, and a schedule that is not the tenant's problem is left running.
/// </summary>
public sealed class ScheduleFailureReporterTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Nightly = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Hourly = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private sealed class Pauser : ISchedulePauser
    {
        public List<(Guid Schedule, string Kind, string Reason)> Paused { get; } = [];

        public Task<IGenericResult> Pause(EscalationFiring firing, CancellationToken cancellationToken = default)
        {
            Paused.Add((firing.Subject.SubjectId, firing.Subject.Kind, firing.Reason));
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }
    }

    private sealed class Source(IReadOnlyList<IThresholdDefinition> schedule, IReadOnlyList<IThresholdDefinition> server, bool fails = false) : IThresholdSource
    {
        public Task<IGenericResult<IReadOnlyList<IThresholdDefinition>>> For(ThresholdSubject subject, CancellationToken cancellationToken = default) =>
            Task.FromResult<IGenericResult<IReadOnlyList<IThresholdDefinition>>>(fails
                ? GenericResult<IReadOnlyList<IThresholdDefinition>>.Failure(new GenericMessage("the thresholds are unreadable"))
                : GenericResult<IReadOnlyList<IThresholdDefinition>>.Success(string.Equals(subject.Kind, "Server", StringComparison.Ordinal) ? server : schedule));
    }

    private readonly Pauser _pauser = new();

    // One tenant-wide threshold: pause a schedule at two failed fires in a row. The number is the rule's, stated here as the seed states it.
    private static Threshold<ServerScheduleFailsThresholdType> TenantWide(long atLeast) => new(
        new ServerScheduleFailsThresholdType(), "ScheduleFailureLimit", true, true, new Dictionary<string, object?>(),
        [new ThresholdRule(new FilterCondition { PropertyName = "consecutiveFailures", Operator = FilterOperators.ByName("GreaterThanOrEqual"), Value = atLeast }, "PauseSchedule", null, [])]);

    private ScheduleFailureReporter Sut(IReadOnlyList<IThresholdDefinition> schedule, IReadOnlyList<IThresholdDefinition> server, bool sourceFails = false)
    {
        var source = new Source(schedule, server, sourceFails);
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupProperty(a => a.Current);
        var services = new ServiceCollection();
        services.AddSingleton(accessor.Object);
        services.AddSingleton<IThresholdSource>(source);
        services.AddSingleton<ISchedulePauser>(_pauser);
        services.AddSingleton<ThresholdEntryTracker>();
        services.AddSingleton<IThresholdEvaluator>(provider => new ThresholdEvaluator(source, provider, provider.GetRequiredService<ThresholdEntryTracker>()));
        var provider = services.BuildServiceProvider();
        return new ScheduleFailureReporter(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<ScheduleFailureReporter>.Instance);
    }

    private static Task<IGenericResult> Report(ScheduleFailureReporter sut, Guid schedule, string name, long failures) =>
        sut.Report(schedule, name, Tenant, failures, TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task ATenantWideRulePausesExactlyTheScheduleWhoseFiresFailedAndNoOther()
    {
        var sut = Sut([], [TenantWide(2)]);

        (await Report(sut, Nightly, "Nightly", 1)).IsSuccess.ShouldBeTrue();
        (await Report(sut, Hourly, "Hourly", 1)).IsSuccess.ShouldBeTrue();
        _pauser.Paused.ShouldBeEmpty();

        // Hourly succeeds in between: that is Hourly's count starting again, and it must not end Nightly's.
        (await Report(sut, Hourly, "Hourly", 0)).IsSuccess.ShouldBeTrue();
        (await Report(sut, Nightly, "Nightly", 2)).IsSuccess.ShouldBeTrue();

        var paused = _pauser.Paused.ShouldHaveSingleItem();
        paused.Schedule.ShouldBe(Nightly);
        paused.Kind.ShouldBe("Schedule");
        paused.Reason.ShouldContain("ScheduleFailureLimit");
        paused.Reason.ShouldContain("consecutiveFailures = 2");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task AnotherScheduleReachingTheNumberAfterwardsIsPausedToo_AndTheFirstIsNotPausedAgain()
    {
        var sut = Sut([], [TenantWide(2)]);
        await Report(sut, Nightly, "Nightly", 1);
        await Report(sut, Nightly, "Nightly", 2);

        await Report(sut, Hourly, "Hourly", 1);
        await Report(sut, Nightly, "Nightly", 3);
        await Report(sut, Hourly, "Hourly", 2);

        _pauser.Paused.Select(paused => paused.Schedule).ShouldBe([Nightly, Hourly]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task ASchedulePausedAndEnabledAgainIsPausedAgainWhenItsCountReachesTheNumberAgain()
    {
        var sut = Sut([], [TenantWide(2)]);
        await Report(sut, Nightly, "Nightly", 1);
        await Report(sut, Nightly, "Nightly", 2);

        // A person enabled it: the count starts again and its first fire reports 0 or 1, which ends the rule's holding.
        await Report(sut, Nightly, "Nightly", 1);
        await Report(sut, Nightly, "Nightly", 2);

        _pauser.Paused.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task AThresholdOfAnotherTypeAttachedToTheTenantIsNotJudgedAsFailedFires()
    {
        var waits = new Threshold<ServerWaitsThresholdType>(
            new ServerWaitsThresholdType(), "Waits", true, false, new Dictionary<string, object?>(),
            [new ThresholdRule(new FilterCondition { PropertyName = "waitingRuns", Operator = FilterOperators.ByName("GreaterThanOrEqual"), Value = 1L }, "PauseSchedule", null, [])]);
        var sut = Sut([], [waits]);

        (await Report(sut, Nightly, "Nightly", 5)).IsSuccess.ShouldBeTrue();

        _pauser.Paused.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task AScheduleWithNoTenantCannotBeJudgedAndSaysSo()
    {
        var sut = Sut([], [TenantWide(2)]);

        var result = await sut.Report(Nightly, "Nightly", null, 5, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("SCHEDULEFAILS-20103");
        _pauser.Paused.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task ThresholdsThatCannotBeReadFailTheReportAndNothingIsPaused()
    {
        var sut = Sut([], [TenantWide(2)], sourceFails: true);

        var result = await Report(sut, Nightly, "Nightly", 5);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("SCHEDULEFAILS-70108");
        _pauser.Paused.ShouldBeEmpty();
    }
}
