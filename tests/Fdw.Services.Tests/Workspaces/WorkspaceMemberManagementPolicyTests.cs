using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Services.Workspaces.Results;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-1071: deciding who is in a workspace is its own question, answered by the owner, a member whose
/// role manages members, or workspaces:members -- never by workspaces:write -- and a member grants and
/// changes only roles at or below their own rank.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class WorkspaceMemberManagementPolicyTests
{
    private static readonly Guid CallerId = Guid.NewGuid();

    [Fact]
    public async Task ThePlatformWritePermissionDoesNotDecideWhoIsInAWorkspace()
    {
        var policy = Policy(["workspaces:write", "workspaces:read"]);

        var manages = await policy.MayManageMembers(Workspace(), TestContext.Current.CancellationToken);

        manages.IsFailure.ShouldBeTrue();
        manages.Code.ShouldBeOfType<WorkspaceMembersNotManageableCode>();
    }

    [Fact]
    public async Task AWriterWhoCannotViewTheWorkspaceIsAnsweredAsIfItDidNotExist()
    {
        var policy = Policy(["workspaces:write"]);

        var manages = await policy.MayManageMembers(Workspace(), TestContext.Current.CancellationToken);

        manages.Code.ShouldBeOfType<WorkspaceNotVisibleCode>();
    }

    [Fact]
    public async Task AContributorChangesTheWorkspaceButDoesNotManageItsMembers()
    {
        var policy = Policy([]);
        var workspace = Workspace(Member(CallerId, "Contributor"));

        (await policy.MayWrite(workspace, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await policy.MayManageMembers(workspace, TestContext.Current.CancellationToken)).Code
            .ShouldBeOfType<WorkspaceMembersNotManageableCode>();
    }

    [Theory]
    [InlineData("Steward", true)]
    [InlineData("Contributor", true)]
    [InlineData("Consumer", true)]
    [InlineData("Observer", true)]
    [InlineData("Owner", false)]
    public async Task AStewardHandlesOnlyRolesAtOrBelowSteward(string role, bool allowed)
    {
        var policy = Policy([]);
        var workspace = Workspace(Member(CallerId, "Steward"));

        var result = await policy.MayManageRole(workspace, WorkspaceMemberRoles.ByName(role), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBe(allowed);
        if (!allowed) result.Code.ShouldBeOfType<WorkspaceMemberRankExceededCode>();
    }

    [Fact]
    public async Task TheOwnerHandlesEveryRole()
    {
        var policy = Policy([]);

        (await policy.MayManageRole(Workspace(owner: CallerId), WorkspaceMemberRoles.Owner, TestContext.Current.CancellationToken))
            .IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("workspaces:members")]
    [InlineData("workspaces:*")]
    public async Task ThePlatformMembersPermissionHandlesEveryRole(string permission)
    {
        var policy = Policy([permission, "workspaces:read"]);

        (await policy.MayManageRole(Workspace(), WorkspaceMemberRoles.Owner, TestContext.Current.CancellationToken))
            .IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void OnlyOwnerAndStewardManageMembersAndTheyRankAboveTheRest()
    {
        WorkspaceMemberRoles.Owner.MayManageMembers.ShouldBeTrue();
        WorkspaceMemberRoles.Steward.MayManageMembers.ShouldBeTrue();
        WorkspaceMemberRoles.Contributor.MayManageMembers.ShouldBeFalse();
        WorkspaceMemberRoles.Owner.Rank.ShouldBeGreaterThan(WorkspaceMemberRoles.Steward.Rank);
        WorkspaceMemberRoles.Steward.Rank.ShouldBeGreaterThan(WorkspaceMemberRoles.Contributor.Rank);
        WorkspaceMemberRoles.Contributor.Rank.ShouldBeGreaterThan(WorkspaceMemberRoles.Consumer.Rank);
        WorkspaceMemberRoles.Consumer.Rank.ShouldBeGreaterThan(WorkspaceMemberRoles.Observer.Rank);
    }

    private static WorkspaceAccessPolicy Policy(string[] permissions)
    {
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.UserId).Returns(CallerId.ToString());
        caller.SetupGet(c => c.Roles).Returns(Array.Empty<string>());
        caller.SetupGet(c => c.Permissions).Returns(permissions);
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(caller.Object);

        var roles = new Mock<IRoleConfigurationProvider>();
        roles.Setup(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IRoleImplementationConfiguration>>.Success([]));

        return new WorkspaceAccessPolicy(accessor.Object, roles.Object, new Mock<IDataSetConfigurationProvider>().Object);
    }

    private static IWorkspaceImplementationConfiguration Workspace(params WorkspaceMemberConfiguration[] members)
        => Workspace(Guid.NewGuid(), members);

    private static IWorkspaceImplementationConfiguration Workspace(Guid owner, params WorkspaceMemberConfiguration[] members)
    {
        var workspace = new Mock<IWorkspaceImplementationConfiguration>();
        workspace.SetupGet(d => d.Id).Returns(Guid.NewGuid());
        workspace.SetupGet(d => d.Name).Returns("Vendor Share");
        workspace.SetupGet(d => d.OwnerUserId).Returns(owner);
        workspace.SetupGet(d => d.Visibility).Returns("Private");
        workspace.SetupGet(d => d.Members).Returns(new List<WorkspaceMemberConfiguration>(members));
        return workspace.Object;
    }

    private static WorkspaceMemberConfiguration Member(Guid userId, string role)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = $"User:{userId}",
            SubjectType = "User",
            SubjectId = userId,
            MemberRole = role,
            State = "Active",
            IsCurrent = true,
        };
}
