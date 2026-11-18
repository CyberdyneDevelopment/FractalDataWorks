using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Operations.Audit;
using Fdw.Operations.Data;
using Fdw.Operations.Logging;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Tests.Audit;

/// <summary>
/// An administrative change is recorded under the caller's own identity and tenant, is refused when it is incomplete or the
/// caller is not a signed-in person, and is read back only within the tenant scope the caller reaches (FDW-1576).
/// </summary>
[Trait("Priority", "P1")]
public sealed class AdministrativeAuditServiceTests
{
    private static readonly Guid TenantA = Guid.Parse("0a000000-0000-0000-0000-00000000000a");
    private static readonly Guid TenantB = Guid.Parse("0b000000-0000-0000-0000-00000000000b");
    private static readonly Guid Person = Guid.Parse("0c000000-0000-0000-0000-00000000000c");
    private static readonly Guid Finance = Guid.Parse("0d000000-0000-0000-0000-00000000000d");

    private sealed class StubGatewayProvider(IDataGateway gateway) : IDataGatewayProvider
    {
        public IDataGateway ByName(string name) => gateway;
    }

    private readonly Mock<IDataGateway> _gateway = new();
    private readonly List<IDataCommand> _writes = [];
    private readonly List<IDataCommand> _queries = [];

    private void Stores(params AdministrativeChange[] rows)
    {
        _gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((c, _, _) => _writes.Add(c))
            .ReturnsAsync(GenericResult<int>.Success(1));
        _gateway
            .Setup(g => g.Execute<IEnumerable<AdministrativeChange>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((c, _, _) => _queries.Add(c))
            .ReturnsAsync(GenericResult<IEnumerable<AdministrativeChange>>.Success(rows));
    }

    private void StoreFails()
    {
        _gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Failure(OperationsLog.OperationalConnectionNotSet(NullLogger.Instance)));
    }

    // The actor's name is read from the person's account, never from the context (FDW-2199): the account here answers accountName, or fails when it is null.
    private AdministrativeAuditService Audit(IAuthenticationContext? caller, string? store = "OpsDb", string? accountName = "Jo")
    {
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(caller);
        var names = new Mock<IUserNames>();
        names.Setup(n => n.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(
            accountName is null
                ? GenericResult<string>.Failure(new Fdw.Messages.GenericMessage("the signed-in person has no account"))
                : GenericResult<string>.Success(accountName));
        return new AdministrativeAuditService(new StubGatewayProvider(_gateway.Object), NullLoggerFactory.Instance, accessor.Object, names.Object, store);
    }

    private static IAuthenticationContext Signed(Guid? tenantId, bool global = false, string userId = "0c000000-0000-0000-0000-00000000000c", string name = "Jo", string? actingRole = null)
    {
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.IsAuthenticated).Returns(true);
        caller.SetupGet(c => c.UserId).Returns(userId);
        caller.SetupGet(c => c.Username).Returns(name);
        caller.SetupGet(c => c.ActiveTenantId).Returns(tenantId);
        caller.SetupGet(c => c.IsSystemContext).Returns(false);
        var claims = global
            ? new Dictionary<string, object>(StringComparer.Ordinal) { ["globalTenant"] = "true" }
            : new Dictionary<string, object>(StringComparer.Ordinal);
        if (actingRole is not null)
            claims["actingRole"] = actingRole;
        caller.SetupGet(c => c.Claims).Returns(claims);
        return caller.Object;
    }

    private static AdministrativeChangeEntry Entry(string level = "Tenant", Guid? organizationId = null, string changeKind = "RoleAssigned")
        => new(changeKind, "UserRole", "jo/Admin", AdministrativeLevels.ByName(level), organizationId, null, "Admin", "new hire");

    private static AdministrativeChangeFilter Everything(Guid? tenantId = null)
        => new(1, 25, null, tenantId, null, null, null);

    private AdministrativeChange? Written()
        => _writes.OfType<InsertCommand<AdministrativeChange>>().SelectMany(c => c.Data).SingleOrDefault();

    private static IEnumerable<FilterCondition> Conditions(IFilterNode? node) => node switch
    {
        FilterCondition c => [c],
        FilterGroup g => g.Nodes.SelectMany(Conditions),
        _ => []
    };

