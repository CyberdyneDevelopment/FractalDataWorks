using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling;
using Fdw.Services.Scheduling.Abstractions.Configuration;
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

    private sealed class Fixture
    {
        public Fixture(IGenericResult<int> answer)
        {
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
                NullLogger<ScheduleImplementationConfigurationProvider>.Instance, gateways.Object, "ConfigurationDb", "sched", "ScheduleConfiguration");
        }

        public Mock<IConfigurationGateway> Gateway { get; } = new();

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

        var result = await fixture.Provider.RecordRun(ScheduleId, Fired, Fired.AddMinutes(5), "Pending", TestContext.Current.CancellationToken);

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

        var result = await fixture.Provider.RecordRun(ScheduleId, Fired, null, "Failed", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("SCHEDULERUNS-14310");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public async Task AFailedWriteIsPassedOnAndTheCacheIsLeftAlone()
    {
        var fixture = new Fixture(GenericResult<int>.Failure(new Fdw.Messages.GenericMessage("the store refused it")));

        var result = await fixture.Provider.RecordRun(ScheduleId, Fired, null, "Failed", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        fixture.Gateway.Verify(g => g.InvalidateCachedResults(It.IsAny<DataStoreTarget>()), Times.Never);
    }
}
