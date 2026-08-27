using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Data;
using Microsoft.Extensions.Logging.Abstractions;
// Why: Aliases disambiguate from Fdw.Services.Scheduling.Commands.Data
// and Fdw.Services.Scheduling.Data namespace segments (matches TenantIsolationTests convention).
using FilterGroup = Fdw.Data.FilterGroup;
using IFilterCondition = Fdw.Data.Abstractions.IFilterCondition;
using IFilterNode = Fdw.Data.Abstractions.IFilterNode;
using ScheduleQueryCommand = Fdw.Commands.Data.QueryCommand<Fdw.Services.Scheduling.Data.ScheduleQueryRecord>;

namespace Fdw.Services.Scheduling.Tests;

/// <summary>
/// Unit tests for <see cref="DefaultSchedulingService"/> covering CRUD, pause/resume toggling,
/// not-found vs gateway-failure result-code distinction, and the tenant OR-group filter applied
/// to single-record lookups (<c>GetScheduleRecord</c>) — complementary to
/// <see cref="TenantIsolationTests"/>, which only exercises the multi-record <c>GetSchedules</c> path.
/// </summary>
public sealed class DefaultSchedulingServiceTests
{
    private static DefaultSchedulerConfiguration CreateConfig() => new()
    {
        Id = Guid.CreateVersion7(),
        Name = "TestScheduler",
        DataStoreName = "ConfigurationDb",
        PathName = "sched",
        ScheduleContainerName = "Schedule"
    };

    private static Mock<IDataGateway> CreateGatewayMock() => new();

    private sealed class TestSchedule : IGenericSchedule
    {
        public TestSchedule(
            string scheduleName,
            string processId = "test-process",
            string cronExpression = "0 0 * * *",
            bool isActive = true,
            string timeZoneId = "UTC",
            DateTime? nextExecution = null)
        {
            ScheduleId = scheduleName;
            ScheduleName = scheduleName;
            ProcessId = processId;
            CronExpression = cronExpression;
            IsActive = isActive;
            TimeZoneId = timeZoneId;
            NextExecution = nextExecution;
        }

        public string ScheduleId { get; }
        public string ScheduleName { get; }
        public string ProcessId { get; }
        public string CronExpression { get; }
        public DateTime? NextExecution { get; }
        public bool IsActive { get; }
        public string TimeZoneId { get; }
        public IReadOnlyDictionary<string, object>? Metadata => null;
    }

    // ── Constructor guards ──────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_NullDataGateway_ThrowsArgumentNullException()
    {
        // Arrange
        var config = CreateConfig();

        // Act
        var act = () => new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, null!, config);

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("dataGateway");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var dataGatewayMock = CreateGatewayMock();

        // Act
        var act = () => new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("configuration");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task Constructor_NullLogger_FallsBackToNullLoggerInstanceAndRemainsFunctional()
    {
        // Arrange — the only sanctioned `??` fallback in the codebase: logger ?? NullLogger<T>.Instance
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleQueryRecord>>.Success(Array.Empty<ScheduleQueryRecord>()));

        var service = new DefaultSchedulingService(null!, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.GetSchedules(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue("a null logger must not prevent normal operation");
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "CoreFramework")]
    public void Scheduler_PropertyAccess_ThrowsNotSupportedException()
    {
        // Arrange
        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, CreateGatewayMock().Object, CreateConfig());

        // Act
        var act = () => service.Scheduler;

