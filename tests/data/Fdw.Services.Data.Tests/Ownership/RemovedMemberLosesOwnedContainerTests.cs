using Fdw.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests.Ownership;

/// <summary>
/// FDW-7093 with FDW-6968: a member of the workspace that owns a container writes it; once removed, the same write is refused at the owner's scope.
/// The workspace access policy here is the real one, reading the membership the removal retired.
/// </summary>
public sealed class RemovedMemberLosesOwnedContainerTests
{
    private static readonly Guid StoreId = Guid.Parse("0193d300-0000-7000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("0193d300-0000-7000-8000-0000000000aa");
    private static readonly Guid ContainerId = Guid.Parse("0193d300-0000-7000-8000-000000000020");
    private static readonly Guid CallerId = Guid.Parse("0193d300-0000-7000-8000-0000000000c1");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AMemberWritesTheOwnedContainerUntilRemovedAndIsRefusedAfter()
    {
        var member = new WorkspaceMemberConfiguration
        {
            Id = Guid.CreateVersion7(), Name = $"User:{CallerId}", SubjectType = "User", SubjectId = CallerId, MemberRole = "Contributor", State = "Active",
        };
        var workspace = new WorkspaceImplementationConfiguration
        {
            Id = Guid.CreateVersion7(), Name = "VendorTeam", OwnerUserId = Guid.CreateVersion7(), TenantId = TenantId,
            OrganizationId = Guid.CreateVersion7(), Status = "Active", Visibility = "Private", Members = [member],
        };
        var ownership = Ownership(workspace);
        var path = new DataPathConfiguration { Id = Guid.CreateVersion7(), Name = "vendor", Containers = [new DataContainerConfiguration { Id = ContainerId, Name = "AcmeFeed" }] };
        var item = new DataStoreItemAddress("SharedFeeds", "vendor", "AcmeFeed");
        var ct = TestContext.Current.CancellationToken;

        var owners = await ownership.Owners(StoreId, ct);
        var owner = owners.Value!.Of(path, path.Containers[0]);
        var before = await ownership.MayWrite(StoreId, item, owner, ct);
        var removed = await Retirement().RemoveMember(workspace, member, ct);
        var after = await ownership.MayWrite(StoreId, item, owner, ct);

        before.IsSuccess.ShouldBeTrue(before.CurrentMessage);
        removed.IsSuccess.ShouldBeTrue(removed.CurrentMessage);
        after.IsFailure.ShouldBeTrue();
    }

    private static WorkspaceChildRetirement Retirement()
    {
        var gateway = new Mock<IConfigurationGateway>(MockBehavior.Strict);
        gateway.Setup(g => g.Execute<ConfigurationWriteResult>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], null)));
        var gateways = new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict);
        gateways.Setup(p => p.Get("ConfigurationDb")).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        return new WorkspaceChildRetirement(gateways.Object, "ConfigurationDb", "workspace", NullLogger<WorkspaceChildRetirement>.Instance);
    }

    // The ownership decision over the real workspace access policy. The caller holds no platform permission and no store permission: whatever
    // they may write in the container comes from their membership of its owner.
    private static DataStoreItemOwnership Ownership(WorkspaceImplementationConfiguration workspace)
    {
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.UserId).Returns(CallerId.ToString());
        caller.SetupGet(c => c.IsAuthenticated).Returns(true);
        caller.SetupGet(c => c.Roles).Returns([]);
        caller.SetupGet(c => c.Permissions).Returns([]);
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupProperty(a => a.Current, caller.Object);

        var roles = new Mock<IRoleConfigurationProvider>(MockBehavior.Strict);
        roles.Setup(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IRoleImplementationConfiguration>>.Success([]));
        var policy = new WorkspaceAccessPolicy(accessor.Object, roles.Object, new Mock<IDataSetConfigurationProvider>(MockBehavior.Strict).Object);

        var stores = new Mock<IDataStoreConfigurationProvider>(MockBehavior.Strict);
        stores.Setup(s => s.TenantOf(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Success(TenantId));

        var workspaces = new Mock<IWorkspaceConfigurationProvider>(MockBehavior.Strict);
        workspaces.Setup(w => w.Get(workspace.Id, ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IWorkspaceImplementationConfiguration>.Success(workspace));

        var owners = new Mock<IWorkspaceResourceOwners>(MockBehavior.Strict);
        owners.Setup(o => o.Owned(DataPathResourceKind.KindName, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<WorkspaceResourceOwner>>.Success([]));
        owners.Setup(o => o.Owned(DataContainerResourceKind.KindName, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<WorkspaceResourceOwner>>.Success([new WorkspaceResourceOwner(Guid.CreateVersion7(), ContainerId, workspace.Id)]));

        return new DataStoreItemOwnership(
            stores.Object, workspaces.Object, policy, owners.Object,
            new Mock<IFrameworkAuthorizationService>(MockBehavior.Strict).Object, accessor.Object, NullLogger<DataStoreItemOwnership>.Instance);
    }
}