    private List<FilterCondition> QueryConditions()
        => Conditions(_queries.OfType<QueryCommand<AdministrativeChange>>().Single().Filter?.Root).ToList();

    [Fact]
    public async Task RecordWritesTheChangeUnderTheCallersOwnIdentityAndTenant()
    {
        Stores();

        var result = await Audit(Signed(TenantA)).Record(Entry(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var row = Written().ShouldNotBeNull();
        row.ActorUserId.ShouldBe(Person);
        row.ActorName.ShouldBe("Jo");
        row.TenantId.ShouldBe(TenantA);
        row.Level.ShouldBe("Tenant");
        row.ChangeKind.ShouldBe("RoleAssigned");
        row.NewValue.ShouldBe("Admin");
        row.Reason.ShouldBe("new hire");
    }

    [Fact]
    public async Task RecordTakesTheActorsNameFromTheirAccountAndNotFromTheContext()
    {
        Stores();

        // The context's Username is the user id, which is what it answers when the token carries no name claim.
        var result = await Audit(Signed(TenantA, name: "0c000000-0000-0000-0000-00000000000c"), accountName: "Jo").Record(Entry(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        Written().ShouldNotBeNull().ActorName.ShouldBe("Jo");
    }

    [Fact]
    public async Task RecordRefusesWhenTheActorHasNoAccountToTakeANameFrom()
    {
        Stores();

        var result = await Audit(Signed(TenantA), accountName: null).Record(Entry(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        Written().ShouldBeNull();
    }

    [Fact]
    public async Task RecordNamesTheRoleActedWithAndStillTheRealPerson()
    {
        Stores();

        var result = await Audit(Signed(TenantA, actingRole: "Viewer")).Record(Entry(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var row = Written().ShouldNotBeNull();
        row.ActingRole.ShouldBe("Viewer");
        row.ActorUserId.ShouldBe(Person);
        row.ActorName.ShouldBe("Jo");
    }

    [Fact]
    public async Task RecordLeavesTheActingRoleEmptyForAChangeMadeInThePersonsOwnSession()
    {
        Stores();

        var result = await Audit(Signed(TenantA)).Record(Entry(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        Written().ShouldNotBeNull().ActingRole.ShouldBeNull();
    }

    [Fact]
    public async Task RecordAtTheOrganizationLevelKeepsTheOrganization()
    {
        Stores();

        var result = await Audit(Signed(TenantA)).Record(Entry("Organization", Finance), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        Written().ShouldNotBeNull().OrganizationId.ShouldBe(Finance);
    }

    [Fact]
    public async Task RecordRefusesAnEntryWithoutAChangeKind()
    {
        Stores();

        var result = await Audit(Signed(TenantA)).Record(Entry(changeKind: " "), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        Written().ShouldBeNull();
    }

    [Fact]
    public async Task RecordRefusesWhenNobodyIsSignedIn()
    {
        Stores();

        var result = await Audit(null).Record(Entry(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        Written().ShouldBeNull();
    }

    [Fact]
    public async Task RecordRefusesACallerWhoseUserIdIsNotAPerson()
    {
        Stores();

        var result = await Audit(Signed(TenantA, userId: "service-account")).Record(Entry(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        Written().ShouldBeNull();
    }

    [Fact]
    public async Task RecordRefusesACallerWhoActsInNoTenant()
    {
        Stores();

        var result = await Audit(Signed(null)).Record(Entry(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        Written().ShouldBeNull();
    }

    [Fact]
    public async Task RecordAtTheOrganizationLevelWithoutAnOrganizationIsRefused()
    {
        Stores();

        var result = await Audit(Signed(TenantA)).Record(Entry("Organization"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        Written().ShouldBeNull();
    }

    [Fact]
    public async Task RecordAboveTheOrganizationLevelNamingAnOrganizationIsRefused()
    {
        Stores();

        var result = await Audit(Signed(TenantA)).Record(Entry("Tenant", Finance), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        Written().ShouldBeNull();
    }

    [Fact]
    public async Task RecordFailsWhenTheStoreRefusesTheRow()
    {
        StoreFails();

        var result = await Audit(Signed(TenantA)).Record(Entry(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task RecordFailsWhenTheHostNamesNoOperationalStore()
    {
        Stores();

        var result = await Audit(Signed(TenantA), store: null).Record(Entry(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        Written().ShouldBeNull();
    }

    [Fact]
    public async Task ReadByATenantCallerIsLimitedToThatTenant()
    {
        Stores();

        var result = await Audit(Signed(TenantA)).Read(Everything(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var tenant = QueryConditions().Single(c => c.PropertyName == "TenantId");
        tenant.Value.ShouldBe(TenantA);
    }

    [Fact]
    public async Task ReadAskingForAnotherTenantAnswersNothingAndDoesNotQuery()
    {
        Stores();

        var result = await Audit(Signed(TenantA)).Read(Everything(TenantB), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().Items.ShouldBeEmpty();
        _queries.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReadByThePlatformReachesEveryTenantUnlessItNarrows()
    {
        Stores();

        await Audit(Signed(TenantA, global: true)).Read(Everything(), TestContext.Current.CancellationToken);
        QueryConditions().Any(c => c.PropertyName == "TenantId").ShouldBeFalse();

        _queries.Clear();
        await Audit(Signed(TenantA, global: true)).Read(Everything(TenantB), TestContext.Current.CancellationToken);
        QueryConditions().Single(c => c.PropertyName == "TenantId").Value.ShouldBe(TenantB);
    }

    [Fact]
    public async Task ReadAnswersRecordsNewestFirstWithTheirFields()
    {
        Stores(new AdministrativeChange
        {
            ActorUserId = Person, ActorName = "Jo", ChangeKind = "RoleAssigned", ItemKind = "UserRole", ItemId = "jo/Admin",
            Level = "Tenant", TenantId = TenantA, NewValue = "Admin"
        });

        var result = await Audit(Signed(TenantA)).Read(Everything(), TestContext.Current.CancellationToken);

        var record = result.Value.ShouldNotBeNull().Items.Single();
        record.ActorName.ShouldBe("Jo");
        record.TenantId.ShouldBe(TenantA);
        record.NewValue.ShouldBe("Admin");
    }

    [Fact]
    public async Task ReadRefusesPageZero()
    {
        Stores();

        var result = await Audit(Signed(TenantA)).Read(new AdministrativeChangeFilter(0, 25, null, null, null, null, null), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _queries.ShouldBeEmpty();
    }

    // FDW-6372: a change of several members is recorded whole or not at all, in one write.
    [Fact]
    public async Task RecordAllWritesEveryEntryInOneInsertUnderTheCallersIdentityAndTenant()
    {
        Stores();

        var result = await Audit(Signed(TenantA)).RecordAll([Entry(changeKind: "NameChanged"), Entry(changeKind: "PathChanged")], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var insert = _writes.OfType<InsertCommand<AdministrativeChange>>().ShouldHaveSingleItem();
        var rows = insert.Data.ToList();
        rows.Select(r => r.ChangeKind).ShouldBe(["NameChanged", "PathChanged"]);
        rows.ShouldAllBe(r => r.ActorUserId == Person && r.TenantId == TenantA && r.ActorName == "Jo");
    }

    [Fact]
    public async Task RecordAllWritesNothingWhenOneEntryIsIncomplete()
    {
        Stores();

        var result = await Audit(Signed(TenantA)).RecordAll([Entry(), Entry(changeKind: " ")], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _writes.ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordAllFailsWholeWhenTheStoreRefusesTheOneInsert()
    {
        _gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((c, _, _) => _writes.Add(c))
            .ReturnsAsync(GenericResult<int>.Failure(OperationsLog.OperationalConnectionNotSet(NullLogger.Instance)));

        var result = await Audit(Signed(TenantA)).RecordAll([Entry(), Entry(changeKind: "PathChanged"), Entry(changeKind: "GroupChanged")], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _writes.OfType<InsertCommand<AdministrativeChange>>().ShouldHaveSingleItem().Data.Count().ShouldBe(3);
    }

    [Fact]
    public async Task RecordAllRefusesAnEmptyChange()
    {
        Stores();

        var result = await Audit(Signed(TenantA)).RecordAll([], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _writes.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReadByItemNarrowsToThatItemWithinTheCallersTenant()
    {
        Stores();

        var result = await Audit(Signed(TenantA)).Read(
            new AdministrativeChangeFilter(1, 25, null, null, null, null, null) { ItemKind = "DataOptionRule", ItemId = "0193a4f1-7c2e-7b10-9a33-5f0e2b9c4d18" },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var conditions = QueryConditions();
        conditions.Single(c => c.PropertyName == "ItemKind").Value.ShouldBe("DataOptionRule");
        conditions.Single(c => c.PropertyName == "ItemId").Value.ShouldBe("0193a4f1-7c2e-7b10-9a33-5f0e2b9c4d18");
        conditions.Single(c => c.PropertyName == "TenantId").Value.ShouldBe(TenantA);
    }

    private void NewestTimes(params Dictionary<string, object?>[] rows)
    {
        _gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((c, _, _) => _queries.Add(c))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows));
    }

    private static AdministrativeChangeFilter NewestOf(params string[] items)
        => new(1, 25, null, null, null, null, null) { ItemKind = "DataConfigurationRule", ItemIds = items, NewestOfEachItem = true };

    // FDW-6346: a page of items is read in one query, each item once.
    [Fact]
    public async Task ReadByItemIdsNarrowsToThoseItemsInOneQuery()
    {
        Stores();

        var result = await Audit(Signed(TenantA)).Read(
            new AdministrativeChangeFilter(1, 25, null, null, null, null, null) { ItemKind = "DataConfigurationRule", ItemIds = ["a", "b"] },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var inItems = QueryConditions().Single(c => c.PropertyName == "ItemId");
        inItems.Operator.Name.ShouldBe("In");
        ((IEnumerable<string>)inItems.Value!).ShouldBe(["a", "b"]);
    }

    [Fact]
    public async Task TheNewestChangeOfEachItemIsReadWithTwoQueriesNeverOneForEachItem()
    {
        var first = new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);
        var second = first.AddMinutes(5);
        NewestTimes(
            new Dictionary<string, object?> { ["ItemId"] = "a", ["NewestAt"] = first },
            new Dictionary<string, object?> { ["ItemId"] = "b", ["NewestAt"] = second });
        Stores(
            new AdministrativeChange { ActorUserId = Person, ActorName = "Jo", ChangeKind = "NameChanged", ItemKind = "DataConfigurationRule", ItemId = "a", Level = "Tenant", TenantId = TenantA },
            new AdministrativeChange { ActorUserId = Person, ActorName = "Jo", ChangeKind = "NameChanged", ItemKind = "DataConfigurationRule", ItemId = "b", Level = "Tenant", TenantId = TenantA });

        var result = await Audit(Signed(TenantA)).Read(NewestOf("a", "b"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Items.Select(r => r.ItemId).ShouldBe(["a", "b"]);
        _queries.Count.ShouldBe(2);
        var grouped = _queries.OfType<QueryCommand<Dictionary<string, object?>>>().ShouldHaveSingleItem();
        var aggregation = grouped.Aggregation.ShouldBeOfType<AggregationExpression>();
        aggregation.GroupByFields.ShouldBe(["ItemId"]);
        aggregation.Aggregations["NewestAt"].ShouldBe("MAX(OccurredAt)");
        var either = ((FilterGroup)_queries.OfType<QueryCommand<AdministrativeChange>>().Single().Filter!.Root!).Nodes.OfType<FilterGroup>().Single(group => group.Operator == LogicalOperator.Or);
        either.Operator.ShouldBe(LogicalOperator.Or);
        either.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    public async Task TheNewestChangesNeedTheKindOfItem()
    {
        Stores();

        var result = await Audit(Signed(TenantA)).Read(
            new AdministrativeChangeFilter(1, 25, null, null, null, null, null) { ItemIds = ["a"], NewestOfEachItem = true },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _queries.ShouldBeEmpty();
    }

    [Fact]
    public async Task TheNewestChangesOfNoItemsAreNothingAndNoQueryIsMade()
    {
        Stores();

        var result = await Audit(Signed(TenantA)).Read(NewestOf(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Items.ShouldBeEmpty();
        _queries.ShouldBeEmpty();
    }

    [Fact]
    public async Task ARowOfTheGroupByThatDoesNotReadAsAnItemAndATimeFailsTheReadAndIsNeverAnswerAsNothing()
    {
        NewestTimes(new Dictionary<string, object?> { ["ItemId"] = "a", ["NewestAt"] = "not a time" });
        Stores();

        var result = await Audit(Signed(TenantA)).Read(NewestOf("a"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _queries.OfType<QueryCommand<AdministrativeChange>>().ShouldBeEmpty();
    }
}
