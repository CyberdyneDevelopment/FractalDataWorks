using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.DataConfigurationRules;
using Fdw.Services.Data.Logging;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests.DataConfigurationRules;

/// <summary>
/// FDW-6345: a data configuration rule and its values are written in one transaction on the store that holds them, in the order write,
/// record in the audit, commit; a failure before the commit rolls everything back and records nothing. Each refusal is the contract's own
/// coded message and is answered before anything is written.
/// </summary>
public sealed class DataConfigurationRuleServiceTests
{
    private const string Store = "ConfigStore";
    private static readonly Guid RuleId = Guid.Parse("0193a4f1-7c2e-7b10-9a33-5f0e2b9c4d18");
    private static readonly DataConfigurationRuleContainer Target = new() { DataStore = "NflDb", DataPath = "NflData", Container = "Teams" };
    private static readonly Guid Custodian = Guid.Parse("0193a100-aaaa-7bbb-8ccc-0123456789ab");
    private static readonly Guid Stranger = Guid.Parse("0193a100-bbbb-7bbb-8ccc-0123456789ab");
    private static readonly string[] InPath = ["DataConfigurationRules", "Comparison", "In"];

    // FDW-6543 (DataOption-setup's decision): the rule service names its tables with four settings of its own, never the three the released option row service reads.
    [Theory]
    [InlineData("DataConfigurationRuleDataStore")]
    [InlineData("DataConfigurationRulePath")]
    [InlineData("DataConfigurationRuleContainer")]
    [InlineData("DataConfigurationRuleValueContainer")]
    public async Task EveryOperationRefusesNamingTheRuleSettingThatIsNotSet(string setting)
    {
        var settings = Fixture.Defaults();
        settings.Remove(setting);
        var fixture = new Fixture(settings: settings);
        var ct = TestContext.Current.CancellationToken;

        foreach (var refused in new[]
        {
            await fixture.Service.List(new DataConfigurationRulesQuery { Target = Target, Removed = false, Skip = 0, Take = 10 }, ct) as IGenericResult,
            await fixture.Service.Get(Key(), ct),
            await fixture.Service.Add(New(), ct),
        })
        {
            Codes(refused).ShouldContain("DATA-121680");
            refused.Messages.Select(m => m.Message).ShouldContain(message => message.Contains(setting, StringComparison.Ordinal));
        }

        fixture.Events.ShouldBeEmpty();
    }

    // The settings the released option row service reads name a consumer's own table: setting them does not name the rule tables.
    [Fact]
    public async Task TheSettingsTheOptionRowServiceReadsDoNotNameTheRuleTables()
    {
        var fixture = new Fixture(settings: new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [SettingDefinitions.DataOptionsDataStore] = Store,
            [SettingDefinitions.DataOptionsPath] = "data",
            [SettingDefinitions.DataOptionsContainer] = "DataOption",
        });

        Codes(await fixture.Service.Get(Key(), TestContext.Current.CancellationToken)).ShouldContain("DATA-121680");
        fixture.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnAddWritesTheRuleThenEachValueRecordsOnceAndCommitsInThatOrder()
    {
        var fixture = new Fixture(reads: [[], [], [Stored()], [StoredValue(1, "East"), StoredValue(2, "West")]]);

        var added = await fixture.Service.Add(New(), TestContext.Current.CancellationToken);

        added.IsSuccess.ShouldBeTrue(added.CurrentMessage);
        fixture.Events.ShouldBe(["write", "write", "write", "record", "commit"]);
        var saves = fixture.Writes.OfType<ConfigurationSaveCommand<DataConfigurationRuleRecord>>().ToList();
        saves.ShouldHaveSingleItem().ReplacesCurrent.ShouldBeFalse();
        saves[0].Data.Path.ShouldBe("DataConfigurationRules:Comparison:In.To");
        saves[0].Data.Container.ShouldBe("Teams");
        fixture.Writes.OfType<ConfigurationSaveCommand<DataConfigurationRuleValueRecord>>().Select(w => w.Data.Ordinal).ShouldBe([1, 2]);
        fixture.Recorded.ShouldHaveSingleItem().Select(e => e.ChangeKind).ShouldBe(["RuleAdded", "ValueAdded", "ValueAdded"]);
        added.Value!.Values.Count.ShouldBe(2);
    }

    [Fact]
    public async Task AWriteThatFailsRollsEverythingBackAndRecordsNothing()
    {
        var fixture = new Fixture(reads: [[], []], writes: [true, false]);

        var added = await fixture.Service.Add(New(), TestContext.Current.CancellationToken);

        added.IsFailure.ShouldBeTrue();
        fixture.Events.ShouldBe(["write", "write", "rollback"]);
        fixture.Recorded.ShouldBeEmpty();
    }

    [Fact]
    public async Task AChangeThatCannotBeRecordedRollsBackAndIsNotCommitted()
    {
        var fixture = new Fixture(reads: [[], []], auditFails: true);

        var added = await fixture.Service.Add(New(), TestContext.Current.CancellationToken);

        added.IsFailure.ShouldBeTrue();
        fixture.Events.ShouldBe(["write", "write", "write", "record", "rollback"]);
    }

    [Fact]
    public async Task AStoreThatCannotHoldATransactionIsRefusedNamingItAndNothingIsWritten()
    {
        var fixture = new Fixture(reads: [[], []], transactional: false);

        var added = await fixture.Service.Add(New(), TestContext.Current.CancellationToken);

        Codes(added).First().ShouldBe("DATA-121681");
        fixture.Writes.ShouldBeEmpty();
    }

