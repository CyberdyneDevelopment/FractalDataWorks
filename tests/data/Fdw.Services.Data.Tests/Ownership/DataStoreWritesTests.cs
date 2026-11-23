using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests.Ownership;

/// <summary>
/// FDW-6968: every write of a data store is decided path by path and container by container, an owned one at its owner's scope alone and the rest by
/// the store's rule, and nothing is written until every scope it touches has said yes.
/// </summary>
public sealed class DataStoreWritesTests
{
    private const string StoreName = "SharedFeeds";
    private static readonly Guid StoreId = Guid.Parse("0193c000-0000-7000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("0193c000-0000-7000-8000-0000000000aa");
    private static readonly Guid VendorPathId = Guid.Parse("0193c000-0000-7000-8000-000000000010");
    private static readonly Guid InternalPathId = Guid.Parse("0193c000-0000-7000-8000-000000000011");
    private static readonly Guid FeedId = Guid.Parse("0193c000-0000-7000-8000-000000000020");
    private static readonly Guid LedgerId = Guid.Parse("0193c000-0000-7000-8000-000000000021");
    private static readonly Guid VendorTeamId = Guid.Parse("0193c000-0000-7000-8000-0000000000f1");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AMemberOfTheOwnerChangesTheOwnedContainerWithoutTheStoresRule()
    {
        var fixture = new Fixture(owners: Owned(FeedId), mayWriteOwner: true, storeRule: false);
        var written = Store();
        written.Paths[0].Containers[0].Format = "Json";

        var result = await fixture.Writes.UpdateStore(written, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        fixture.Scopes.ShouldBe(["VendorTeam"]);
        fixture.Updated.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AStoreAdministratorWhoIsNotAMemberOfTheOwnerIsRefusedAndNothingIsWritten()
    {
        var fixture = new Fixture(owners: Owned(FeedId), mayWriteOwner: false, storeRule: true);
        var written = Store();
        written.Paths[0].Containers[0].Format = "Json";

        var result = await fixture.Writes.UpdateStore(written, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        fixture.Scopes.ShouldBe(["VendorTeam"]);
        fixture.Updated.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnUnownedContainerIsStillDecidedByTheStore()
    {
        var fixture = new Fixture(owners: Owned(FeedId), mayWriteOwner: true, storeRule: false);
        var written = Store();
        written.Paths[1].Containers[0].Format = "Json";

        var result = await fixture.Writes.UpdateStore(written, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        fixture.Scopes.ShouldBe(["store"]);
        fixture.Updated.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AContainerOwnedThroughItsPathIsDecidedAtThePathOwnersScope()
    {
        var fixture = new Fixture(owners: OwnedPath(VendorPathId), mayWriteOwner: true, storeRule: false);
        var written = Store();
        written.Paths[0].Containers[0].Fields.Add(new DataContainerFieldConfiguration { Id = Guid.CreateVersion7(), Name = "Price" });

        var result = await fixture.Writes.UpdateStore(written, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        fixture.Scopes.ShouldBe(["VendorTeam"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AContainerAddedUnderAnOwnedPathIsTheOwnersToAdd()
    {
        var fixture = new Fixture(owners: OwnedPath(VendorPathId), mayWriteOwner: true, storeRule: false);
        var written = Store();
        written.Paths[0].Containers.Add(new DataContainerConfiguration { Id = Guid.CreateVersion7(), Name = "SecondFeed" });

        var result = await fixture.Writes.UpdateStore(written, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        fixture.Scopes.ShouldBe(["VendorTeam"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AWriteTouchingAnOwnedAndAnUnownedContainerNeedsBothScopes()
    {
        var fixture = new Fixture(owners: Owned(FeedId), mayWriteOwner: true, storeRule: false);
        var written = Store();
        written.Paths[0].Containers[0].Format = "Json";
        written.Paths[1].Containers[0].Format = "Json";

        var result = await fixture.Writes.UpdateStore(written, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        fixture.Scopes.ShouldBe(["VendorTeam", "store"]);
        fixture.Updated.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AChangeToTheStoresOwnFieldsIsDecidedByTheStore()
    {
        var fixture = new Fixture(owners: Owned(FeedId), mayWriteOwner: true, storeRule: true);
        var written = Store();
        written.WriteMode = "Append";

        var result = await fixture.Writes.UpdateStore(written, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        fixture.Scopes.ShouldBe(["store"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task DeletingAStoreWithAnOwnedContainerNeedsItsOwnerAsWellAsTheStore()
    {
        var refused = new Fixture(owners: Owned(FeedId), mayWriteOwner: false, storeRule: true);
        var allowed = new Fixture(owners: Owned(FeedId), mayWriteOwner: true, storeRule: true);

        var refusal = await refused.Writes.DeleteStore(StoreId, TestContext.Current.CancellationToken);
        var deleted = await allowed.Writes.DeleteStore(StoreId, TestContext.Current.CancellationToken);

        refusal.IsFailure.ShouldBeTrue();
        refused.Deleted.ShouldBe(0);
        deleted.IsSuccess.ShouldBeTrue(deleted.CurrentMessage);
        allowed.Scopes.ShouldBe(["VendorTeam", "store"]);
        allowed.Deleted.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ASaveThatChangesNothingTouchesNoItem()
    {
        var changes = DataStoreItemChanges.Between(Store(), Store(), NullLogger.Instance);

        changes.IsSuccess.ShouldBeTrue(changes.CurrentMessage);
        changes.Value!.Items.ShouldBeEmpty();
        changes.Value.StoreChanged.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ARemovedContainerIsTouched()
    {
        var written = Store();
        written.Paths[1].Containers.Clear();

        var changes = DataStoreItemChanges.Between(Store(), written, NullLogger.Instance);

        changes.Value!.Items.ShouldHaveSingleItem().Container!.Id.ShouldBe(LedgerId);
    }

    private static Dictionary<Guid, DataStoreItemOwner> Owned(Guid containerId) => new() { [containerId] = VendorTeam() };

    private static Dictionary<Guid, DataStoreItemOwner> OwnedPath(Guid pathId) => new() { [pathId] = VendorTeam() };

    private static DataStoreItemOwner VendorTeam() => new(VendorTeamId, "VendorTeam", Guid.CreateVersion7(), Guid.CreateVersion7(), ThroughPath: false);

    // A fresh copy each call, as a read of the store gives: the stored and the written store never share an object.
    private static DataStoreImplementationConfiguration Store() => new()
    {
        Id = StoreId,
        Name = StoreName,
        Domain = "DataStore",
        Implementation = "MsSql",
        WriteMode = "Replace",
        Paths =
        [
            new DataPathConfiguration { Id = VendorPathId, Name = "vendor", Containers = [new DataContainerConfiguration { Id = FeedId, Name = "AcmeFeed" }] },
            new DataPathConfiguration { Id = InternalPathId, Name = "internal", Containers = [new DataContainerConfiguration { Id = LedgerId, Name = "Ledger" }] },
        ],
    };

    // The ownership decision is a strict stand-in that states its answers; the provider's writes are the fake below.
    private sealed class Fixture
    {
        public Fixture(Dictionary<Guid, DataStoreItemOwner> owners, bool mayWriteOwner, bool storeRule)
        {
            var paths = owners.Where(o => o.Key == VendorPathId || o.Key == InternalPathId).ToDictionary(o => o.Key, o => o.Value);
            var containers = owners.Where(o => o.Key != VendorPathId && o.Key != InternalPathId).ToDictionary(o => o.Key, o => o.Value);

            var ownership = new Mock<IDataStoreItemOwnership>(MockBehavior.Strict);
            ownership.Setup(o => o.Owners(StoreId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<DataStoreItemOwners>.Success(new DataStoreItemOwners(TenantId, paths, containers)));
            ownership.Setup(o => o.MayWrite(StoreId, It.IsAny<DataStoreItemAddress>(), It.IsAny<DataStoreItemOwner?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid _, DataStoreItemAddress item, DataStoreItemOwner? owner, CancellationToken _) =>
                {
                    Scopes.Add(owner is null ? "store" : owner.WorkspaceName);
                    var allowed = owner is null ? storeRule : mayWriteOwner;
                    return allowed
                        ? GenericResult.Success()
                        : GenericResult.Failure(DataStoreItemOwnershipLog.StorePermissionMissing(NullLogger.Instance, item.ToString(), "datastores:write"));
                });

            Writes = new DataStoreWrites(new StoredStore(this), ownership.Object, NullLogger<DataStoreWrites>.Instance);
        }

        public DataStoreWrites Writes { get; }

        public List<string> Scopes { get; } = [];

        public int Updated { get; set; }

        public int Deleted { get; set; }
    }

    // The provider's unchecked writes, stating their answers: the stored store is read fresh each time, and each write is counted and succeeds.
    private sealed class StoredStore(Fixture fixture) : IDataStoreUncheckedWrites
    {
        public Task<IGenericResult<IDataStoreImplementationConfiguration>> Stored(Guid storeId, CancellationToken cancellationToken) =>
            Task.FromResult(storeId == StoreId
                ? GenericResult<IDataStoreImplementationConfiguration>.Success(Store())
                : GenericResult<IDataStoreImplementationConfiguration>.Failure(DataStoreItemOwnershipLog.StoreNotFoundForWrite(NullLogger.Instance, storeId.ToString("D"))));

        public Task<IGenericResult> Update(IDataStoreImplementationConfiguration store, CancellationToken cancellationToken)
        {
            fixture.Updated++;
            return Task.FromResult(GenericResult.Success());
        }

        public Task<IGenericResult> Delete(Guid storeId, CancellationToken cancellationToken)
        {
            fixture.Deleted++;
            return Task.FromResult(GenericResult.Success());
        }
    }
}