        // Assert
        Should.Throw<NotSupportedException>(act);
    }

    // ── CreateSchedule — idempotency on Name ────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateSchedule_WhenScheduleNameAlreadyExists_ReturnsSuccessWithoutInserting()
    {
        // Arrange — GetScheduleRecord (the idempotency check) finds an existing active row
        var existing = new ScheduleQueryRecord { Name = "daily-report", PipelineName = "report-generator", IsEnabled = true };

        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleQueryRecord>>.Success([existing]));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act — create the same schedule name a second time
        var result = await service.CreateSchedule(new TestSchedule("daily-report"), TestContext.Current.CancellationToken);

        // Assert — treated as success, no INSERT issued (sched.Schedule has a unique index on Name)
        result.IsSuccess.ShouldBeTrue();
        dataGatewayMock.Verify(
            g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "a duplicate CreateSchedule must not issue an INSERT");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public async Task CreateSchedule_WhenScheduleNameIsNew_InsertsRecordAndReturnsSuccess()
    {
        // Arrange — no existing row found
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleQueryRecord>>.Success(Array.Empty<ScheduleQueryRecord>()));

        InsertCommand<ScheduleInsertRecord>? captured = null;
        dataGatewayMock
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((cmd, _, _) => captured = cmd as InsertCommand<ScheduleInsertRecord>)
            .ReturnsAsync(GenericResult<int>.Success(1));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.CreateSchedule(new TestSchedule("new-schedule", processId: "process-1"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        captured.ShouldNotBeNull();
        captured!.Data.Name.ShouldBe("new-schedule");
        captured.Data.PipelineName.ShouldBe("process-1");
        captured.Data.Id.ShouldNotBe(Guid.Empty, "Id is uniqueidentifier NOT NULL with no DB default — the caller must mint it");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public async Task CreateSchedule_WhenInsertFails_ReturnsFailureWithCreationFailedCode()
    {
        // Arrange
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleQueryRecord>>.Success(Array.Empty<ScheduleQueryRecord>()));
        dataGatewayMock
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Failure(new GenericMessage("simulated insert failure")));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.CreateSchedule(new TestSchedule("broken-schedule"), TestContext.Current.CancellationToken);

        // Assert — CreationFailed (EventId 91000)
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
        result.Messages[0].Code.ShouldBe("SCHEDULING2-91000");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task CreateSchedule_WhenGatewayThrows_ReturnsFailureWithExceptionCode()
    {
        // Arrange
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated connection failure"));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.CreateSchedule(new TestSchedule("exploding-schedule"), TestContext.Current.CancellationToken);

        // Assert — GetSchedulingException (EventId 91002), caught not thrown
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("SCHEDULING2-91002");
    }

    // ── DeleteSchedule — not-found vs gateway-failure distinction (P0) ──────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task DeleteSchedule_WhenGatewayFails_ReturnsFailureWithSchedulingFailedCode()
    {
        // Arrange — the gateway itself reports failure (e.g. connection error), distinct from "0 rows affected"
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Failure(new GenericMessage("simulated gateway failure")));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.DeleteSchedule("missing-or-broken", TestContext.Current.CancellationToken);

        // Assert — SchedulingFailed (EventId 91005), the generic gateway-failure code
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("SCHEDULING2-91005");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task DeleteSchedule_WhenNoRowsAffected_ReturnsFailureWithScheduleNotFoundCode()
    {
        // Arrange — gateway succeeds, but 0 rows matched: the schedule genuinely does not exist
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(0));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.DeleteSchedule("does-not-exist", TestContext.Current.CancellationToken);

        // Assert — ScheduleNotFound (EventId 31000) — a DIFFERENT code than the gateway-failure case above,
        // even though both return a non-success IGenericResult.
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("SCHEDULING2-31000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public async Task DeleteSchedule_WhenRowsAffected_ReturnsSuccess()
    {
        // Arrange
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(1));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.DeleteSchedule("real-schedule", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // ── UpdateSchedule — same not-found vs gateway-failure shape ────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task UpdateSchedule_WhenGatewayFails_ReturnsFailureWithSchedulingFailedCode()
    {
        // Arrange
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Failure(new GenericMessage("simulated gateway failure")));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.UpdateSchedule(new TestSchedule("some-schedule"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("SCHEDULING2-91005");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task UpdateSchedule_WhenNoRowsAffected_ReturnsFailureWithScheduleNotFoundCode()
    {
        // Arrange
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(0));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.UpdateSchedule(new TestSchedule("ghost-schedule"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("SCHEDULING2-31000");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task UpdateSchedule_WhenSuccessful_ReturnsSuccess()
    {
        // Arrange
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(1));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.UpdateSchedule(
            new TestSchedule("real-schedule", nextExecution: DateTime.UtcNow.AddHours(1)),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // ── PauseSchedule / ResumeSchedule — toggle behavior ─────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public async Task PauseSchedule_WhenScheduleExists_SendsIsEnabledFalseAndReturnsSuccess()
    {
        // Arrange — GetScheduleRecord finds an existing, currently-enabled schedule
        var existing = new ScheduleQueryRecord
        {
            Name = "toggle-me",
            PipelineName = "process-x",
            ServiceOptionType = "Cron",
            CronExpression = "0 9 * * *",
            TimeZoneId = "UTC",
            IsEnabled = true
        };

        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleQueryRecord>>.Success([existing]));

        UpdateCommand<ScheduleInsertRecord>? captured = null;
        dataGatewayMock
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((cmd, _, _) => captured = cmd as UpdateCommand<ScheduleInsertRecord>)
            .ReturnsAsync(GenericResult<int>.Success(1));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.PauseSchedule("toggle-me", TestContext.Current.CancellationToken);

        // Assert — the toggle must flip IsEnabled to false, preserving the rest of the row
        result.IsSuccess.ShouldBeTrue();
        captured.ShouldNotBeNull();
        captured!.Data.IsEnabled.ShouldBeFalse();
        captured.Data.Name.ShouldBe("toggle-me");
        captured.Data.PipelineName.ShouldBe("process-x");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public async Task ResumeSchedule_WhenScheduleExists_SendsIsEnabledTrueAndReturnsSuccess()
    {
        // Arrange — GetScheduleRecord finds an existing, currently-disabled schedule
        var existing = new ScheduleQueryRecord
        {
            Name = "toggle-me",
            PipelineName = "process-x",
            ServiceOptionType = "Cron",
            CronExpression = "0 9 * * *",
            TimeZoneId = "UTC",
            IsEnabled = false
        };

        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleQueryRecord>>.Success([existing]));

        UpdateCommand<ScheduleInsertRecord>? captured = null;
        dataGatewayMock
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((cmd, _, _) => captured = cmd as UpdateCommand<ScheduleInsertRecord>)
            .ReturnsAsync(GenericResult<int>.Success(1));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.ResumeSchedule("toggle-me", TestContext.Current.CancellationToken);

        // Assert — the toggle must flip IsEnabled to true
        result.IsSuccess.ShouldBeTrue();
        captured.ShouldNotBeNull();
        captured!.Data.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public async Task PauseSchedule_WhenScheduleNotFound_ReturnsFailureWithScheduleNotFoundCode()
    {
        // Arrange — no matching row
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleQueryRecord>>.Success(Array.Empty<ScheduleQueryRecord>()));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.PauseSchedule("ghost", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("SCHEDULING2-31000");
        dataGatewayMock.Verify(
            g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "a not-found schedule must not trigger an UPDATE");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public async Task ResumeSchedule_WhenScheduleNotFound_ReturnsFailureWithScheduleNotFoundCode()
    {
        // Arrange
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleQueryRecord>>.Success(Array.Empty<ScheduleQueryRecord>()));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.ResumeSchedule("ghost", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("SCHEDULING2-31000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task PauseSchedule_WhenGatewayUpdateFails_ReturnsFailureWithSchedulingFailedCode()
    {
        // Arrange — the lookup succeeds (schedule exists) but the UPDATE itself fails at the gateway
        var existing = new ScheduleQueryRecord { Name = "flaky", PipelineName = "p", IsEnabled = true };

        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleQueryRecord>>.Success([existing]));
        dataGatewayMock
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Failure(new GenericMessage("simulated update failure")));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.PauseSchedule("flaky", TestContext.Current.CancellationToken);

        // Assert — distinct from the not-found case: the schedule WAS found, the UPDATE failed
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("SCHEDULING2-91005");
    }

    // ── TriggerSchedule ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task TriggerSchedule_WhenScheduleNotFound_ReturnsFailureWithScheduleNotFoundCode()
    {
        // Arrange
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleQueryRecord>>.Success(Array.Empty<ScheduleQueryRecord>()));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.TriggerSchedule("ghost", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("SCHEDULING2-31000");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task TriggerSchedule_WhenScheduleExists_ReturnsSuccess()
    {
        // Arrange
        var existing = new ScheduleQueryRecord { Name = "fire-now", PipelineName = "p", IsEnabled = true };

        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleQueryRecord>>.Success([existing]));
        dataGatewayMock
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(1));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.TriggerSchedule("fire-now", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // ── GetSchedule ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task GetSchedule_WhenFound_ReturnsSuccessWithMappedSchedule()
    {
        // Arrange
        var existing = new ScheduleQueryRecord
        {
            Name = "mapped-schedule",
            PipelineName = "process-y",
            CronExpression = "0 0 * * *",
            TimeZoneId = "UTC",
            IsEnabled = true
        };

        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleQueryRecord>>.Success([existing]));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.GetSchedule("mapped-schedule", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.ScheduleName.ShouldBe("mapped-schedule");
        result.Value.ProcessId.ShouldBe("process-y");
        result.Value.IsActive.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task GetSchedule_WhenNotFound_ReturnsFailureWithScheduleNotFoundCode()
    {
        // Arrange
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleQueryRecord>>.Success(Array.Empty<ScheduleQueryRecord>()));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.GetSchedule("ghost", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("SCHEDULING2-31000");
    }

    // ── GetSchedules ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task GetSchedules_WhenGatewayFails_ReturnsFailureWithSchedulingFailedCode()
    {
        // Arrange
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleQueryRecord>>.Failure(new GenericMessage("simulated query failure")));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig());

        // Act
        var result = await service.GetSchedules(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("SCHEDULING2-91005");
    }

    // ── IGenericService.Execute — unsupported contract ──────────────────────

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "CoreFramework")]
    public async Task Execute_GenericCommand_ReturnsUnsupportedFailure()
    {
        // Arrange
        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, CreateGatewayMock().Object, CreateConfig());
        var commandMock = new Mock<IGenericCommand>();

        // Act
        var result = await service.Execute(commandMock.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("SCHEDULING2-91005");
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteOfT_GenericCommand_ReturnsUnsupportedFailure()
    {
        // Arrange
        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, CreateGatewayMock().Object, CreateConfig());
        var commandMock = new Mock<IGenericCommand>();

        // Act
        var result = await service.Execute<int>(commandMock.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("SCHEDULING2-91005");
    }

    // ── Tenant OR-group filter on single-record lookups (P0) ────────────────
    // TenantIsolationTests proves the filter for the multi-record GetSchedules() path (built
    // via DefaultSchedulingFactory). These tests prove the SAME OR-group discipline is applied by
    // the private GetScheduleRecord() helper — reached by CreateSchedule/GetSchedule/PauseSchedule/
    // ResumeSchedule/TriggerSchedule — which TenantIsolationTests does not exercise.

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task GetScheduleRecord_WithTenantContext_AppliesOrGroupFilterIncludingTenantId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(t => t.HasTenant).Returns(true);
        tenantContextMock.Setup(t => t.TenantId).Returns(tenantId);

        IDataCommand? captured = null;
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((cmd, _, _) => captured = cmd)
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleQueryRecord>>.Success(Array.Empty<ScheduleQueryRecord>()));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig(), tenantContextMock.Object);

        // Act — GetSchedule exercises the private GetScheduleRecord helper directly
        await service.GetSchedule("any-name", TestContext.Current.CancellationToken);

        // Assert — the single-record lookup must carry the same TenantId OR IS NULL filter
        captured.ShouldNotBeNull();
        AssertContainsGuidCondition(captured!, tenantId).ShouldBeTrue(
            "GetScheduleRecord must apply the tenant OR-group filter when a tenant context is present");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task GetScheduleRecord_WithTenantContext_ExcludesOtherTenantsId()
    {
        // Arrange — tenant A's context must never leak tenant B's GUID into the filter
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();

        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(t => t.HasTenant).Returns(true);
        tenantContextMock.Setup(t => t.TenantId).Returns(tenantAId);

        IDataCommand? captured = null;
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((cmd, _, _) => captured = cmd)
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleQueryRecord>>.Success(Array.Empty<ScheduleQueryRecord>()));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig(), tenantContextMock.Object);

        // Act
        await service.PauseSchedule("any-name", TestContext.Current.CancellationToken);

        // Assert
        captured.ShouldNotBeNull();
        AssertContainsGuidCondition(captured!, tenantAId).ShouldBeTrue();
        AssertContainsGuidCondition(captured!, tenantBId).ShouldBeFalse(
            "cross-tenant data leak: tenant B's GUID must never appear in tenant A's filter");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task GetScheduleRecord_WithoutTenantContext_AppliesNoTenantFilter()
    {
        // Arrange — no ITenantContext supplied at all (background worker / cron daemon scenario)
        IDataCommand? captured = null;
        var dataGatewayMock = CreateGatewayMock();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<ScheduleQueryRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((cmd, _, _) => captured = cmd)
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleQueryRecord>>.Success(Array.Empty<ScheduleQueryRecord>()));

        var service = new DefaultSchedulingService(NullLogger<DefaultSchedulingService>.Instance, dataGatewayMock.Object, CreateConfig(), tenantContext: null);

        // Act
        await service.ResumeSchedule("any-name", TestContext.Current.CancellationToken);

        // Assert — no GUID-valued condition anywhere in the filter tree
        captured.ShouldNotBeNull();
        var queryCommand = captured as ScheduleQueryCommand;
        if (queryCommand?.Filter is not null)
        {
            ContainsAnyGuidConditionValue(queryCommand.Filter.Root).ShouldBeFalse(
                "without a tenant context, no tenant filter may be applied — all schedules are visible");
        }
    }

    // ── Filter-tree inspection helpers (mirrors TenantIsolationTests) ───────

    private static bool AssertContainsGuidCondition(IDataCommand command, Guid targetId)
    {
        var queryCommand = command as ScheduleQueryCommand;
        queryCommand.ShouldNotBeNull($"Expected QueryCommand<ScheduleQueryRecord> but got {command.GetType().Name}");

        var filter = queryCommand!.Filter;
        if (filter is null)
            return false;

        return ContainsGuidConditionValue(filter.Root, targetId);
    }

    private static bool ContainsGuidConditionValue(IFilterNode? node, Guid targetId)
    {
        if (node is null) return false;

        if (node is IFilterCondition condition)
        {
            return condition.Value is Guid g && g == targetId;
        }

        if (node is FilterGroup group)
        {
            foreach (var child in group.Nodes)
                if (ContainsGuidConditionValue(child, targetId)) return true;
        }

        return false;
    }

    private static bool ContainsAnyGuidConditionValue(IFilterNode? node)
    {
        if (node is null) return false;

        if (node is IFilterCondition condition)
            return condition.Value is Guid;

        if (node is FilterGroup group)
        {
            foreach (var child in group.Nodes)
                if (ContainsAnyGuidConditionValue(child)) return true;
        }

        return false;
    }
}
