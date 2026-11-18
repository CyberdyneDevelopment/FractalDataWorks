using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.Options;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Logging;
using Fdw.Services.Multitenancy;
using Fdw.Services.Settings;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Results;
using Fdw.Web.Endpoints.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6489: the thirteen data option rule routes the rule service backs answer through it. Each is checked over strict stand-ins for the service and the names directory: what it
/// asks of the service, what it answers, and the status and code of each refusal. A route never answers a name it was not given, and a body it cannot read is refused before the
/// service is called.
/// </summary>
public sealed class DataOptionRuleRoutesAnswerTests
{
    private const string Store = "NflDb";
    private const string StorePath = "NflData";
    private const string Container = "Teams";

    private static readonly Guid RuleId = Guid.Parse("0193a4f1-7c2e-7b10-9a33-5f0e2b9c4d18");
    private static readonly Guid ValueId = Guid.Parse("0193a4f1-7c2e-7b10-9a33-5f0e2b9c4d77");
    private static readonly Guid Custodian = Guid.Parse("0193a100-aaaa-7bbb-8ccc-0123456789ab");
    private static readonly Guid Changer = Guid.Parse("0193a100-bbbb-7bbb-8ccc-0123456789ab");
    private static readonly Guid OwnerId = Guid.Parse("0193a3e0-1b2c-7d4e-8f50-6a7b8c9d0e1f");

    private readonly Mock<IDataConfigurationRules> _rules = new(MockBehavior.Strict);
    private readonly Mock<IDataConfigurationRuleNames> _names = new(MockBehavior.Strict);

