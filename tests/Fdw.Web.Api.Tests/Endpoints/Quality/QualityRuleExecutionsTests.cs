using System.Security.Claims;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality.Configuration;
using Fdw.Services.Quality.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Quality;

/// <summary>
/// Tests for <see cref="QualityRuleExecutions"/> (FDW-1363): a verdict is kept as the row of the rule that ran, under the
/// caller's tenant, in the operational store the host names; and it is never kept as a pass it was not.
/// </summary>
public sealed class QualityRuleExecutionsTests
{
    private static readonly Guid RuleId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantId = new("22222222-2222-2222-2222-222222222222");

    private static readonly QualityRuleImplementationConfiguration Rule =
        new() { RuleType = "NotNull", FieldName = "Name", DataSetName = "Teams", Severity = "Warning" };

    private static QualityCheckResultResponse Verdict(string status, int failures, int total, string? error = null) =>
        new() { Status = status, FailureCount = failures, TotalCount = total, ExecutedAt = new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc), ErrorMessage = error };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AFailedCheckKeepsItsPassAndFailCounts()
    {
        var row = QualityRuleExecutions.Of(RuleId, Rule, Verdict("Failed", 3, 10), TenantId, 42);

        row.QualityRuleId.ShouldBe(RuleId);
        row.TenantId.ShouldBe(TenantId);
        row.DataSetName.ShouldBe("Teams");
        row.FieldName.ShouldBe("Name");
        row.RuleType.ShouldBe("NotNull");
        row.Severity.ShouldBe("Warning");
        row.Outcome.ShouldBe("Failed");
        row.PassCount.ShouldBe(7);
        row.FailCount.ShouldBe(3);
        row.SkipCount.ShouldBe(0);
        row.ExecutionDurationMs.ShouldBe(42);
        row.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARuleThatCouldNotRunJudgedNoRowsAndNeverReadsAsAPass()
    {
        var row = QualityRuleExecutions.Of(RuleId, Rule, Verdict("Failed", 0, 10, "FieldName is required for NotNull rule"), TenantId, 1);

        row.Outcome.ShouldBe("Failed");
        row.PassCount.ShouldBe(0);
        row.FailCount.ShouldBe(0);
        row.SkipCount.ShouldBe(10);
        row.ErrorMessage.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANotSupportedRuleIsRecordedAsNotJudged()
    {
        var row = QualityRuleExecutions.Of(RuleId, Rule, Verdict("NotSupported", 0, 5, "CustomExpression rules are not yet supported"), TenantId, 1);

        row.Outcome.ShouldBe("NotSupported");
        row.PassCount.ShouldBe(0);
        row.SkipCount.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task RecordingWithNoOperationalStoreIsRefusedAndWritesNothing()
    {
        var gateway = new Mock<IDataGateway>();
        var row = QualityRuleExecutions.Of(RuleId, Rule, Verdict("Passed", 0, 4), TenantId, 1);

        var result = await QualityRuleExecutions.Record(gateway.Object, null, row, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        gateway.Verify(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARecordedVerdictIsInsertedIntoTheExecutionsTableOfTheHostsStore()
    {
        DataStoreTarget? target = null;
        IDataCommand? command = null;
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((c, t, _) => { command = c; target = t; })
            .ReturnsAsync(GenericResult<int>.Success(1));
        var row = QualityRuleExecutions.Of(RuleId, Rule, Verdict("Passed", 0, 4), TenantId, 1);

        var result = await QualityRuleExecutions.Record(gateway.Object, "OpsDb", row, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        target.ShouldBe(new DataStoreTarget("OpsDb", "dq", "QualityRuleExecution"));
        command.ShouldBeOfType<InsertCommand<QualityRuleExecutionRecord>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AWriteTheStoreRefusesIsAFailureTheCallerIsToldAbout()
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Failure(Mock.Of<IGenericMessage>()));
        var row = QualityRuleExecutions.Of(RuleId, Rule, Verdict("Passed", 0, 4), TenantId, 1);

        var result = await QualityRuleExecutions.Record(gateway.Object, "OpsDb", row, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ACallerWithNoActiveTenantHasNothingToRecordUnder()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        var tenant = QualityRuleExecutions.CallerTenant(anonymous, NullLogger.Instance);

        tenant.IsFailure.ShouldBeTrue();
    }
}
