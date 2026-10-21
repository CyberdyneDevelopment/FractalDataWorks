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

    private AdministrativeAuditService Audit(IAuthenticationContext? caller, string? store = "OpsDb")
    {
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(caller);
        return new AdministrativeAuditService(new StubGatewayProvider(_gateway.Object), NullLoggerFactory.Instance, accessor.Object, store);
    }

    private static IAuthenticationContext Signed(Guid? tenantId, bool global = false, string userId = "0c000000-0000-0000-0000-00000000000c", string name = "Jo")
    {
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.IsAuthenticated).Returns(true);
        caller.SetupGet(c => c.UserId).Returns(userId);
        caller.SetupGet(c => c.Username).Returns(name);
        caller.SetupGet(c => c.ActiveTenantId).Returns(tenantId);
        caller.SetupGet(c => c.IsSystemContext).Returns(false);
        caller.SetupGet(c => c.Claims).Returns(global
            ? new Dictionary<string, object>(StringComparer.Ordinal) { ["globalTenant"] = "true" }
            : new Dictionary<string, object>(StringComparer.Ordinal));
        return caller.Object;
    }

    private static AdministrativeChangeEntry Entry(string level = "Tenant", Guid? organizationId = null, string changeKind = "RoleAssigned")
        => new(changeKind, "UserRole", "jo/Admin", AdministrativeLevels.ByName(level), organizationId, null, "Admin", "new hire");

    private static AdministrativeChangeFilter Everything(Guid? tenantId = null)
        => new(1, 25, null, tenantId, null, null, null);

    private AdministrativeChange? Written()
        => _writes.OfType<InsertCommand<AdministrativeChange>>().Select(c => c.Data).SingleOrDefault();

    private static IEnumerable<FilterCondition> Conditions(IFilterNode? node) => node switch
    {
        FilterCondition c => [c],
        FilterGroup g => g.Nodes.SelectMany(Conditions),
        _ => []
    };

    private IReadOnlyList<FilterCondition> QueryConditions()
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
}
