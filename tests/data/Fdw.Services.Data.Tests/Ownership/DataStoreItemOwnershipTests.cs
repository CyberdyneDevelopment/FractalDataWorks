using Fdw.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections;
using Fdw.Services.Data.Logging;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests.Ownership;

/// <summary>
/// FDW-6967: a workspace owns one path or one container of a shared data store. The store stays shared; an owned item is decided at its owner's
/// scope alone, an unowned one by the store's rule.
/// </summary>
public sealed class DataStoreItemOwnershipTests
{
    private const string StoreName = "SharedFeeds";
    private const string PathName = "vendor";
    private const string FeedName = "AcmeFeed";
    private const string OtherName = "Internal";
    private static readonly Guid StoreId = Guid.Parse("0193b000-0000-7000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("0193b000-0000-7000-8000-0000000000aa");
    private static readonly Guid PathId = Guid.Parse("0193b000-0000-7000-8000-000000000010");
    private static readonly Guid FeedId = Guid.Parse("0193b000-0000-7000-8000-000000000020");
    private static readonly Guid OtherId = Guid.Parse("0193b000-0000-7000-8000-000000000021");
    private static readonly Guid VendorTeamId = Guid.Parse("0193b000-0000-7000-8000-0000000000f1");
    private static readonly Guid AnalystsId = Guid.Parse("0193b000-0000-7000-8000-0000000000f2");
    private static readonly Guid OrganizationId = Guid.Parse("0193b000-0000-7000-8000-0000000000c1");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AContainersOwnOwnerWinsOverItsPathsOwner()
    {
        var path = Path();
        var owners = new DataStoreItemOwners(
            TenantId,
            new Dictionary<Guid, DataStoreItemOwner> { [PathId] = Owner(VendorTeamId, "VendorTeam") },
            new Dictionary<Guid, DataStoreItemOwner> { [FeedId] = Owner(AnalystsId, "Analysts") });

        owners.Of(path, path.Containers[0])!.WorkspaceId.ShouldBe(AnalystsId);
        owners.Of(path, path.Containers[0])!.ThroughPath.ShouldBeFalse();
        owners.Of(path, path.Containers[1])!.WorkspaceId.ShouldBe(VendorTeamId);
        owners.Of(path, path.Containers[1])!.ThroughPath.ShouldBeTrue();
        owners.OwnOf(path, path.Containers[1]).ShouldBeNull();
        owners.Of(path, null)!.WorkspaceId.ShouldBe(VendorTeamId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void AnItemNobodyOwnsIsTheStores()
    {
        var path = Path();
        var owners = new DataStoreItemOwners(TenantId, new Dictionary<Guid, DataStoreItemOwner>(), new Dictionary<Guid, DataStoreItemOwner>());

        owners.Of(path, path.Containers[0]).ShouldBeNull();
        owners.Of(path, null).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnUnownedContainerIsAssignedWhenTheStoreRuleAndTheWorkspaceBothAllowIt()
    {
        var fixture = new Fixture();

        var assigned = await fixture.Service.Assign(Container(FeedName), "VendorTeam", TestContext.Current.CancellationToken);

        assigned.IsSuccess.ShouldBeTrue(assigned.CurrentMessage);
        assigned.Value!.WorkspaceId.ShouldBe(VendorTeamId);
        assigned.Value.OrganizationId.ShouldBe(OrganizationId);
        fixture.Owned.ShouldHaveSingleItem().ShouldBe((VendorTeamId, DataContainerResourceKind.KindName, FeedId));
        fixture.StoreWriteChecks.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AContainerAnotherWorkspaceOwnsIsRefusedNamingTheOwner()
    {
        var fixture = new Fixture(containerOwners: [(FeedId, AnalystsId)]);

        var assigned = await fixture.Service.Assign(Container(FeedName), "VendorTeam", TestContext.Current.CancellationToken);

        assigned.IsFailure.ShouldBeTrue();
        Codes(assigned).ShouldContain(code => code!.EndsWith("41760", StringComparison.Ordinal));
        assigned.CurrentMessage!.ShouldContain("Analysts");
        fixture.Owned.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AssigningTheOwnerItAlreadyHasWritesNothing()
    {
        var fixture = new Fixture(containerOwners: [(FeedId, VendorTeamId)]);

        var assigned = await fixture.Service.Assign(Container(FeedName), "VendorTeam", TestContext.Current.CancellationToken);

        assigned.IsSuccess.ShouldBeTrue(assigned.CurrentMessage);
        fixture.Owned.ShouldBeEmpty();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [InlineData("NoSuchPath", null, "31761")]
    [InlineData(PathName, "NoSuchContainer", "31762")]
    public async Task AnItemTheStoreDoesNotHaveIsRefused(string pathName, string? containerName, string code)
    {
        var fixture = new Fixture();

        var assigned = await fixture.Service.Assign(new DataStoreItemAddress(StoreName, pathName, containerName), "VendorTeam", TestContext.Current.CancellationToken);

        Codes(assigned).ShouldContain(c => c!.EndsWith(code, StringComparison.Ordinal));
        fixture.Owned.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerWhoMayNotWriteTheWorkspaceIsRefused()
    {
        var fixture = new Fixture(writable: []);

        var assigned = await fixture.Service.Assign(Container(FeedName), "VendorTeam", TestContext.Current.CancellationToken);

        assigned.IsFailure.ShouldBeTrue();
        fixture.Owned.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerWithoutTheStoresWritePermissionCannotLendAnUnownedItem()
    {
        var fixture = new Fixture(holdsStoreWrite: false);

        var assigned = await fixture.Service.Assign(Container(FeedName), "VendorTeam", TestContext.Current.CancellationToken);

        Codes(assigned).ShouldContain(c => c!.EndsWith("51760", StringComparison.Ordinal));
        fixture.Owned.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AContainerUnderAnOwnedPathIsLentAtThePathOwnersScopeNotTheStores()
    {
        // The caller holds no store permission but may write both workspaces: the path's owner decides, so the store is never asked.
        var fixture = new Fixture(pathOwners: [(PathId, VendorTeamId)], holdsStoreWrite: false);

        var assigned = await fixture.Service.Assign(Container(FeedName), "Analysts", TestContext.Current.CancellationToken);

        assigned.IsSuccess.ShouldBeTrue(assigned.CurrentMessage);
        fixture.StoreWriteChecks.ShouldBe(0);
        fixture.Owned.ShouldHaveSingleItem().ShouldBe((AnalystsId, DataContainerResourceKind.KindName, FeedId));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AReleaseIsDecidedAtTheOwnersScopeAloneSoAStoreAdministratorWhoIsNotAMemberIsRefused()
    {
        // The caller holds the store's write permission and may write Analysts, but not the owner.
        var fixture = new Fixture(containerOwners: [(FeedId, VendorTeamId)], writable: [AnalystsId]);

        var released = await fixture.Service.Release(Container(FeedName), TestContext.Current.CancellationToken);

        released.IsFailure.ShouldBeTrue();
        fixture.Released.ShouldBeEmpty();
        fixture.StoreWriteChecks.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AMemberOfTheOwnerReleasesItsContainer()
    {
        var fixture = new Fixture(containerOwners: [(FeedId, VendorTeamId)]);

        var released = await fixture.Service.Release(Container(FeedName), TestContext.Current.CancellationToken);

        released.IsSuccess.ShouldBeTrue(released.CurrentMessage);
        fixture.Released.ShouldHaveSingleItem().WorkspaceId.ShouldBe(VendorTeamId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AContainerOwnedOnlyThroughItsPathHasNoOwnerOfItsOwnToRelease()
    {
        var fixture = new Fixture(pathOwners: [(PathId, VendorTeamId)]);

        var released = await fixture.Service.Release(Container(FeedName), TestContext.Current.CancellationToken);

        Codes(released).ShouldContain(c => c!.EndsWith("41762", StringComparison.Ordinal));
        fixture.Released.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TwoOwnerRowsForOneItemFailLoudAndNoneIsPicked()
    {
        var fixture = new Fixture(containerOwners: [(FeedId, VendorTeamId), (FeedId, AnalystsId)]);

        var owners = await fixture.Service.Owners(StoreId, TestContext.Current.CancellationToken);

        Codes(owners).ShouldContain(c => c!.EndsWith("91760", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnUnownedItemIsDecidedByTheStoresRule()
    {
        var fixture = new Fixture(holdsStoreWrite: false);

        var refused = await fixture.Service.MayWrite(StoreId, Container(FeedName), null, TestContext.Current.CancellationToken);
        var permitted = await new Fixture().Service.MayWrite(StoreId, Container(FeedName), null, TestContext.Current.CancellationToken);

        Codes(refused).ShouldContain(c => c!.EndsWith("51760", StringComparison.Ordinal));
        permitted.IsSuccess.ShouldBeTrue(permitted.CurrentMessage);
    }

    private static DataStoreItemAddress Container(string name) => new(StoreName, PathName, name);

    private static DataStoreItemOwner Owner(Guid workspaceId, string name) => new(workspaceId, name, OrganizationId, Guid.CreateVersion7(), ThroughPath: false);

    private static DataPathConfiguration Path() => new()
    {
        Id = PathId,
        Name = PathName,
        Containers =
        [
            new DataContainerConfiguration { Id = FeedId, Name = FeedName },
            new DataContainerConfiguration { Id = OtherId, Name = OtherName },
        ],
    };

    private static List<string?> Codes(IGenericResult result) => [.. result.Messages.Select(message => message.Code)];

    // Every FDW contract the service calls is a strict stand-in that states its answers.
    private sealed class Fixture
    {
        public Fixture(
            IReadOnlyList<(Guid Item, Guid Workspace)>? pathOwners = null,
            IReadOnlyList<(Guid Item, Guid Workspace)>? containerOwners = null,
            IReadOnlyCollection<Guid>? writable = null,
            bool holdsStoreWrite = true)
        {
            IReadOnlyCollection<Guid> allowed = writable is null ? [VendorTeamId, AnalystsId] : writable;
            var workspaces = new Dictionary<Guid, WorkspaceImplementationConfiguration>
            {
                [VendorTeamId] = new() { Id = VendorTeamId, Name = "VendorTeam", OrganizationId = OrganizationId, TenantId = TenantId },
                [AnalystsId] = new() { Id = AnalystsId, Name = "Analysts", OrganizationId = OrganizationId, TenantId = TenantId },
            };

            var store = new Mock<IDataStoreImplementationConfiguration>(MockBehavior.Strict);
            store.SetupGet(s => s.Id).Returns(StoreId);
            store.SetupGet(s => s.Paths).Returns([Path()]);

            var stores = new Mock<IDataStoreConfigurationProvider>(MockBehavior.Strict);
            stores.Setup(s => s.Get(StoreName, ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Success(store.Object));
            stores.Setup(s => s.TenantOf(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Success(TenantId));
            stores.Setup(s => s.PermitWrite(StoreId, It.IsAny<CancellationToken>()))
                .Callback(() => StoreWriteChecks++)
                .ReturnsAsync(GenericResult.Success());

            var provider = new Mock<IWorkspaceConfigurationProvider>(MockBehavior.Strict);
            provider.Setup(p => p.Get(It.IsAny<Guid>(), ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, int _, ConfigurationReadOptions _, CancellationToken _) => GenericResult<IWorkspaceImplementationConfiguration>.Success(workspaces[id]));
            provider.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string name, CancellationToken _) => GenericResult<IWorkspaceImplementationConfiguration>.Success(
                    workspaces.Values.Single(w => string.Equals(w.Name, name, StringComparison.Ordinal))));

            var access = new Mock<IWorkspaceAccessPolicy>(MockBehavior.Strict);
            access.Setup(a => a.MayWrite(It.IsAny<IWorkspaceImplementationConfiguration>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IWorkspaceImplementationConfiguration w, CancellationToken _) => allowed.Contains(w.Id)
                    ? GenericResult.Success()
                    : GenericResult.Failure(DataStoreItemOwnershipLog.WorkspaceNotFound(NullLogger.Instance, w.Name)));

            var owners = new Mock<IWorkspaceResourceOwners>(MockBehavior.Strict);
            owners.Setup(o => o.Owned(DataPathResourceKind.KindName, TenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IReadOnlyList<WorkspaceResourceOwner>>.Success(Rows(pathOwners)));
            owners.Setup(o => o.Owned(DataContainerResourceKind.KindName, TenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IReadOnlyList<WorkspaceResourceOwner>>.Success(Rows(containerOwners)));
            owners.Setup(o => o.Own(It.IsAny<IWorkspaceImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IWorkspaceImplementationConfiguration w, string kind, Guid id, string label, CancellationToken _) =>
                {
                    Owned.Add((w.Id, kind, id));
                    return GenericResult<WorkspaceResourceConfiguration>.Success(new WorkspaceResourceConfiguration { Id = Guid.CreateVersion7(), ResourceId = id, ResourceType = kind, Name = label });
                });
            owners.Setup(o => o.Release(It.IsAny<WorkspaceResourceOwner>(), It.IsAny<CancellationToken>()))
                .Callback((WorkspaceResourceOwner owner, CancellationToken _) => Released.Add(owner))
                .ReturnsAsync(GenericResult.Success());

            var caller = new Mock<IAuthenticationContext>(MockBehavior.Strict);
            caller.SetupGet(c => c.IsAuthenticated).Returns(true);
            var accessor = new Mock<IAuthenticationContextAccessor>(MockBehavior.Strict);
            accessor.SetupProperty(a => a.Current, caller.Object);

            var authorization = new Mock<IFrameworkAuthorizationService>(MockBehavior.Strict);
            authorization.Setup(a => a.Authorize(It.IsAny<IAuthenticationContext>(), "datastores", "write", It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<bool>.Success(holdsStoreWrite));

            Service = new DataStoreItemOwnership(
                stores.Object, provider.Object, access.Object, owners.Object, authorization.Object, accessor.Object, NullLogger<DataStoreItemOwnership>.Instance);
        }

        public DataStoreItemOwnership Service { get; }

        public List<(Guid Workspace, string Kind, Guid Item)> Owned { get; } = [];

        public List<WorkspaceResourceOwner> Released { get; } = [];

        public int StoreWriteChecks { get; private set; }

        private static List<WorkspaceResourceOwner> Rows(IReadOnlyList<(Guid Item, Guid Workspace)>? rows) =>
            [.. (rows is null ? [] : rows).Select(row => new WorkspaceResourceOwner(Guid.CreateVersion7(), row.Item, row.Workspace))];
    }
}
