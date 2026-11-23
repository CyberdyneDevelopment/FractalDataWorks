using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Tests;

/// <summary>
/// The pause a threshold's rule brings (FDW-6144): the schedule the firing's subject is, and no other, is disabled by one targeted write that keeps when and why. A firing on anything but a schedule is a
/// coded failure and writes nothing.
/// </summary>
public sealed class SchedulePauserTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ScheduleId = Guid.Parse("01a1be2a-1344-7419-866c-a8bd65bf4a7b");
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private readonly Mock<IConfigurationGateway> _gateway = new();
    private IDataCommand? _sent;

    private ScheduleImplementationConfigurationProvider Sut(int rows)
    {
        _gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback((IDataCommand command, DataStoreTarget _, CancellationToken __) => _sent = command)
            .ReturnsAsync(GenericResult<int>.Success(rows));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(p => p.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(_gateway.Object));
        return new ScheduleImplementationConfigurationProvider(
            NullLogger<ScheduleImplementationConfigurationProvider>.Instance, gateways.Object, "ConfigurationDb", "sched", "ScheduleConfiguration",
            Mock.Of<IScheduleFailureReporter>(), new FixedClock(Now));
    }

    private static EscalationFiring Firing(string kind, Guid subjectId) => new(
        new ThresholdSubject(kind, "Nightly", subjectId, Tenant, null), "ScheduleFailureLimit", 1,
        new ThresholdRule(new FilterCondition { PropertyName = "consecutiveFailures", Operator = FilterOperators.ByName("GreaterThanOrEqual"), Value = 3 }, "PauseSchedule", null, []),
        new Dictionary<string, object?> { ["consecutiveFailures"] = 3L },
        "Rule 1 of threshold 'ScheduleFailureLimit' matched on Schedule 'Nightly': consecutiveFailures = 3");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task TheScheduleTheFiringIsAboutIsDisabledWithWhenAndWhyAndNoOtherRowIsAsked()
    {
        var firing = Firing("Schedule", ScheduleId);

        var paused = await Sut(1).Pause(firing, TestContext.Current.CancellationToken);

        paused.IsSuccess.ShouldBeTrue(paused.CurrentMessage);
        var write = _sent.ShouldBeOfType<UpdateCommand<SchedulePauseRecord>>().Data;
        write.IsEnabled.ShouldBeFalse();
        write.PausedAt.ShouldBe(Now);
        write.PausedReason.ShouldBe(firing.Reason);
        _gateway.Verify(g => g.InvalidateCachedResults(new DataStoreTarget("ConfigurationDb", "sched", "ScheduleConfiguration")), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task AFiringOnAnythingButAScheduleIsRefusedAndWritesNothing()
    {
        var paused = await Sut(1).Pause(Firing("Server", Tenant), TestContext.Current.CancellationToken);

        paused.IsFailure.ShouldBeTrue();
        paused.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("SCHEDULEFAILS-20104");
        _sent.ShouldBeNull();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    [InlineData(0)]
    [InlineData(2)]
    public async Task AScheduleWhoseCurrentRowDoesNotTakeTheWriteOnceIsNotPaused(int rows)
    {
        var paused = await Sut(rows).Pause(Firing("Schedule", ScheduleId), TestContext.Current.CancellationToken);

        paused.IsFailure.ShouldBeTrue();
        paused.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("SCHEDULEFAILS-30105");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void EnablingAPausedScheduleStartsItAfreshAndEnablingARunningOneChangesNothing()
    {
        var paused = new ScheduleImplementationConfiguration { IsEnabled = false, ConsecutiveFailures = 3, PausedAt = Now, PausedReason = "paused by a rule" };

        paused.ChangeEnabled(true);

        paused.IsEnabled.ShouldBeTrue();
        paused.ConsecutiveFailures.ShouldBe(0);
        paused.PausedAt.ShouldBeNull();
        paused.PausedReason.ShouldBeNull();

        paused.ConsecutiveFailures = 2;
        paused.ChangeEnabled(true);
        paused.ConsecutiveFailures.ShouldBe(2);

        paused.ChangeEnabled(false);
        paused.IsEnabled.ShouldBeFalse();
        paused.ConsecutiveFailures.ShouldBe(2);
    }
}
