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
/// FDW-7024: while a container's <c>Owns</c> row is current, a store administrator who is not a member of the owner is refused at the owner's scope;
/// once the row is detached, the same write is decided by the store's rule.
/// </summary>
public sealed class DetachReleasesOwnershipTests
{
    private static readonly Guid StoreId = Guid.Parse("0193d100-0000-7000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("0193d100-0000-7000-8000-0000000000aa");
    private static readonly Guid ContainerId = Guid.Parse("0193d100-0000-7000-8000-000000000020");
    private static readonly Guid PathId = Guid.Parse("0193d100-0000-7000-8000-000000000010");
    private static readonly Guid VendorTeamId = Guid.Parse("0193d100-0000-7000-8000-0000000000f1");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AStoreAdministratorIsRefusedAtTheOwnersScopeOnlyWhileTheOwnsRowIsCurrent()
    {
        var rows = new InMemoryOwnsRows([new WorkspaceResourceOwner(Guid.CreateVersion7(), ContainerId, VendorTeamId)]);
        var ownership = Ownership(rows);
        var item = new DataStoreItemAddress("SharedFeeds", "vendor", "AcmeFeed");
        var ct = TestContext.Current.CancellationToken;

        var owned = await ownership.Owners(StoreId, ct);
        var whileOwned = await ownership.MayWrite(StoreId, item, owned.Value!.Of(Path(), Path().Containers[0]), ct);

        await rows.Release(rows.Rows.Single(), ct);
        var released = await ownership.Owners(StoreId, ct);
        var afterDetach = await ownership.MayWrite(StoreId, item, released.Value!.Of(Path(), Path().Containers[0]), ct);

        whileOwned.IsFailure.ShouldBeTrue();
        whileOwned.Messages.Select(m => m.Code).ShouldContain(code => code!.EndsWith("31763", StringComparison.Ordinal));
        afterDetach.IsSuccess.ShouldBeTrue(afterDetach.CurrentMessage);
    }

    private static DataPathConfiguration Path() => new()
    {
        Id = PathId,
        Name = "vendor",
        Containers = [new DataContainerConfiguration { Id = ContainerId, Name = "AcmeFeed" }],
    };

    // The store administrator: holds datastores:write and passes the cross-tenant check, but may not write VendorTeam.
    private static DataStoreItemOwnership Ownership(InMemoryOwnsRows rows)
    {
        var stores = new Mock<IDataStoreConfigurationProvider>(MockBehavior.Strict);
        stores.Setup(s => s.TenantOf(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Success(TenantId));
        stores.Setup(s => s.PermitWrite(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        var vendorTeam = new WorkspaceImplementationConfiguration { Id = VendorTeamId, Name = "VendorTeam", OrganizationId = Guid.CreateVersion7(), TenantId = TenantId };
        var workspaces = new Mock<IWorkspaceConfigurationProvider>(MockBehavior.Strict);
        workspaces.Setup(w => w.Get(VendorTeamId, ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IWorkspaceImplementationConfiguration>.Success(vendorTeam));

        var access = new Mock<IWorkspaceAccessPolicy>(MockBehavior.Strict);
        access.Setup(a => a.MayWrite(It.IsAny<IWorkspaceImplementationConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWorkspaceImplementationConfiguration w, CancellationToken _) =>
                GenericResult.Failure(DataStoreItemOwnershipLog.WorkspaceNotFound(NullLogger.Instance, w.Name)));

        var caller = new Mock<IAuthenticationContext>(MockBehavior.Strict);
        caller.SetupGet(c => c.IsAuthenticated).Returns(true);
        var accessor = new Mock<IAuthenticationContextAccessor>(MockBehavior.Strict);
        accessor.SetupProperty(a => a.Current, caller.Object);

        var authorization = new Mock<IFrameworkAuthorizationService>(MockBehavior.Strict);
        authorization.Setup(a => a.Authorize(It.IsAny<IAuthenticationContext>(), "datastores", "write", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(true));

        return new DataStoreItemOwnership(
            stores.Object, workspaces.Object, access.Object, rows, authorization.Object, accessor.Object, NullLogger<DataStoreItemOwnership>.Instance);
    }

    // The Owns rows, held in memory: a release retires the row, so the next read no longer sees it, as the retire by Id does on the table.
    private sealed class InMemoryOwnsRows(List<WorkspaceResourceOwner> rows) : IWorkspaceResourceOwners
    {
        public List<WorkspaceResourceOwner> Rows { get; } = rows;

        public Task<IGenericResult<IReadOnlyList<WorkspaceResourceOwner>>> Owners(string kind, Guid resourceId, Guid tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IGenericResult<IReadOnlyList<WorkspaceResourceOwner>>>(
                GenericResult<IReadOnlyList<WorkspaceResourceOwner>>.Success([.. Rows.Where(row => row.ResourceId == resourceId)]));

        public Task<IGenericResult<IReadOnlyList<WorkspaceResourceOwner>>> Owned(string kind, Guid tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IGenericResult<IReadOnlyList<WorkspaceResourceOwner>>>(
                GenericResult<IReadOnlyList<WorkspaceResourceOwner>>.Success(
                    string.Equals(kind, DataContainerResourceKind.KindName, StringComparison.Ordinal) ? [.. Rows] : []));

        public Task<IGenericResult<WorkspaceResourceConfiguration>> Own(
            IWorkspaceImplementationConfiguration workspace, string kind, Guid resourceId, string label, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("This test records no new owner.");

        public Task<IGenericResult> Release(WorkspaceResourceOwner owner, CancellationToken cancellationToken = default)
        {
            Rows.Remove(owner);
            return Task.FromResult(GenericResult.Success());
        }

        public Task<IGenericResult<IReadOnlyList<WorkspaceResourceConfiguration>>> ReleaseOnArchive(
            IWorkspaceImplementationConfiguration workspace, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("This test archives nothing.");
    }
}
