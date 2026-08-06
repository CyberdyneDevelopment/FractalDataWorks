using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Quality.Configuration;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Quality.Tests;

/// <summary>
/// Tests for <see cref="QualityService"/> - quality rule CRUD and check execution/evaluation logic.
/// </summary>
public sealed class QualityServiceTests
{
    private static QualityService CreateService()
    {
        var loggerFactory = LoggerFactory.Create(_ => { });
        var rulesMonitor = new Mock<IOptionsMonitor<System.Collections.Generic.List<QualityRuleConfiguration>>>();
        return new QualityService(loggerFactory, rulesMonitor.Object);
    }

    private static QualityRuleConfiguration MakeRule(string ruleType = "Range", string dataSetName = "Orders", bool isEnabled = true)
        => new()
        {
            RuleType = ruleType,
            DataSetName = dataSetName,
            IsEnabled = isEnabled,
        };

    // ── CreateRule ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task CreateRuleWithValidRuleAssignsIdAndReturnsSuccess()
    {
        var service = CreateService();
        var rule = MakeRule();

        var result = await service.CreateRule(rule, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldNotBe(Guid.Empty);
        result.Value!.RuleType.ShouldBe("Range");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task CreateRuleWithPreAssignedIdPreservesId()
    {
        var service = CreateService();
        var presetId = Guid.NewGuid();
        var rule = MakeRule();
        rule.Id = presetId;

        var result = await service.CreateRule(rule, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(presetId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateRuleWithEmptyRuleTypeReturnsFailureWithInvalidRuleTypeCode()
    {
        var service = CreateService();
        var rule = MakeRule(ruleType: string.Empty);

        var result = await service.CreateRule(rule, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-21001");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task CreateRuleWithWhitespaceRuleTypeReturnsFailure()
    {
        var service = CreateService();
        var rule = MakeRule(ruleType: "   ");

        var result = await service.CreateRule(rule, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-21001");
    }

    // ── UpdateRule ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task UpdateRuleWithExistingIdReplacesRuleAndReturnsSuccess()
    {
        var service = CreateService();
        var created = await service.CreateRule(MakeRule(ruleType: "Range"), TestContext.Current.CancellationToken);
        var id = created.Value!.Id;

        var updated = MakeRule(ruleType: "Pattern");
        var result = await service.UpdateRule(id, updated, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(id);
        result.Value!.RuleType.ShouldBe("Pattern");

        var all = await service.GetAllRules(TestContext.Current.CancellationToken);
        all.Value!.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task UpdateRuleWithNonExistentIdReturnsFailureWithRuleNotFoundCode()
    {
        var service = CreateService();

        var result = await service.UpdateRule(Guid.NewGuid(), MakeRule(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-31004");
    }

    // ── DeleteRule ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task DeleteRuleWithExistingIdReturnsSuccessAndRemovesRule()
    {
        var service = CreateService();
        var created = await service.CreateRule(MakeRule(), TestContext.Current.CancellationToken);

        var result = await service.DeleteRule(created.Value!.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var all = await service.GetAllRules(TestContext.Current.CancellationToken);
        all.Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task DeleteRuleWithNonExistentIdReturnsFailure()
    {
        var service = CreateService();

        var result = await service.DeleteRule(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-31004");
    }

    // ── GetRule ─────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetRuleWithExistingIdReturnsSuccess()
    {
        var service = CreateService();
        var created = await service.CreateRule(MakeRule(), TestContext.Current.CancellationToken);

        var result = await service.GetRule(created.Value!.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(created.Value!.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetRuleWithNonExistentIdReturnsFailure()
    {
        var service = CreateService();

        var result = await service.GetRule(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-31004");
    }

    // ── GetRulesForDataSet / GetAllRules ────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task GetRulesForDataSetReturnsOnlyMatchingDataSetOrdinalCaseSensitive()
    {
        var service = CreateService();
        await service.CreateRule(MakeRule(dataSetName: "Orders"), TestContext.Current.CancellationToken);
        await service.CreateRule(MakeRule(dataSetName: "Customers"), TestContext.Current.CancellationToken);

        var result = await service.GetRulesForDataSet("Orders", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
        result.Value![0].DataSetName.ShouldBe("Orders");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public async Task GetRulesForDataSetIsCaseSensitiveAndExcludesDifferentCasing()
    {
        var service = CreateService();
        await service.CreateRule(MakeRule(dataSetName: "Orders"), TestContext.Current.CancellationToken);

        var result = await service.GetRulesForDataSet("orders", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public async Task GetAllRulesReturnsEveryCreatedRule()
    {
        var service = CreateService();
        await service.CreateRule(MakeRule(dataSetName: "Orders"), TestContext.Current.CancellationToken);
        await service.CreateRule(MakeRule(dataSetName: "Customers"), TestContext.Current.CancellationToken);

        var result = await service.GetAllRules(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(2);
    }

    // ── ExecuteCheck ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteCheckWithExistingRuleReturnsPassingResult()
    {
        var service = CreateService();
        var created = await service.CreateRule(MakeRule(ruleType: "Range"), TestContext.Current.CancellationToken);

        var result = await service.ExecuteCheck(created.Value!.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RuleId.ShouldBe(created.Value!.Id);
        result.Value!.Passed.ShouldBeTrue();
        result.Value!.TotalRecords.ShouldBe(100);
        result.Value!.PassedRecords.ShouldBe(100);
        result.Value!.FailedRecords.ShouldBe(0);
        result.Value!.PassRate.ShouldBe(1.0);
        result.Value!.SampleViolations.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteCheckWithNonExistentRuleReturnsFailure()
    {
        var service = CreateService();

        var result = await service.ExecuteCheck(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "QUALITY-31004");
    }

    // ── ExecuteAllChecks ────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteAllChecksRunsOnlyEnabledRulesForDataSet()
    {
        var service = CreateService();
        await service.CreateRule(MakeRule(dataSetName: "Orders", isEnabled: true), TestContext.Current.CancellationToken);
        await service.CreateRule(MakeRule(dataSetName: "Orders", isEnabled: false), TestContext.Current.CancellationToken);
        await service.CreateRule(MakeRule(dataSetName: "Customers", isEnabled: true), TestContext.Current.CancellationToken);

        var result = await service.ExecuteAllChecks("Orders", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
        result.Value!.All(r => r.Passed).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteAllChecksWithNoRulesForDataSetReturnsEmptyResults()
    {
        var service = CreateService();

        var result = await service.ExecuteAllChecks("Nonexistent", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ExecuteAllChecksWithAllRulesDisabledReturnsEmptyResults()
    {
        var service = CreateService();
        await service.CreateRule(MakeRule(dataSetName: "Orders", isEnabled: false), TestContext.Current.CancellationToken);

        var result = await service.ExecuteAllChecks("Orders", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldBeEmpty();
    }
}
