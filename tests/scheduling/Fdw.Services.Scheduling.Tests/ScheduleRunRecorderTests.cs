using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Tests;

/// <summary>
/// FDW-4598: a fire's last run, next run and status are written to the schedule's implementation container (the one that holds the columns) by the schedule's durable Id, and the
/// container's cached reads are invalidated. A row that does not take the update is a coded failure.
/// </summary>
public sealed class ScheduleRunRecorderTests
{
    private static readonly Guid ScheduleId = Guid.Parse("01a1be2a-1344-7419-866c-a8bd65bf4a7b");
    private static readonly DateTimeOffset Fired = new(2026, 11, 9, 2, 40, 0, TimeSpan.Zero);
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static ScheduleImplementationConfiguration Schedule(int failedSoFar = 0) =>
        new() { Id = ScheduleId, Name = "Nightly", TenantId = Tenant, ConsecutiveFailures = failedSoFar };

    private sealed class Fixture
    {
        public Fixture(IGenericResult<int> answer, IGenericResult? reported = null)
        {
            Reporter
                .Setup(r => r.Report(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(reported ?? GenericResult.Success());
            Gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .Callback((IDataCommand command, DataStoreTarget target, CancellationToken _) =>
                {
                    Command = command;
                    Target = target;
                })
                .ReturnsAsync(answer);
            var gateways = new Mock<IConfigurationGatewayProvider>();
            gateways.Setup(p => p.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(Gateway.Object));
            Provider = new ScheduleImplementationConfigurationProvider(
                NullLogger<ScheduleImplementationConfigurationProvider>.Instance, gateways.Object, "ConfigurationDb", "sched", "ScheduleConfiguration",
                Reporter.Object, TimeProvider.System);
        }

        public Mock<IConfigurationGateway> Gateway { get; } = new();

        public Mock<IScheduleFailureReporter> Reporter { get; } = new();

        public ScheduleImplementationConfigurationProvider Provider { get; }

        public IDataCommand? Command { get; private set; }

        public DataStoreTarget? Target { get; private set; }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task AFireIsRecordedOnTheContainerThatOwnsTheColumnsAndItsCachedReadsAreInvalidated()
    {
        var fixture = new Fixture(GenericResult<int>.Success(1));

        var result = await fixture.Provider.RecordRun(Schedule(), Fired, Fired.AddMinutes(5), "Pending", false, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        fixture.Target.ShouldBe(new DataStoreTarget("ConfigurationDb", "sched", "ScheduleConfiguration"));
        fixture.Command.ShouldNotBeNull();
        fixture.Gateway.Verify(g => g.InvalidateCachedResults(new DataStoreTarget("ConfigurationDb", "sched", "ScheduleConfiguration")), Times.Once);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    [InlineData(0)]
    [InlineData(2)]
    public async Task AScheduleWhoseCurrentRowDoesNotTakeTheUpdateOnceIsACodedFailure(int rows)
    {
        var fixture = new Fixture(GenericResult<int>.Success(rows));

        var result = await fixture.Provider.RecordRun(Schedule(), Fired, null, "Failed", true, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("SCHEDULERUNS-14310");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task AFailedWriteIsPassedOnAndTheCacheIsLeftAlone()
    {
        var fixture = new Fixture(GenericResult<int>.Failure(new Fdw.Messages.GenericMessage("the store refused it")));

        var result = await fixture.Provider.RecordRun(Schedule(), Fired, null, "Failed", true, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        fixture.Gateway.Verify(g => g.InvalidateCachedResults(It.IsAny<DataStoreTarget>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task AFireThatFailedAddsOneToTheCountAndTheCountIsWrittenAndReported()
    {
        var fixture = new Fixture(GenericResult<int>.Success(1));

        var result = await fixture.Provider.RecordRun(Schedule(failedSoFar: 2), Fired, null, "Failed", true, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        fixture.Command.ShouldBeOfType<UpdateCommand<ScheduleUpdateRecord>>().Data.ConsecutiveFailures.ShouldBe(3);
        fixture.Reporter.Verify(r => r.Report(ScheduleId, "Nightly", Tenant, 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task AFireThatDidNotFailStartsTheCountAgainAndReportsTheZero()
    {
        var fixture = new Fixture(GenericResult<int>.Success(1));

        var result = await fixture.Provider.RecordRun(Schedule(failedSoFar: 2), Fired, Fired.AddMinutes(5), "Pending", false, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        fixture.Command.ShouldBeOfType<UpdateCommand<ScheduleUpdateRecord>>().Data.ConsecutiveFailures.ShouldBe(0);
        fixture.Reporter.Verify(r => r.Report(ScheduleId, "Nightly", Tenant, 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task ARowThatDidNotTakeTheUpdateReportsNothingSoACountThatWasNotWrittenIsNeverJudged()
    {
        var fixture = new Fixture(GenericResult<int>.Success(0));

        var result = await fixture.Provider.RecordRun(Schedule(), Fired, null, "Failed", true, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        fixture.Reporter.Verify(r => r.Report(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task AReportThatCannotBeMadeFailsTheRecordingAfterTheOutcomeIsWritten()
    {
        var fixture = new Fixture(GenericResult<int>.Success(1), GenericResult.Failure(new Fdw.Messages.GenericMessage("the thresholds could not be read")));

        var result = await fixture.Provider.RecordRun(Schedule(), Fired, null, "Failed", true, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        fixture.Command.ShouldNotBeNull();
        fixture.Gateway.Verify(g => g.InvalidateCachedResults(It.IsAny<DataStoreTarget>()), Times.Once);
    }
}