    // Release-Supervisor (2026-11-17): the picker and the check use the holders of datastores:write; a custodian is one of them.
    [Fact]
    public async Task AnAddRefusesACustodianWhoDoesNotHoldDatastoresWriteWithItsCodeAndWritesNothing()
    {
        var fixture = new Fixture(holders: [Stranger]);

        var added = await fixture.Service.Add(New(), TestContext.Current.CancellationToken);

        Codes(added).ShouldContain("DATA-31690");
        fixture.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnAddWhoseHoldersCannotBeReadFailsAndIsNeverAnsweredAsNoHolders()
    {
        var fixture = new Fixture(holdersFail: true);

        var added = await fixture.Service.Add(New(), TestContext.Current.CancellationToken);

        added.IsFailure.ShouldBeTrue();
        Codes(added).ShouldNotContain("DATA-31690");
        fixture.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task AChangeToACustodianWhoDoesNotHoldDatastoresWriteIsRefusedWithItsCode()
    {
        var fixture = new Fixture(reads: [[Stored()], []], holders: [Custodian]);

        var changed = await fixture.Service.Change(new DataConfigurationRuleChange { Key = Key(), CustodianUserId = Stranger }, TestContext.Current.CancellationToken);

        Codes(changed).ShouldContain("DATA-31690");
        fixture.Events.ShouldBeEmpty();
    }

    // A custodian who has since lost the permission is not hidden and does not block the rule being renamed: only a change that names a different
    // custodian is checked against the holders; the rule is then shown with the NoLongerHolder state.
    [Fact]
    public async Task AChangeThatLeavesTheCustodianAloneIsNotRefusedWhenTheCustodianHasLostThePermission()
    {
        var fixture = new Fixture(reads: [[Stored()], [], [Stored()], []], holders: []);

        var changed = await fixture.Service.Change(new DataConfigurationRuleChange { Key = Key(), Description = "East division teams" }, TestContext.Current.CancellationToken);

        changed.IsSuccess.ShouldBeTrue(changed.CurrentMessage);
        changed.Value!.CustodianState.ShouldBe("NoLongerHolder");
    }

    [Fact]
    public async Task ARuleStatesWhetherItsCustodianStillHoldsTheWritePermission()
    {
        var holds = new Fixture(reads: [[Stored()], []], holders: [Custodian]);
        var lost = new Fixture(reads: [[Stored()], []], holders: [Stranger]);

        (await holds.Service.Get(Key(), TestContext.Current.CancellationToken)).Value!.CustodianState.ShouldBe("Holder");
        (await lost.Service.Get(Key(), TestContext.Current.CancellationToken)).Value!.CustodianState.ShouldBe("NoLongerHolder");
    }

    [Fact]
    public async Task ARuleWhoseHoldersCannotBeReadFailsWithTheReadsOwnMessageAndIsNeverGivenAGuessedState()
    {
        var fixture = new Fixture(reads: [[Stored()], []], holdersFail: true);

        var rule = await fixture.Service.Get(Key(), TestContext.Current.CancellationToken);

        rule.IsFailure.ShouldBeTrue();
        Codes(rule).ShouldNotContain("DATA-31690");
    }

    // Release-Supervisor (2026-11-17): a workspace owner must exist and be one the caller may view (the workspace access policy's MayView).
    [Fact]
    public async Task AnAddRefusesAWorkspaceOwnerTheCallerCannotViewWithItsCodeAndWritesNothing()
    {
        var fixture = new Fixture(workspaceAccess: "hidden");

        var added = await fixture.Service.Add(New(), TestContext.Current.CancellationToken);

        Codes(added).ShouldContain("DATA-51682");
        fixture.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnAddRefusesAWorkspaceOwnerThatIsNotThereWithItsCode()
    {
        var fixture = new Fixture(workspaceFound: false);

        var added = await fixture.Service.Add(New(), TestContext.Current.CancellationToken);

        Codes(added).ShouldContain("DATA-31688");
        fixture.ViewChecks.ShouldBe(0);
        fixture.Events.ShouldBeEmpty();
    }

    // A policy that cannot tell fails with its own message: answering it as not visible would hide an outage.
    [Fact]
    public async Task AnAddWhoseAccessPolicyCannotTellFailsAndIsNeverAnsweredAsNotVisible()
    {
        var fixture = new Fixture(workspaceAccess: "undetermined");

        var added = await fixture.Service.Add(New(), TestContext.Current.CancellationToken);

        added.IsFailure.ShouldBeTrue();
        Codes(added).ShouldNotContain("DATA-51682");
        fixture.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnOrganizationOwnerIsNotCheckedAgainstTheWorkspaceAccessPolicy()
    {
        var fixture = new Fixture(reads: [[], [], [Stored()], [StoredValue(1, "East"), StoredValue(2, "West")]], workspaceAccess: "hidden");

        var added = await fixture.Service.Add(New(ownerKind: "Organization"), TestContext.Current.CancellationToken);

        added.IsSuccess.ShouldBeTrue(added.CurrentMessage);
        fixture.ViewChecks.ShouldBe(0);
    }

    [Fact]
    public async Task AChangeToAnotherWorkspaceOwnerTheCallerCannotViewIsRefusedAndOneThatKeepsTheOwnerIsNotChecked()
    {
        var other = Guid.Parse("0193a3e0-2222-7d4e-8f50-6a7b8c9d0e1f");
        var refused = new Fixture(reads: [[Stored()], []], workspaceAccess: "hidden");
        var kept = new Fixture(reads: [[Stored()], [], [Stored()], []], workspaceAccess: "hidden");

        var toOther = await refused.Service.Change(new DataConfigurationRuleChange { Key = Key(), OwnerId = other }, TestContext.Current.CancellationToken);
        var sameOwner = await kept.Service.Change(
            new DataConfigurationRuleChange { Key = Key(), OwnerId = Guid.Parse("0193a3e0-1b2c-7d4e-8f50-6a7b8c9d0e1f"), Description = "East division teams" },
            TestContext.Current.CancellationToken);

        Codes(toOther).ShouldContain("DATA-51682");
        refused.Events.ShouldBeEmpty();
        sameOwner.IsSuccess.ShouldBeTrue(sameOwner.CurrentMessage);
        kept.ViewChecks.ShouldBe(0);
    }

    [Fact]
    public async Task AnAddRefusesAPathThatNamesNoLayerBeforeAnythingIsWritten()
    {
        var fixture = new Fixture();
        var rule = New(path: ["DataConfigurationRules", "NoSuchLayer", "In"]);

        var added = await fixture.Service.Add(rule, TestContext.Current.CancellationToken);

        Codes(added).ShouldContain("DATA-31683");
        fixture.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnAddRefusesAValueCountTheKindDoesNotTake()
    {
        var fixture = new Fixture();
        var rule = New(path: ["DataConfigurationRules", "Comparison", "Equal"]);

        var added = await fixture.Service.Add(rule, TestContext.Current.CancellationToken);

        Codes(added).ShouldContain("DATA-21689");
        fixture.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnAddRefusesARuleWithNoNameWithItsOwnCode()
    {
        var fixture = new Fixture();

        var added = await fixture.Service.Add(New(name: null), TestContext.Current.CancellationToken);

        Codes(added).ShouldContain("DATA-21680");
        fixture.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnAddRefusesANameAnotherRuleOfTheColumnHas()
    {
        var fixture = new Fixture(reads: [[Stored()]]);

        var added = await fixture.Service.Add(New(), TestContext.Current.CancellationToken);

        Codes(added).ShouldContain("DATA-41681");
        fixture.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task ARuleThatIsNotThereIsNotFound()
    {
        var fixture = new Fixture(reads: [[]]);

        Codes(await fixture.Service.Get(Key(), TestContext.Current.CancellationToken)).ShouldContain("DATA-31680");
    }

    [Fact]
    public async Task ARemoveRetiresTheRuleMarkedDeletedAndRecordsIt()
    {
        var fixture = new Fixture(reads: [[Stored()], [StoredValue(1, "East")], [Stored(removed: true)], [StoredValue(1, "East")]]);

        var removed = await fixture.Service.Remove(Key(), TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue(removed.CurrentMessage);
        fixture.Events.ShouldBe(["write", "record", "commit"]);
        fixture.Writes.ShouldHaveSingleItem().ShouldBeOfType<ConfigurationRetireCommand>();
        fixture.Recorded.ShouldHaveSingleItem().Single().ChangeKind.ShouldBe("RuleRemoved");
        removed.Value!.Removed.ShouldBeTrue();
    }

    // FDW-7248: the service reads the rule again right after every write. A read kept from before the write must not answer that second read, so the write drops the
    // cached reads of the tables it wrote once it has committed.
    [Fact]
    public async Task ARemoveAnswersTheRemovedRuleAndNotTheReadKeptFromBeforeTheWrite()
    {
        var store = new CachingStore(new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.Ordinal)
        {
            ["DataConfigurationRule"] = [Stored()],
            ["DataConfigurationRuleValue"] = [StoredValue(1, "East")],
        });
        var fixture = new Fixture(store: store);

        var removed = await fixture.Service.Remove(Key(), TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue(removed.CurrentMessage);
        removed.Value!.Removed.ShouldBeTrue();
        fixture.Invalidated.Select(table => table.Container).ShouldBe(["DataConfigurationRule"]);
    }

    [Fact]
    public async Task AnAddDropsTheCachedReadsOfTheRuleTableAndTheValueTableAfterTheCommitAndAFailedWriteDropsNone()
    {
        var added = new Fixture(reads: [[], [], [Stored()], [StoredValue(1, "East"), StoredValue(2, "West")]]);
        (await added.Service.Add(New(), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        added.Invalidated.Select(table => table.Container).Distinct().ShouldBe(["DataConfigurationRule", "DataConfigurationRuleValue"], ignoreOrder: true);

        var failed = new Fixture(reads: [[], []], writes: [true, false]);
        (await failed.Service.Add(New(), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        failed.Invalidated.ShouldBeEmpty();
    }

    // The contract's list carries a total, and a total worked out from "is there a next page" would be a value that was never read: the service counts.
    [Fact]
    public async Task AListCutsTheRequestedPageFromTheMatchingRulesAndCountsAllOfThem()
    {
        var second = Guid.Parse("0193a4f1-7c2e-7b10-9a33-5f0e2b9c4d19");
        var fixture = new Fixture(reads: [[Stored(name: "A"), Stored(id: second, name: "B"), Stored(id: Guid.CreateVersion7(), name: "C")], []]);

        var page = await fixture.Service.List(new DataConfigurationRulesQuery { Target = Target, Removed = false, Skip = 1, Take = 1 }, TestContext.Current.CancellationToken);

        page.IsSuccess.ShouldBeTrue(page.CurrentMessage);
        page.Value!.TotalCount.ShouldBe(3);
        page.Value.Items.ShouldHaveSingleItem().Id.ShouldBe(second);
    }

    // FDW-6571: the history reads the audit for the container's rules and names each record through the catalog and the rule's name.
    [Fact]
    public async Task AHistoryNamesEachRecordedChangeThroughTheCatalogAndAsksTheAuditForExactlyTheContainersRules()
    {
        var fixture = new Fixture(reads: [[Stored()]], history: [Recorded("NameChanged", "East", "West"), Recorded("RuleAdded", null, "East teams")]);

        var page = await fixture.Service.History(new DataConfigurationRuleHistoryQuery { Target = Target, Page = 2, PageSize = 10 }, TestContext.Current.CancellationToken);

        page.IsSuccess.ShouldBeTrue(page.CurrentMessage);
        page.Value!.TotalCount.ShouldBe(2);
        page.Value.Items.Select(entry => entry.Kind.Name).ShouldBe(["NameChanged", "RuleAdded"]);
        page.Value.Items.ShouldAllBe(entry => entry.RuleName == "East teams" && entry.RuleId == RuleId && entry.ByName == "Lee");
        (page.Value.Items[0].OldValue, page.Value.Items[0].NewValue).ShouldBe(("East", "West"));
        var filter = fixture.AuditReads.ShouldHaveSingleItem();
        (filter.ItemKind, filter.Page, filter.PageSize).ShouldBe(("DataConfigurationRule", 2, 10));
        filter.ItemIds.ShouldBe([RuleId.ToString()]);
    }

    [Fact]
    public async Task AHistoryOfAContainerWithNoRulesIsEmptyAndDoesNotReadTheAudit()
    {
        var fixture = new Fixture(reads: [[]]);

        var page = await fixture.Service.History(new DataConfigurationRuleHistoryQuery { Target = Target, Page = 1, PageSize = 10 }, TestContext.Current.CancellationToken);

        page.IsSuccess.ShouldBeTrue(page.CurrentMessage);
        page.Value!.Items.ShouldBeEmpty();
        page.Value.TotalCount.ShouldBe(0);
        fixture.AuditReads.ShouldBeEmpty();
    }

    [Fact]
    public async Task AHistoryWithAKindTheCatalogDoesNotKnowFailsThePageWithItsCode()
    {
        var fixture = new Fixture(reads: [[Stored()]], history: [Recorded("Mystery", null, null)]);

        var page = await fixture.Service.History(new DataConfigurationRuleHistoryQuery { Target = Target, Page = 1, PageSize = 10 }, TestContext.Current.CancellationToken);

        Codes(page).ShouldContain("DATA-91681");
    }

    [Fact]
    public async Task AHistoryOfARuleTheContainerDoesNotHoldFailsThePage()
    {
        var fixture = new Fixture(reads: [[Stored()]], history: [Recorded("NameChanged", "a", "b", ruleId: Guid.CreateVersion7())]);

        var page = await fixture.Service.History(new DataConfigurationRuleHistoryQuery { Target = Target, Page = 1, PageSize = 10 }, TestContext.Current.CancellationToken);

        Codes(page).ShouldContain("DATA-91680");
    }

    [Fact]
    public async Task AHistoryWhoseAuditCannotBeReadFailsWithTheReadsOwnMessageAndNotTheCatalogsCode()
    {
        var fixture = new Fixture(reads: [[Stored()]], auditReadFails: true);

        var page = await fixture.Service.History(new DataConfigurationRuleHistoryQuery { Target = Target, Page = 1, PageSize = 10 }, TestContext.Current.CancellationToken);

        page.IsFailure.ShouldBeTrue();
        Codes(page).ShouldNotContain("DATA-91681");
    }

    // The catalog names every kind of change the service records: one change that touches every member of a rule, and the kinds the other writes record.
    [Fact]
    public async Task TheCatalogNamesEveryKindTheServiceRecords()
    {
        var other = Guid.Parse("0193a3e0-2222-7d4e-8f50-6a7b8c9d0e1f");
        var fixture = new Fixture(reads: [[Stored()], [StoredValue(1, "East")], [], [], [Stored()], [StoredValue(1, "East")]], holders: [Custodian, Stranger], permissions: ["sports:read"]);
        var change = new DataConfigurationRuleChange
        {
            Key = Key(),
            Name = "West teams",
            Abbreviation = "WT",
            Description = "The west",
            Column = "Conference",
            Path = ["DataConfigurationRules", "Comparison", "NotIn"],
            OwnerKind = "Organization",
            OwnerId = other,
            CustodianUserId = Stranger,
            RequiredPermission = "sports:read",
        };

        var changed = await fixture.Service.Change(change, TestContext.Current.CancellationToken);

        changed.IsSuccess.ShouldBeTrue(changed.CurrentMessage);
        var kinds = fixture.Recorded.Single().Select(entry => entry.ChangeKind).ToList();
        kinds.ShouldBe(
            ["NameChanged", "AbbreviationChanged", "DescriptionChanged", "ColumnChanged", "PathChanged", "OwnerKindChanged", "OwnerChanged", "CustodianChanged", "RequiredPermissionChanged"],
            ignoreOrder: true);
        kinds.ShouldAllBe(kind => DataConfigurationRuleCatalog.ChangeKind(kind) != null);

        // The kinds the other writes record, as the tests of those writes assert them.
        foreach (var kind in new[] { "RuleAdded", "ValueAdded", "RuleRemoved", "RuleRestored", "ActiveChanged", "ValueChanged", "ValueRemoved" })
            DataConfigurationRuleCatalog.ChangeKind(kind).ShouldNotBeNull(kind);
    }

    // FDW-6575: a length is what a container declares as a field's MaxLength metadata; a field that declares none is left out, and nothing is invented for it.
    [Fact]
    public async Task TheLimitsAreTheMaximumLengthsTheTwoRuleTablesDeclareByMemberName()
    {
        var fixture = new Fixture(declared: Declared(("Name", 200), ("Abbreviation", 20), ("RequiredPermission", 256L)));

        var limits = await fixture.Service.Limits(TestContext.Current.CancellationToken);

        limits.IsSuccess.ShouldBeTrue(limits.CurrentMessage);
        limits.Value!.Rule.ShouldBe(new Dictionary<string, int> { ["name"] = 200, ["abbreviation"] = 20, ["requiredPermission"] = 256 }, ignoreOrder: true);
        limits.Value.Value.ShouldBe(new Dictionary<string, int> { ["name"] = 200, ["abbreviation"] = 20 }, ignoreOrder: true);
    }

    [Fact]
    public async Task AFieldThatDeclaresNoLengthIsLeftOutOfTheLimits()
    {
        var fixture = new Fixture(declared: new Dictionary<string, IReadOnlyDictionary<string, object>>(StringComparer.Ordinal) { ["Name"] = new Dictionary<string, object>(StringComparer.Ordinal) });

        var limits = await fixture.Service.Limits(TestContext.Current.CancellationToken);

        limits.IsSuccess.ShouldBeTrue(limits.CurrentMessage);
        limits.Value!.Rule.ShouldBeEmpty();
        limits.Value.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task ADeclaredLengthThatIsNotAWholeNumberFailsTheLimitsWithItsCode()
    {
        var fixture = new Fixture(declared: new Dictionary<string, IReadOnlyDictionary<string, object>>(StringComparer.Ordinal)
        {
            ["Name"] = new Dictionary<string, object>(StringComparer.Ordinal) { ["MaxLength"] = "two hundred" },
        });

        var limits = await fixture.Service.Limits(TestContext.Current.CancellationToken);

        Codes(limits).ShouldContain("DATA-91682");
    }

    [Fact]
    public async Task ARuleTableThatCannotBeReachedFailsTheLimitsWithTheProvidersMessageAndNotTheLengthCode()
    {
        var fixture = new Fixture(storeUnreachable: true);

        var limits = await fixture.Service.Limits(TestContext.Current.CancellationToken);

        limits.IsFailure.ShouldBeTrue();
        Codes(limits).ShouldNotContain("DATA-91682");
    }

    private static Dictionary<string, IReadOnlyDictionary<string, object>> Declared(params (string Field, object Length)[] fields)
        => fields.ToDictionary(field => field.Field, field => (IReadOnlyDictionary<string, object>)new Dictionary<string, object>(StringComparer.Ordinal) { ["MaxLength"] = field.Length }, StringComparer.Ordinal);

    [Fact]
    public async Task AListThatDoesNotSayRemovedOrCurrentIsRefused()
    {
        var fixture = new Fixture();

        var page = await fixture.Service.List(new DataConfigurationRulesQuery { Target = Target, Skip = 0, Take = 10 }, TestContext.Current.CancellationToken);

        Codes(page).ShouldContain("DATA-21700");
        fixture.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task APersonWhoIsNotTheCustodianAndHoldsNoPermissionMayNotChangeARule()
    {
        var fixture = new Fixture(reads: [[Stored()], []], caller: Stranger);

        var switched = await fixture.Service.Switch(Key(), false, TestContext.Current.CancellationToken);

        Codes(switched).ShouldContain("DATA-51680");
        fixture.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task AHolderOfTheRulesPermissionMayChangeItWithoutBeingTheCustodian()
    {
        var fixture = new Fixture(
            reads: [[Stored(permission: "sports:custodian")], [], [Stored(permission: "sports:custodian")], []], caller: Stranger, permissions: ["sports:custodian"]);

        var switched = await fixture.Service.Switch(Key(), false, TestContext.Current.CancellationToken);

        switched.IsSuccess.ShouldBeTrue(switched.CurrentMessage);
        fixture.Events.ShouldBe(["write", "record", "commit"]);
    }

    [Fact]
    public async Task ARuleAnswersWhetherTheCallerMayChangeIt()
    {
        var asCustodian = new Fixture(reads: [[Stored()], []]);
        var asStranger = new Fixture(reads: [[Stored()], []], caller: Stranger);

        (await asCustodian.Service.Get(Key(), TestContext.Current.CancellationToken)).Value!.CanChange.ShouldBeTrue();
        (await asStranger.Service.Get(Key(), TestContext.Current.CancellationToken)).Value!.CanChange.ShouldBeFalse();
    }

    [Fact]
    public async Task ARuleCannotRequireAPermissionItsAuthorDoesNotHold()
    {
        var fixture = new Fixture(reads: [], permissions: ["sports:read"]);

        var added = await fixture.Service.Add(New(permission: "sports:custodian"), TestContext.Current.CancellationToken);

        Codes(added).ShouldContain("DATA-51681");
        fixture.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task ACallerWhoIsNotASignedInPersonIsRefusedBeforeAnythingIsRead()
    {
        var fixture = new Fixture(signedIn: false);

        Codes(await fixture.Service.Get(Key(), TestContext.Current.CancellationToken)).ShouldContain("DATA-51683");
        fixture.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task ARuleStatesWhoLastChangedItFromTheNewestRecordedChangeReadOnceForThePage()
    {
        var fixture = new Fixture(reads: [[Stored()], []]);

        var rule = await fixture.Service.Get(Key(), TestContext.Current.CancellationToken);

        rule.Value!.ChangedByUserId.ShouldBe(Stranger);
        var read = fixture.AuditReads.ShouldHaveSingleItem();
        read.ItemKind.ShouldBe("DataConfigurationRule");
        read.ItemIds.ShouldBe([RuleId.ToString()]);
        read.NewestOfEachItem.ShouldBeTrue();
    }

    [Fact]
    public async Task ARuleWithNoRecordedChangeHasNoChangerAndIsNotGivenTheDatabaseLogin()
    {
        var fixture = new Fixture(reads: [[Stored()], []], changeRecorded: false);

        var rule = await fixture.Service.Get(Key(), TestContext.Current.CancellationToken);

        rule.IsSuccess.ShouldBeTrue(rule.CurrentMessage);
        rule.Value!.ChangedByUserId.ShouldBeNull();
    }

    [Fact]
    public async Task AnAuditThatCannotBeReadFailsTheReadAndIsNeverAnsweredAsNoChanger()
    {
        var fixture = new Fixture(reads: [[Stored()], []], auditReadFails: true);

        var rule = await fixture.Service.Get(Key(), TestContext.Current.CancellationToken);

        rule.IsFailure.ShouldBeTrue();
    }

    // The newest recorded change of the rule: made by the stranger, so it is a different person from the custodian a rule names.
    private static AdministrativeChangeRecord Changed() => new(
        Guid.CreateVersion7(), new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero), Stranger, "Lee", "NameChanged", "DataConfigurationRule",
        RuleId.ToString(), "Tenant", Guid.CreateVersion7(), null, null, null, null, null);

    // One recorded change of a rule, as the audit holds it.
    private static AdministrativeChangeRecord Recorded(string changeKind, string? oldValue, string? newValue, Guid? ruleId = null) => new(
        Guid.CreateVersion7(), new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero), Stranger, "Lee", changeKind, "DataConfigurationRule",
        (ruleId ?? RuleId).ToString(), "Tenant", Guid.CreateVersion7(), null, oldValue, newValue, null, null);

    private static DataConfigurationRuleKey Key() => new() { Target = Target, RuleId = RuleId };

    private static List<string?> Codes(IGenericResult result) => [.. result.Messages.Select(message => message.Code)];

    private static NewDataConfigurationRule New(
        string? name = "East teams", string[]? path = null, string? permission = null, string ownerKind = "Workspace", string? description = null, string? valueDescription = null) => new()
    {
        Target = Target,
        Name = name,
        Abbreviation = "ET",
        Description = description,
        Column = "Division",
        Path = path ?? InPath,
        Method = "To",
        OwnerKind = ownerKind,
        OwnerId = Guid.Parse("0193a3e0-1b2c-7d4e-8f50-6a7b8c9d0e1f"),
        CustodianUserId = Guid.Parse("0193a100-aaaa-7bbb-8ccc-0123456789ab"),
        RequiredPermission = permission,
        Active = true,
        Values =
        [
            new NewDataConfigurationRuleValue { Kind = "Literal", Value = "East", Ordinal = 1, Description = valueDescription },
            new NewDataConfigurationRuleValue { Kind = "Literal", Value = "West", Ordinal = 2 },
        ],
    };

    private static Dictionary<string, object?> Stored(bool removed = false, string? permission = null, Guid? id = null, string name = "East teams") => new(StringComparer.Ordinal)
    {
        ["Id"] = id ?? RuleId,
        ["Name"] = name,
        ["Abbreviation"] = "ET",
        ["Description"] = null,
        ["DataStore"] = Target.DataStore,
        ["DataPath"] = Target.DataPath,
        ["Container"] = Target.Container,
        ["Column"] = "Division",
        ["Path"] = "DataConfigurationRules:Comparison:In.To",
        ["OwnerKind"] = "Workspace",
        ["OwnerId"] = Guid.Parse("0193a3e0-1b2c-7d4e-8f50-6a7b8c9d0e1f"),
        ["CustodianUserId"] = Guid.Parse("0193a100-aaaa-7bbb-8ccc-0123456789ab"),
        ["RequiredPermission"] = permission,
        ["Active"] = true,
        ["IsDeleted"] = removed,
        ["ModifyDate"] = new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero),
    };

    private static Dictionary<string, object?> StoredValue(int ordinal, string value) => new(StringComparer.Ordinal)
    {
        ["Id"] = Guid.CreateVersion7(),
        ["DataConfigurationRuleId"] = RuleId,
        ["Kind"] = "Literal",
        ["Value"] = value,
        ["Parameter"] = null,
        ["Ordinal"] = ordinal,
    };

    private sealed class Settings(IReadOnlyDictionary<string, string> values) : IEffectiveSettingsProvider
    {
        public T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null)
            => TryGetEffectiveValue<T>(settingName, out var value, tenantId, roleName) ? value : throw new InvalidOperationException(settingName);

        public bool TryGetEffectiveValue<T>(string settingName, out T value, Guid? tenantId = null, string? roleName = null)
        {
            if (values.TryGetValue(settingName, out var text) && text is T typed)
            {
                value = typed;
                return true;
            }

            value = default!;
            return false;
        }
    }

    // The gateway's read cache as the real one behaves (FDW-7248): a read is kept per container until something drops it, and a write inside a transaction changes
    // the stored rows but drops nothing by itself.
    private sealed class CachingStore(IReadOnlyDictionary<string, List<Dictionary<string, object?>>> rows)
    {
        private readonly Dictionary<string, List<Dictionary<string, object?>>> _kept = new(StringComparer.Ordinal);

        public List<Dictionary<string, object?>> Read(DataStoreTarget table)
        {
            if (!_kept.TryGetValue(table.Container, out var kept))
            {
                kept = [.. rows[table.Container].Select(row => new Dictionary<string, object?>(row, StringComparer.Ordinal))];
                _kept[table.Container] = kept;
            }

            return kept;
        }

        public void Apply(IDataCommand command, DataStoreTarget table)
        {
            if (command is not ConfigurationRetireCommand)
                return;

            foreach (var row in rows[table.Container])
                row["IsDeleted"] = true;
        }

        public void Invalidate(DataStoreTarget table) => _kept.Remove(table.Container);
    }

    // Strict stand-ins that answer: the gateway answers the queued reads, the transaction answers each write from the queue, and the order of
    // write, record, commit and rollback is kept as events.
    // FDW-6208: a rule's description, and each value's, is a row of the descriptions table and not a column of either rule table. The service writes it in the unit of the write
    // (the same unit as the rule's rows, before the audit record, so the three commit or roll back together) and fills the answer from the table.
    [Fact]
    public async Task AnAddDescribesTheRuleAndEachValueInTheUnitAfterTheirRowsAndBeforeTheAuditRecord()
    {
        var fixture = new Fixture(reads: [[], [], [Stored()], [StoredValue(1, "East"), StoredValue(2, "West")]], traceDescriptions: true);

        var added = await fixture.Service.Add(New(description: "East division teams", valueDescription: "The eastern division"), TestContext.Current.CancellationToken);

        added.IsSuccess.ShouldBeTrue(added.CurrentMessage);
        fixture.Events.ShouldBe(["write", "describe", "write", "describe", "write", "describe", "record", "commit"]);
        fixture.Described.Count.ShouldBe(3);
        fixture.Described[0].ShouldEndWith("East teams = East division teams");
        fixture.Described[1].ShouldEndWith("= The eastern division");
        fixture.Described[1].ShouldContain("owner ");
        // The second value states no text, so what it had is removed: nothing is invented and nothing is left behind.
        fixture.Described[2].ShouldEndWith("removed");
        // No write names a Description: the records no longer store it.
        typeof(DataConfigurationRuleRecord).GetProperty("Description").ShouldNotBeNull().GetCustomAttributes(typeof(Fdw.Data.NotStoredAttribute), false).ShouldNotBeEmpty();
        typeof(DataConfigurationRuleValueRecord).GetProperty("Description").ShouldNotBeNull().GetCustomAttributes(typeof(Fdw.Data.NotStoredAttribute), false).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ADescriptionTheDescriptionsTableRefusesRollsTheWholeAddBackAndRecordsNothing()
    {
        var fixture = new Fixture(reads: [[], []], descriptionWriteFails: true);

        var added = await fixture.Service.Add(New(description: "East division teams"), TestContext.Current.CancellationToken);

        added.IsFailure.ShouldBeTrue();
        added.Messages.Select(message => message.Message).ShouldContain(message => message.Contains("description refused", StringComparison.Ordinal));
        fixture.Events.ShouldContain("rollback");
        fixture.Events.ShouldNotContain("commit");
        fixture.Recorded.ShouldBeEmpty();
    }

    [Fact]
    public async Task AChangeOfTheDescriptionDescribesTheRuleUnderItsNameInTheUnitOfTheChange()
    {
        var fixture = new Fixture(reads: [[Stored()], [], [Stored()], []]);

        var changed = await fixture.Service.Change(new DataConfigurationRuleChange { Key = Key(), Description = "East division teams" }, TestContext.Current.CancellationToken);

        changed.IsSuccess.ShouldBeTrue(changed.CurrentMessage);
        fixture.Described.ShouldBe([$"rule {RuleId:D} East teams = East division teams"]);
    }

    [Fact]
    public async Task ASwitchChangesNoTextSoItWritesNoDescription()
    {
        var fixture = new Fixture(reads: [[Stored()], [], [Stored()], []]);

        var switched = await fixture.Service.Switch(Key(), false, TestContext.Current.CancellationToken);

        switched.IsSuccess.ShouldBeTrue(switched.CurrentMessage);
        fixture.Described.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddedValuesAreDescribedWithTheRuleAsTheirOwner()
    {
        var fixture = new Fixture(reads: [[Stored()], [], [Stored()], [StoredValue(1, "North")]]);
        var north = new NewDataConfigurationRuleValue { Kind = "Literal", Value = "North", Ordinal = 1, Description = "The northern division" };

        var added = await fixture.Service.AddValues(Key(), [north], TestContext.Current.CancellationToken);

        added.IsSuccess.ShouldBeTrue(added.CurrentMessage);
        fixture.Described.ShouldHaveSingleItem().ShouldEndWith($"North owner {RuleId:D} = The northern division");
    }

    [Fact]
    public async Task AChangedValueIsDescribedAgainByItsOwnId()
    {
        var value = StoredValue(1, "East");
        var valueId = (Guid)value["Id"]!;
        var fixture = new Fixture(reads: [[Stored()], [value], [Stored()], [value]]);

        var changed = await fixture.Service.ChangeValue(new DataConfigurationRuleValueChange { Key = Key(), ValueId = valueId, Description = "The eastern side" }, TestContext.Current.CancellationToken);

        changed.IsSuccess.ShouldBeTrue(changed.CurrentMessage);
        fixture.Described.ShouldHaveSingleItem().ShouldBe($"value {valueId:D} East owner {RuleId:D} = The eastern side");
    }

    [Fact]
    public async Task ARuleAndItsValuesAnswerTheTextsTheDescriptionsTableHolds()
    {
        var value = StoredValue(1, "East");
        var valueId = (Guid)value["Id"]!;
        var fixture = new Fixture(
            reads: [[Stored()], [value]], holders: [Custodian],
            ruleTexts: new Dictionary<Guid, string> { [RuleId] = "East division teams" }, valueTexts: new Dictionary<Guid, string> { [valueId] = "The eastern division" });

        var rule = await fixture.Service.Get(Key(), TestContext.Current.CancellationToken);

        rule.IsSuccess.ShouldBeTrue(rule.CurrentMessage);
        rule.Value!.Description.ShouldBe("East division teams");
        rule.Value.Values.ShouldHaveSingleItem().Description.ShouldBe("The eastern division");
    }

    [Fact]
    public async Task ARuleNobodyDescribedAnswersNoDescription()
    {
        var fixture = new Fixture(reads: [[Stored()], [StoredValue(1, "East")]], holders: [Custodian]);

        var rule = await fixture.Service.Get(Key(), TestContext.Current.CancellationToken);

        rule.IsSuccess.ShouldBeTrue(rule.CurrentMessage);
        rule.Value!.Description.ShouldBeNull();
        rule.Value.Values.ShouldHaveSingleItem().Description.ShouldBeNull();
    }

    private sealed class Fixture
    {
        private readonly Queue<IReadOnlyList<Dictionary<string, object?>>> _reads;
        private readonly Queue<bool> _writes;

        public Fixture(
            IReadOnlyList<IReadOnlyList<Dictionary<string, object?>>>? reads = null,
            IReadOnlyList<bool>? writes = null,
            IDictionary<string, string>? settings = null,
            bool auditFails = false,
            bool transactional = true,
            Guid? caller = null,
            string[]? permissions = null,
            bool signedIn = true,
            bool changeRecorded = true,
            bool auditReadFails = false,
            IReadOnlyList<Guid>? holders = null,
            bool holdersFail = false,
            bool workspaceFound = true,
            string workspaceAccess = "view",
            IReadOnlyList<AdministrativeChangeRecord>? history = null,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>? declared = null,
            bool storeUnreachable = false,
            CachingStore? store = null,
            IReadOnlyDictionary<Guid, string>? ruleTexts = null,
            IReadOnlyDictionary<Guid, string>? valueTexts = null,
            bool descriptionWriteFails = false,
            bool traceDescriptions = false)
        {
            _reads = new Queue<IReadOnlyList<Dictionary<string, object?>>>(reads ?? []);
            _writes = new Queue<bool>(writes ?? []);

            var transaction = new Mock<IDataGatewayTransaction>(MockBehavior.Strict);
            transaction
                .Setup(t => t.Execute<ConfigurationWriteResult>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .Callback<IDataCommand, DataStoreTarget, CancellationToken>((command, table, _) =>
                {
                    Writes.Add(command);
                    Events.Add("write");
                    store?.Apply(command, table);
                })
                .ReturnsAsync(() => _writes.Count == 0 || _writes.Dequeue()
                    ? GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], new Dictionary<string, object?>()))
                    : GenericResult<ConfigurationWriteResult>.Failure(DataOptionRulesLog.RuleTablesRefusedWrite(NullLogger.Instance, "test", "Rule", Store)));
            transaction.Setup(t => t.Commit(It.IsAny<CancellationToken>())).Callback(() => Events.Add("commit")).ReturnsAsync(GenericResult.Success());
            transaction.Setup(t => t.Rollback(It.IsAny<CancellationToken>())).Callback(() => Events.Add("rollback")).ReturnsAsync(GenericResult.Success());
            transaction.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);

            var gateway = new Mock<IConfigurationGateway>(MockBehavior.Strict);
            gateway
                .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IDataCommand _, DataStoreTarget table, CancellationToken _) => GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(
                    store is null ? _reads.Dequeue() : store.Read(table)));
            gateway
                .Setup(g => g.InvalidateCachedResults(It.IsAny<DataStoreTarget>()))
                .Callback<DataStoreTarget>(table =>
                {
                    Invalidated.Add(table);
                    store?.Invalidate(table);
                });
            gateway
                .Setup(g => g.BeginTransaction(Store, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => transactional
                    ? GenericResult<IDataGatewayTransaction>.Success(transaction.Object)
                    : GenericResult<IDataGatewayTransaction>.Failure(DataOptionRulesLog.RuleTablesRefusedWrite(NullLogger.Instance, "begin", "Rule", Store)));
            var gateways = new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict);
            gateways.Setup(p => p.Get(Store)).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));

            var audit = new Mock<IAdministrativeAudit>(MockBehavior.Strict);
            audit
                .Setup(a => a.RecordAll(It.IsAny<IReadOnlyList<AdministrativeChangeEntry>>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyList<AdministrativeChangeEntry>, CancellationToken>((entries, _) =>
                {
                    Recorded.Add(entries);
                    Events.Add("record");
                })
                .ReturnsAsync(() => auditFails
                    ? GenericResult.Failure(DataOptionRulesLog.RuleTablesRefusedWrite(NullLogger.Instance, "audit", "Rule", Store))
                    : GenericResult.Success());

            audit
                .Setup(a => a.Read(It.IsAny<AdministrativeChangeFilter>(), It.IsAny<CancellationToken>()))
                .Callback<AdministrativeChangeFilter, CancellationToken>((filter, _) => AuditReads.Add(filter))
                .ReturnsAsync(() => auditReadFails
                    ? GenericResult<IPagedResponse<AdministrativeChangeRecord>>.Failure(DataOptionRulesLog.RuleTablesRefusedWrite(NullLogger.Instance, "audit read", "Rule", Store))
                    : GenericResult<IPagedResponse<AdministrativeChangeRecord>>.Success(new PagedResponse<AdministrativeChangeRecord>(history ?? (changeRecorded ? [Changed()] : []), 1, 1, (history ?? (changeRecorded ? [Changed()] : [])).Count)));

            var context = new Mock<IAuthenticationContext>(MockBehavior.Strict);
            context.Setup(c => c.IsAuthenticated).Returns(true);
            context.Setup(c => c.UserId).Returns((caller ?? Custodian).ToString("D"));
            context.Setup(c => c.Permissions).Returns(permissions ?? []);
            var auth = new Mock<IAuthenticationContextAccessor>(MockBehavior.Strict);
            auth.SetupGet(a => a.Current).Returns(signedIn ? context.Object : null);

            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            tenant.Setup(t => t.TenantId).Returns(Guid.CreateVersion7());

            var permissionHolders = new Mock<IPermissionHolders>(MockBehavior.Strict);
            permissionHolders
                .Setup(h => h.Get("datastores:write", It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => holdersFail
                    ? GenericResult<IReadOnlyList<Guid>>.Failure(DataOptionRulesLog.RuleTablesRefusedWrite(NullLogger.Instance, "holders", "Rule", Store))
                    : GenericResult<IReadOnlyList<Guid>>.Success(holders ?? [Custodian]));

            var workspaces = new Mock<IWorkspaceConfigurationProvider>(MockBehavior.Strict);
            workspaces
                .Setup(w => w.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, CancellationToken _) => workspaceFound
                    ? GenericResult<IWorkspaceImplementationConfiguration>.Success(new WorkspaceImplementationConfiguration { Id = id, Name = "Scouting" })
                    : GenericResult<IWorkspaceImplementationConfiguration>.Failure(DataOptionRulesLog.OwnerNotFound(NullLogger.Instance, "Workspace", id.ToString("D"))));
            var access = new Mock<IWorkspaceAccessPolicy>(MockBehavior.Strict);
            access
                .Setup(a => a.MayView(It.IsAny<IWorkspaceImplementationConfiguration>(), It.IsAny<CancellationToken>()))
                .Callback<IWorkspaceImplementationConfiguration, CancellationToken>((_, _) => ViewChecks++)
                .ReturnsAsync(() => workspaceAccess switch
                {
                    "view" => GenericResult.Success(),
                    "undetermined" => GenericResult.Failure(WorkspacesResultCodes.ByName("WorkspaceAccessUndetermined"), NullLogger.Instance),
                    _ => GenericResult.Failure(WorkspacesResultCodes.ByName("WorkspaceNotVisible"), NullLogger.Instance),
                });

            // The containers' declared fields: each container name answers the fields it declares with their metadata (the maximum length of a field).
            var stores = new Mock<IDataStoreProvider>(MockBehavior.Strict);
            stores
                .Setup(provider => provider.Get(Store, "data", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string _, string _, string container, CancellationToken _) =>
                {
                    if (storeUnreachable)
                        return GenericResult<IDataContainer>.Failure(DataOptionRulesLog.RuleTablesUnreadable(NullLogger.Instance, container, Store));

                    var schema = new Mock<IContainerSchema>(MockBehavior.Strict);
                    schema.SetupGet(c => c.Fields).Returns([.. (declared ?? new Dictionary<string, IReadOnlyDictionary<string, object>>()).Select(field =>
                    {
                        var f = new Mock<IField>(MockBehavior.Strict);
                        f.SetupGet(x => x.Name).Returns(field.Key);
                        f.SetupGet(x => x.Metadata).Returns(field.Value);
                        return f.Object;
                    })]);
                    var rowContainer = new Mock<IDataContainer>(MockBehavior.Strict);
                    rowContainer.SetupGet(c => c.Schema).Returns(schema.Object);
                    return GenericResult<IDataContainer>.Success(rowContainer.Object);
                });

            // The descriptions writer and reader (FDW-6208), strict: they state their answers. A write is noted in Described (and, when a test traces it, in Events, so its place
            // among the unit's writes shows); a read answers the texts the test holds, by the Id of the rule or of the value.
            var describeResult = descriptionWriteFails
                ? GenericResult.Failure(new Fdw.Messages.GenericMessage("description refused"))
                : GenericResult.Success();
            void Noted(string note)
            {
                Described.Add(note);
                if (traceDescriptions)
                    Events.Add("describe");
            }

            var writer = new Mock<IDescriptionWriter>(MockBehavior.Strict);
            writer.Setup(w => w.Language()).Returns(GenericResult<string>.Success("en-US"));
            writer
                .Setup(w => w.Describe("DataConfigurationRule", "Text", It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, Guid, string, string?, ConfigurationWriteScope, CancellationToken>((_, _, id, name, text, _, _) => Noted($"rule {id:D} {name} = {text}"))
                .ReturnsAsync(describeResult);
            writer
                .Setup(w => w.Save(It.Is<DescriptionKey>(k => k.Type == "DataConfigurationRuleValue"), It.IsAny<string>(), It.IsAny<DescriptionOwner?>(), It.IsAny<string>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
                .Callback<DescriptionKey, string, DescriptionOwner?, string, ConfigurationWriteScope, CancellationToken>((key, name, owner, text, _, _) => Noted($"value {key.ThingId:D} {name} owner {owner?.Id:D} = {text}"))
                .ReturnsAsync(describeResult);
            writer
                .Setup(w => w.Delete(It.Is<DescriptionKey>(k => k.Type == "DataConfigurationRuleValue"), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
                .Callback<DescriptionKey, ConfigurationWriteScope, CancellationToken>((key, _, _) => Noted($"value {key.ThingId:D} removed"))
                .ReturnsAsync(describeResult);
            var reader = new Mock<IDescriptionReader>(MockBehavior.Strict);
            reader
                .Setup(r => r.ReadMany("DataConfigurationRule", "Text", It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string type, string sub, IReadOnlyCollection<Guid> ids, CancellationToken _) => GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(
                    ids.Where(id => ruleTexts?.ContainsKey(id) == true)
                        .ToDictionary(id => id, id => new DescriptionEntry(new DescriptionKey(type, sub, id, "en-US"), "rule", null, ruleTexts![id]))));
            reader
                .Setup(r => r.ReadOwnedBy("DataConfigurationRuleValue", "Text", It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string type, string sub, IReadOnlyCollection<Guid> owners, CancellationToken _) => GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(
                    (valueTexts ?? new Dictionary<Guid, string>()).ToDictionary(
                        held => held.Key, held => new DescriptionEntry(new DescriptionKey(type, sub, held.Key, "en-US"), "value", new DescriptionOwner(owners.First(), "rule"), held.Value))));

            Service = new DataConfigurationRuleService(
                NullLogger<DataConfigurationRuleService>.Instance, gateways.Object, new Settings((IReadOnlyDictionary<string, string>)(settings ?? Defaults())), tenant.Object, audit.Object, auth.Object, permissionHolders.Object, workspaces.Object, access.Object, stores.Object,
                writer.Object, reader.Object);
        }

        // The descriptions the service wrote, one note each, in the order it wrote them.
        public List<string> Described { get; } = [];

        public DataConfigurationRuleService Service { get; }

        public int ViewChecks { get; private set; }

        public List<IDataCommand> Writes { get; } = [];

        public List<string> Events { get; } = [];

        // The targets whose cached reads were dropped (the scope's commit does it after a commit; a raw transaction does not).
        public List<DataStoreTarget> Invalidated { get; } = [];

        public List<AdministrativeChangeFilter> AuditReads { get; } = [];

        public List<IReadOnlyList<AdministrativeChangeEntry>> Recorded { get; } = [];

        public static Dictionary<string, string> Defaults() => new(StringComparer.Ordinal)
        {
            [SettingDefinitions.DataConfigurationRuleDataStore] = Store,
            [SettingDefinitions.DataConfigurationRulePath] = "data",
            [SettingDefinitions.DataConfigurationRuleContainer] = "DataConfigurationRule",
            [SettingDefinitions.DataConfigurationRuleValueContainer] = "DataConfigurationRuleValue",
        };
    }
}