    [Fact]
    public async Task AGetAnswersTheRuleWithTheNamesOfItsOwnerCustodianAndChanger()
    {
        _rules.Setup(r => r.Get(It.Is<DataConfigurationRuleKey>(k => k.RuleId == RuleId && k.Target.Container == Container), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataConfigurationRule>.Success(Rule()));
        NamesAnswer();

        var (status, rule) = await Call<GetRule, DataOptionRuleKeyRequest, DataOptionRulePayload>([_rules.Object, _names.Object], Key());

        status.ShouldBe(200);
        rule.ShouldNotBeNull().Name.ShouldBe("East teams");
        rule.Owner.Name.ShouldBe("Scouting");
        rule.Owner.Kind.ShouldBe("Workspace");
        rule.CustodianName.ShouldBe("Dana");
        rule.CustodianState.ShouldBe("Holder");
        rule.ChangedByUserId.ShouldBe(Changer);
        rule.ChangedBy.ShouldBe("Lee");
        rule.CanChange.ShouldBeTrue();
        rule.Values.ShouldHaveSingleItem().Value.ShouldBe("East");
    }

    [Fact]
    public async Task ARuleWithNoRecordedChangeStatesNoChangerNotAnEmptyName()
    {
        _rules.Setup(r => r.Get(It.IsAny<DataConfigurationRuleKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<DataConfigurationRule>.Success(Rule(changed: false)));
        NamesAnswer();

        var (_, rule) = await Call<GetRule, DataOptionRuleKeyRequest, DataOptionRulePayload>([_rules.Object, _names.Object], Key());

        rule.ShouldNotBeNull().ChangedByUserId.ShouldBeNull();
        rule.ChangedBy.ShouldBeNull();
    }

    [Fact]
    public async Task AServiceRefusalIsAnsweredWithItsOwnStatusAndTheNamesAreNotRead()
    {
        _rules.Setup(r => r.Get(It.IsAny<DataConfigurationRuleKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataConfigurationRule>.Failure(DataOptionRulesLog.RuleNotFound(NullLogger.Instance, RuleId.ToString(), Container)));

        var (status, _) = await Call<GetRule, DataOptionRuleKeyRequest, DataOptionRulePayload>([_rules.Object, _names.Object], Key());

        status.ShouldBe(404);
        _names.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ANameThatCannotBeFoundFailsTheAnswerWithTheDirectorysCodeAndIsNeverAnEmptyName()
    {
        _rules.Setup(r => r.Get(It.IsAny<DataConfigurationRuleKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<DataConfigurationRule>.Success(Rule()));
        _names.Setup(n => n.Users(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, string>>.Failure(DataOptionRulesLog.UserNotFound(NullLogger.Instance, Custodian.ToString("D"))));

        var (status, rule) = await Call<GetRule, DataOptionRuleKeyRequest, DataOptionRulePayload>([_rules.Object, _names.Object], Key());

        status.ShouldBe(404);
        rule.ShouldBeNull();
    }

    [Fact]
    public async Task AnAddAsksTheServiceForTheRuleTheBodyDescribesAndAnswersCreated()
    {
        NewDataConfigurationRule? asked = null;
        _rules.Setup(r => r.Add(It.IsAny<NewDataConfigurationRule>(), It.IsAny<CancellationToken>()))
            .Callback<NewDataConfigurationRule, CancellationToken>((rule, _) => asked = rule)
            .ReturnsAsync(GenericResult<DataConfigurationRule>.Success(Rule()));
        NamesAnswer();
        var request = new AddDataOptionRuleRequest
        {
            Name = Store,
            PathName = StorePath,
            ContainerName = Container,
            Rule = new NewDataOptionRuleBody
            {
                Name = "East teams",
                Abbreviation = "ET",
                Column = "Division",
                Path = ["DataConfigurationRules", "Comparison", "In"],
                Method = "To",
                Owner = new DataOptionOwnerBody { Kind = "Workspace", Id = OwnerId },
                CustodianUserId = Custodian,
                Active = true,
                Values = [new DataOptionValueBody { Kind = "Literal", Value = "East", Ordinal = 1 }],
            },
        };

        var (status, rule) = await Call<AddRule, AddDataOptionRuleRequest, DataOptionRulePayload>([_rules.Object, _names.Object], request);

        status.ShouldBe(201);
        rule.ShouldNotBeNull().Id.ShouldBe(RuleId);
        var sent = asked.ShouldNotBeNull();
        sent.Target.DataStore.ShouldBe(Store);
        sent.Target.DataPath.ShouldBe(StorePath);
        sent.Target.Container.ShouldBe(Container);
        sent.Name.ShouldBe("East teams");
        sent.Column.ShouldBe("Division");
        sent.Path.ShouldBe(["DataConfigurationRules", "Comparison", "In"]);
        sent.OwnerKind.ShouldBe("Workspace");
        sent.OwnerId.ShouldBe(OwnerId);
        sent.CustodianUserId.ShouldBe(Custodian);
        sent.Active.ShouldBe(true);
        sent.Values.ShouldNotBeNull().ShouldHaveSingleItem().Value.ShouldBe("East");
    }

    [Fact]
    public async Task AnAddWithNoBodyIsRefusedWithItsCodeBeforeTheServiceIsCalled()
    {
        var (status, _) = await Call<AddRule, AddDataOptionRuleRequest, DataOptionRulePayload>([_rules.Object, _names.Object], new AddDataOptionRuleRequest { Name = Store, PathName = StorePath, ContainerName = Container });

        status.ShouldBe(400);
        _rules.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AnAddWhoseBodyNamesAnotherContainerThanTheRouteIsRefusedBeforeTheServiceIsCalled()
    {
        var request = new AddDataOptionRuleRequest
        {
            Name = Store,
            PathName = StorePath,
            ContainerName = Container,
            Rule = new NewDataOptionRuleBody { Container = "Players" },
        };

        var (status, _) = await Call<AddRule, AddDataOptionRuleRequest, DataOptionRulePayload>([_rules.Object, _names.Object], request);

        status.ShouldBe(400);
        _rules.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AChangeAsksTheServiceForTheFieldsTheBodyNamesAndWhatItClears()
    {
        DataConfigurationRuleChange? asked = null;
        _rules.Setup(r => r.Change(It.IsAny<DataConfigurationRuleChange>(), It.IsAny<CancellationToken>()))
            .Callback<DataConfigurationRuleChange, CancellationToken>((change, _) => asked = change)
            .ReturnsAsync(GenericResult<DataConfigurationRule>.Success(Rule()));
        NamesAnswer();
        var request = new ChangeDataOptionRuleRequest
        {
            Name = Store,
            PathName = StorePath,
            ContainerName = Container,
            RuleId = RuleId,
            Change = new DataOptionRuleChangeBody { Name = "West teams", CustodianUserId = Custodian, Clear = ["description"] },
        };

        var (status, _) = await Call<ChangeRule, ChangeDataOptionRuleRequest, DataOptionRulePayload>([_rules.Object, _names.Object], request);

        status.ShouldBe(200);
        var sent = asked.ShouldNotBeNull();
        sent.Key.RuleId.ShouldBe(RuleId);
        sent.Name.ShouldBe("West teams");
        sent.CustodianUserId.ShouldBe(Custodian);
        sent.Clear.ShouldBe(["description"]);
        sent.Column.ShouldBeNull();
    }

    [Fact]
    public async Task ASwitchPassesTheStateAndASwitchWithNoStateIsRefusedBeforeTheServiceIsCalled()
    {
        _rules.Setup(r => r.Switch(It.Is<DataConfigurationRuleKey>(k => k.RuleId == RuleId), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataConfigurationRule>.Success(Rule()));
        NamesAnswer();
        var routed = new SwitchDataOptionRuleRequest { Name = Store, PathName = StorePath, ContainerName = Container, RuleId = RuleId, Switch = new DataOptionRuleSwitchBody { Active = false } };
        var empty = new SwitchDataOptionRuleRequest { Name = Store, PathName = StorePath, ContainerName = Container, RuleId = RuleId, Switch = new DataOptionRuleSwitchBody() };

        var (done, _) = await Call<SwitchRule, SwitchDataOptionRuleRequest, DataOptionRulePayload>([_rules.Object, _names.Object], routed);
        var (refused, _) = await Call<SwitchRule, SwitchDataOptionRuleRequest, DataOptionRulePayload>([_rules.Object, _names.Object], empty);

        done.ShouldBe(200);
        refused.ShouldBe(400);
        _rules.Verify(r => r.Switch(It.IsAny<DataConfigurationRuleKey>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ARemoveAndARestoreAskTheServiceForTheRuleTheRouteNames()
    {
        _rules.Setup(r => r.Remove(It.Is<DataConfigurationRuleKey>(k => k.RuleId == RuleId), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<DataConfigurationRule>.Success(Rule()));
        _rules.Setup(r => r.Restore(It.Is<DataConfigurationRuleKey>(k => k.RuleId == RuleId), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<DataConfigurationRule>.Success(Rule()));
        NamesAnswer();

        var (removed, _) = await Call<RemoveRule, DataOptionRuleKeyRequest, DataOptionRulePayload>([_rules.Object, _names.Object], Key());
        var (restored, _) = await Call<RestoreRule, DataOptionRuleKeyRequest, DataOptionRulePayload>([_rules.Object, _names.Object], Key());

        removed.ShouldBe(200);
        restored.ShouldBe(200);
    }

    [Fact]
    public async Task AddingValuesPassesEachValueAndABodyWithNoneIsRefusedBeforeTheServiceIsCalled()
    {
        IReadOnlyList<NewDataConfigurationRuleValue>? asked = null;
        _rules.Setup(r => r.AddValues(It.Is<DataConfigurationRuleKey>(k => k.RuleId == RuleId), It.IsAny<IReadOnlyList<NewDataConfigurationRuleValue>>(), It.IsAny<CancellationToken>()))
            .Callback<DataConfigurationRuleKey, IReadOnlyList<NewDataConfigurationRuleValue>, CancellationToken>((_, values, _) => asked = values)
            .ReturnsAsync(GenericResult<DataConfigurationRule>.Success(Rule()));
        NamesAnswer();
        var request = new AddDataOptionValuesRequest
        {
            Name = Store,
            PathName = StorePath,
            ContainerName = Container,
            RuleId = RuleId,
            Values = new DataOptionValuesBody { Values = [new DataOptionValueBody { Kind = "Parameter", Parameter = "Division" }] },
        };
        var bodiless = new AddDataOptionValuesRequest { Name = Store, PathName = StorePath, ContainerName = Container, RuleId = RuleId };

        var (done, _) = await Call<AddValues, AddDataOptionValuesRequest, DataOptionRulePayload>([_rules.Object, _names.Object], request);
        var (refused, _) = await Call<AddValues, AddDataOptionValuesRequest, DataOptionRulePayload>([_rules.Object, _names.Object], bodiless);

        done.ShouldBe(200);
        asked.ShouldNotBeNull().ShouldHaveSingleItem().Parameter.ShouldBe("Division");
        refused.ShouldBe(400);
    }

    [Fact]
    public async Task ChangingAValuePassesItsIdAndWhatItClears()
    {
        DataConfigurationRuleValueChange? asked = null;
        _rules.Setup(r => r.ChangeValue(It.IsAny<DataConfigurationRuleValueChange>(), It.IsAny<CancellationToken>()))
            .Callback<DataConfigurationRuleValueChange, CancellationToken>((change, _) => asked = change)
            .ReturnsAsync(GenericResult<DataConfigurationRule>.Success(Rule()));
        NamesAnswer();
        var request = new ChangeDataOptionValueRequest
        {
            Name = Store,
            PathName = StorePath,
            ContainerName = Container,
            RuleId = RuleId,
            ValueId = ValueId,
            Change = new DataOptionValueChangeBody { Value = "West", Clear = ["name"] },
        };

        var (status, _) = await Call<ChangeValue, ChangeDataOptionValueRequest, DataOptionRulePayload>([_rules.Object, _names.Object], request);

        status.ShouldBe(200);
        var sent = asked.ShouldNotBeNull();
        sent.ValueId.ShouldBe(ValueId);
        sent.Key.RuleId.ShouldBe(RuleId);
        sent.Value.ShouldBe("West");
        sent.Clear.ShouldBe(["name"]);
    }

    [Fact]
    public async Task RemovingValuesPassesTheIdsTheRequestNames()
    {
        _rules.Setup(r => r.RemoveValues(It.Is<DataConfigurationRuleKey>(k => k.RuleId == RuleId), It.Is<IReadOnlyList<Guid>>(ids => ids.SequenceEqual(new[] { ValueId })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataConfigurationRule>.Success(Rule()));
        NamesAnswer();
        var request = new RemoveDataOptionValuesRequest { Name = Store, PathName = StorePath, ContainerName = Container, RuleId = RuleId, Id = [ValueId] };

        var (status, _) = await Call<RemoveValues, RemoveDataOptionValuesRequest, DataOptionRulePayload>([_rules.Object, _names.Object], request);

        status.ShouldBe(200);
    }

    [Fact]
    public async Task AListCutsItsWindowFromThePageAndPageSizeAndStatesTheTotalTheServiceCounted()
    {
        DataConfigurationRulesQuery? asked = null;
        _rules.Setup(r => r.List(It.IsAny<DataConfigurationRulesQuery>(), It.IsAny<CancellationToken>()))
            .Callback<DataConfigurationRulesQuery, CancellationToken>((query, _) => asked = query)
            .ReturnsAsync(GenericResult<DataConfigurationRulesPage>.Success(new DataConfigurationRulesPage { Items = [Rule()], TotalCount = 25 }));
        NamesAnswer();
        var request = new ListDataOptionRulesRequest { Name = Store, PathName = StorePath, ContainerName = Container, Removed = false, Page = 2, PageSize = 10 };

        var (status, page) = await Call<ListRules, ListDataOptionRulesRequest, PaginatedResponse<DataOptionRulePayload>>([_rules.Object, _names.Object], request, WithPaging);

        status.ShouldBe(200);
        var query = asked.ShouldNotBeNull();
        query.Skip.ShouldBe(10);
        query.Take.ShouldBe(10);
        query.Removed.ShouldBe(false);
        query.Target.Container.ShouldBe(Container);
        page.ShouldNotBeNull().TotalCount.ShouldBe(25);
        page.Page.ShouldBe(2);
        page.PageSize.ShouldBe(10);
        page.Items.ShouldHaveSingleItem().CustodianName.ShouldBe("Dana");
    }

    [Fact]
    public async Task AListWithAPageBelowOneIsRefusedBeforeTheServiceIsCalled()
    {
        var request = new ListDataOptionRulesRequest { Name = Store, PathName = StorePath, ContainerName = Container, Removed = false, Page = 0 };

        var (status, _) = await Call<ListRules, ListDataOptionRulesRequest, PaginatedResponse<DataOptionRulePayload>>([_rules.Object, _names.Object], request, WithPaging);

        status.ShouldBe(400);
        _rules.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TheCustodianPickerListsTheHoldersByNameKeptByTheSearchAndPaged()
    {
        var third = Guid.Parse("0193a100-cccc-7bbb-8ccc-0123456789ab");
        _names.Setup(n => n.Custodians(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, string>>.Success(
            new Dictionary<Guid, string> { [Custodian] = "Dana", [Changer] = "Lee", [third] = "Danny" }));
        var request = new ListDataOptionCustodiansRequest { Search = "dan", Page = 1, PageSize = 1 };

        var (status, page) = await Call<ListCustodians, ListDataOptionCustodiansRequest, PaginatedResponse<DataOptionCustodianPayload>>([_names.Object], request, WithPaging);

        status.ShouldBe(200);
        page.ShouldNotBeNull().TotalCount.ShouldBe(2);
        page.Items.ShouldHaveSingleItem().Name.ShouldBe("Dana");
    }

    [Fact]
    public async Task TheOwnerPickerListsTheViewableWorkspacesNamedByTheDirectoryAndRefusesAKindLeftOut()
    {
        var hidden = Guid.Parse("0193a3e0-2222-7d4e-8f50-6a7b8c9d0e1f");
        var workspaces = new Mock<IWorkspaceConfigurationProvider>(MockBehavior.Strict);
        workspaces.Setup(w => w.Get(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyList<IWorkspaceImplementationConfiguration>>.Success(
            [new WorkspaceImplementationConfiguration { Id = OwnerId, Name = "Scouting" }, new WorkspaceImplementationConfiguration { Id = hidden, Name = "Secret" }]));
        var access = new Mock<IWorkspaceAccessPolicy>(MockBehavior.Strict);
        access.Setup(a => a.MayView(It.Is<IWorkspaceImplementationConfiguration>(w => w.Id == OwnerId), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        access.Setup(a => a.MayView(It.Is<IWorkspaceImplementationConfiguration>(w => w.Id == hidden), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(WorkspacesResultCodes.ByName("WorkspaceNotVisible"), NullLogger.Instance));
        _names.Setup(n => n.Owners("Workspace", It.Is<IEnumerable<Guid>>(ids => ids.SequenceEqual(new[] { OwnerId })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, string>>.Success(new Dictionary<Guid, string> { [OwnerId] = "Scouting" }));
        var organizations = Mock.Of<IOrganizationConfigurationProvider>(MockBehavior.Strict);

        var (status, page) = await Call<ListOwners, ListDataOptionOwnersRequest, PaginatedResponse<DataOptionOwnerChoicePayload>>([_names.Object, organizations, workspaces.Object, access.Object], new ListDataOptionOwnersRequest { Kind = "Workspace" }, WithPaging);
        var (refused, _) = await Call<ListOwners, ListDataOptionOwnersRequest, PaginatedResponse<DataOptionOwnerChoicePayload>>([_names.Object, organizations, workspaces.Object, access.Object], new ListDataOptionOwnersRequest(), WithPaging);

        status.ShouldBe(200);
        page.ShouldNotBeNull().Items.ShouldHaveSingleItem().Name.ShouldBe("Scouting");
        refused.ShouldBe(400);
    }

    [Fact]
    public async Task ThePermissionsListIsTheCallersPermissionsInNameOrderAndARefusalForACallerWhoIsNotSignedIn()
    {
        var context = new Mock<IAuthenticationContext>(MockBehavior.Strict);
        context.Setup(c => c.IsAuthenticated).Returns(true);
        context.Setup(c => c.Permissions).Returns(["sports:read", "datastores:write"]);
        var signedIn = new Mock<IAuthenticationContextAccessor>(MockBehavior.Strict);
        signedIn.SetupGet(a => a.Current).Returns(context.Object);
        var anonymous = new Mock<IAuthenticationContextAccessor>(MockBehavior.Strict);
        anonymous.SetupGet(a => a.Current).Returns((IAuthenticationContext?)null);

        var (status, permissions) = await Call<ListPermissions, EmptyRequest, List<string>>([signedIn.Object], EmptyRequest.Instance);
        var (refused, _) = await Call<ListPermissions, EmptyRequest, List<string>>([anonymous.Object], EmptyRequest.Instance);

        status.ShouldBe(200);
        permissions.ShouldBe(["datastores:write", "sports:read"]);
        refused.ShouldBe(401);
    }

    [Fact]
    public async Task TheHistoryAnswersAPageOfChangesNamedThroughTheCatalogWithTheMemberAChangeTouched()
    {
        DataConfigurationRuleHistoryQuery? asked = null;
        _rules.Setup(r => r.History(It.IsAny<DataConfigurationRuleHistoryQuery>(), It.IsAny<CancellationToken>()))
            .Callback<DataConfigurationRuleHistoryQuery, CancellationToken>((query, _) => asked = query)
            .ReturnsAsync(GenericResult<DataConfigurationRuleHistoryPage>.Success(new DataConfigurationRuleHistoryPage
            {
                Items =
                [
                    Change("NameChanged", "East", "West"),
                    Change("RuleAdded", null, "East teams"),
                ],
                TotalCount = 12,
            }));
        var request = new ListDataOptionRuleHistoryRequest { Name = Store, PathName = StorePath, ContainerName = Container, Column = "Division", RuleId = RuleId, Page = 2, PageSize = 10 };

        var (status, page) = await Call<ListHistory, ListDataOptionRuleHistoryRequest, PaginatedResponse<DataOptionRuleChangePayload>>([_rules.Object], request, WithPaging);

        status.ShouldBe(200);
        var query = asked.ShouldNotBeNull();
        (query.Page, query.PageSize, query.Column, query.RuleId, query.Target.Container).ShouldBe((2, 10, "Division", RuleId, Container));
        page.ShouldNotBeNull().TotalCount.ShouldBe(12);
        (page.Page, page.PageSize).ShouldBe((2, 10));
        var named = page.Items[0];
        (named.Kind, named.KindDisplayName, named.ByName, named.RuleName).ShouldBe(("NameChanged", "Name changed", "Lee", "East teams"));
        var member = named.Changes.ShouldHaveSingleItem();
        (member.Member, member.MemberDisplayName, member.Old, member.New).ShouldBe(("name", "Name", "East", "West"));
        page.Items[1].Changes.ShouldBeEmpty();
    }

    [Fact]
    public async Task TheLimitsAnswerTheDeclaredLengthsByMemberNameAndEveryChangeableMemberOfTheCatalog()
    {
        _rules.Setup(r => r.Limits(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<DataConfigurationRuleLimits>.Success(new DataConfigurationRuleLimits
        {
            Rule = new Dictionary<string, int> { ["name"] = 200 },
            Value = new Dictionary<string, int> { ["value"] = 400 },
        }));
        var request = new DataOptionContainerRequest { Name = Store, PathName = StorePath, ContainerName = Container };

        var (status, limits) = await Call<GetLimits, DataOptionContainerRequest, DataOptionLimitsPayload>([_rules.Object], request);

        status.ShouldBe(200);
        limits.ShouldNotBeNull().Rule.ShouldBe(new Dictionary<string, int> { ["name"] = 200 });
        limits.Value.ShouldBe(new Dictionary<string, int> { ["value"] = 400 });
        limits.Members.Count.ShouldBe(DataConfigurationRuleCatalog.Members.Count);
        var description = limits.Members.Single(member => member.Of == "rule" && member.Name == "description");
        (description.DisplayName, description.Clearable).ShouldBe(("Description", true));
    }

    [Fact]
    public async Task AFailureOfTheLimitsIsAnsweredWithItsOwnStatusAndNoAnswer()
    {
        _rules.Setup(r => r.Limits(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataConfigurationRuleLimits>.Failure(DataOptionRulesLog.LimitNotAWholeNumber(NullLogger.Instance, "Name", Container)));

        var (status, limits) = await Call<GetLimits, DataOptionContainerRequest, DataOptionLimitsPayload>([_rules.Object], new DataOptionContainerRequest { Name = Store, PathName = StorePath, ContainerName = Container });

        status.ShouldBeInRange(400, 599);
        limits.ShouldBeNull();
    }

    [Fact]
    public async Task AHistoryWithAPageBelowOneIsRefusedBeforeTheServiceIsCalled()
    {
        var request = new ListDataOptionRuleHistoryRequest { Name = Store, PathName = StorePath, ContainerName = Container, Page = 0 };

        var (status, _) = await Call<ListHistory, ListDataOptionRuleHistoryRequest, PaginatedResponse<DataOptionRuleChangePayload>>([_rules.Object], request, WithPaging);

        status.ShouldBe(400);
        _rules.VerifyNoOtherCalls();
    }

    private static DataConfigurationRuleHistoryEntry Change(string kind, string? oldValue, string? newValue) => new()
    {
        At = new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero),
        ByUserId = Changer,
        ByName = "Lee",
        RuleId = RuleId,
        RuleName = "East teams",
        Kind = DataConfigurationRuleCatalog.ChangeKind(kind)!,
        OldValue = oldValue,
        NewValue = newValue,
    };

    private static DataOptionRuleKeyRequest Key() => new() { Name = Store, PathName = StorePath, ContainerName = Container, RuleId = RuleId };

    private static DataConfigurationRule Rule(bool changed = true) => new()
    {
        Id = RuleId,
        Name = "East teams",
        Abbreviation = "ET",
        Target = new DataConfigurationRuleContainer { DataStore = Store, DataPath = StorePath, Container = Container },
        Column = "Division",
        Path = ["DataConfigurationRules", "Comparison", "In"],
        Method = "To",
        Values = [new DataConfigurationRuleValue { Id = ValueId, Kind = "Literal", Value = "East", Ordinal = 1 }],
        OwnerKind = "Workspace",
        OwnerId = OwnerId,
        CustodianUserId = Custodian,
        CustodianState = "Holder",
        Active = true,
        Removed = false,
        CanChange = true,
        ChangedByUserId = changed ? Changer : null,
        ChangedAt = new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero),
    };

    // The directory names every user it is asked about and the one workspace owner.
    private void NamesAnswer()
    {
        _names.Setup(n => n.Users(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) => GenericResult<IReadOnlyDictionary<Guid, string>>.Success(ids.Distinct().ToDictionary(id => id, id => id == Custodian ? "Dana" : "Lee")));
        _names.Setup(n => n.Owners("Workspace", It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, IEnumerable<Guid> ids, CancellationToken _) => GenericResult<IReadOnlyDictionary<Guid, string>>.Success(ids.Distinct().ToDictionary(id => id, _ => "Scouting")));
    }

    private static void WithPaging(Microsoft.AspNetCore.Http.DefaultHttpContext context)
        => context.RequestServices = new ServiceCollection().AddLogging().AddSingleton<IEffectiveSettingsProvider>(new PagingSettings()).BuildServiceProvider();

    private static async Task<(int Status, TResponse? Body)> Call<TEndpoint, TRequest, TResponse>(
        object[] dependencies, TRequest request, Action<Microsoft.AspNetCore.Http.DefaultHttpContext>? context = null)
        where TEndpoint : DataOptionRuleServiceEndpointBase<TRequest, TResponse>
        where TRequest : notnull
        where TResponse : class
    {
        var endpoint = Factory.Create<TEndpoint>(context ?? (_ => { }), dependencies);
        await endpoint.HandleAsync(request, TestContext.Current.CancellationToken);
        var status = endpoint.HttpContext.Response.StatusCode;

        // Why only after a success: Endpoint.Response builds a default response when none was sent, and the payloads have required members, so reading it after a refusal throws.
        return (status, status is >= 200 and < 300 ? endpoint.Response : null);
    }

    private sealed class PagingSettings : IEffectiveSettingsProvider
    {
        public T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null)
            => TryGetEffectiveValue<T>(settingName, out var value, tenantId, roleName) ? value : throw new InvalidOperationException(settingName);

        public bool TryGetEffectiveValue<T>(string settingName, out T value, Guid? tenantId = null, string? roleName = null)
        {
            object? found = settingName switch
            {
                SettingDefinitions.MaxPaginationSize => 200,
                SettingDefinitions.DefaultPaginationSize => 50,
                _ => null,
            };
            if (found is T typed)
            {
                value = typed;
                return true;
            }

            value = default!;
            return false;
        }
    }

    private sealed class GetRule(IDataConfigurationRules rules, IDataConfigurationRuleNames names) : GetDataOptionRuleEndpointBase(NullLogger.Instance, rules, names);

    private sealed class AddRule(IDataConfigurationRules rules, IDataConfigurationRuleNames names) : AddDataOptionRuleEndpointBase(NullLogger.Instance, rules, names);

    private sealed class ChangeRule(IDataConfigurationRules rules, IDataConfigurationRuleNames names) : ChangeDataOptionRuleEndpointBase(NullLogger.Instance, rules, names);

    private sealed class SwitchRule(IDataConfigurationRules rules, IDataConfigurationRuleNames names) : SwitchDataOptionRuleEndpointBase(NullLogger.Instance, rules, names);

    private sealed class RemoveRule(IDataConfigurationRules rules, IDataConfigurationRuleNames names) : RemoveDataOptionRuleEndpointBase(NullLogger.Instance, rules, names);

    private sealed class RestoreRule(IDataConfigurationRules rules, IDataConfigurationRuleNames names) : RestoreDataOptionRuleEndpointBase(NullLogger.Instance, rules, names);

    private sealed class AddValues(IDataConfigurationRules rules, IDataConfigurationRuleNames names) : AddDataOptionValuesEndpointBase(NullLogger.Instance, rules, names);

    private sealed class ChangeValue(IDataConfigurationRules rules, IDataConfigurationRuleNames names) : ChangeDataOptionValueEndpointBase(NullLogger.Instance, rules, names);

    private sealed class RemoveValues(IDataConfigurationRules rules, IDataConfigurationRuleNames names) : RemoveDataOptionValuesEndpointBase(NullLogger.Instance, rules, names);

    private sealed class ListRules(IDataConfigurationRules rules, IDataConfigurationRuleNames names) : ListDataOptionRulesEndpointBase(NullLogger.Instance, rules, names);

    private sealed class GetLimits(IDataConfigurationRules rules) : GetDataOptionLimitsEndpointBase(NullLogger.Instance, rules);

    private sealed class ListHistory(IDataConfigurationRules rules) : ListDataOptionRuleHistoryEndpointBase(NullLogger.Instance, rules);

    private sealed class ListCustodians(IDataConfigurationRuleNames names) : ListDataOptionCustodiansEndpointBase(NullLogger.Instance, names);

    private sealed class ListOwners(IDataConfigurationRuleNames names, IOrganizationConfigurationProvider organizations, IWorkspaceConfigurationProvider workspaces, IWorkspaceAccessPolicy access)
        : ListDataOptionOwnersEndpointBase(NullLogger.Instance, names, organizations, workspaces, access);

    private sealed class ListPermissions(IAuthenticationContextAccessor auth) : ListDataOptionPermissionsEndpointBase(NullLogger.Instance, auth);
}
