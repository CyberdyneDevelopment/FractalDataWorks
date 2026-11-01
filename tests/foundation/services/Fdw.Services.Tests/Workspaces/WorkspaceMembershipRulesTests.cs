using System;
using System.Collections.Generic;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-1070: an AutoApprove workspace states the role it grants, with no default and never a role that
/// manages members; FDW-1071: the owner's own membership is never removed or demoted.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class WorkspaceMembershipRulesTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    [Fact]
    public void AnAutoApproveWorkspaceMustStateTheRoleItGrants()
        => WorkspaceLifecycleValidator.ValidateAutoApproveRole("Quakes", "AutoApprove", null, NullLogger.Instance)
            .Code.ShouldBeOfType<WorkspaceAutoApproveRoleInvalidCode>();

    [Theory]
    [InlineData("Owner")]
    [InlineData("Steward")]
    public void AnAutoApproveWorkspaceNeverHandsOutARoleThatManagesMembers(string role)
        => WorkspaceLifecycleValidator.ValidateAutoApproveRole("Quakes", "AutoApprove", role, NullLogger.Instance)
            .Code.ShouldBeOfType<WorkspaceAutoApproveRoleInvalidCode>();

    [Theory]
    [InlineData("Consumer")]
    [InlineData("Observer")]
    [InlineData("Contributor")]
    public void AnAutoApproveWorkspaceMayGrantARoleThatDoesNotManageMembers(string role)
        => WorkspaceLifecycleValidator.ValidateAutoApproveRole("Quakes", "AutoApprove", role, NullLogger.Instance)
            .IsSuccess.ShouldBeTrue();

    [Fact]
    public void AnUnknownRoleIsRefused()
        => WorkspaceLifecycleValidator.ValidateAutoApproveRole("Quakes", "AutoApprove", "Admin", NullLogger.Instance)
            .Code.ShouldBeOfType<WorkspaceAutoApproveRoleInvalidCode>();

    [Theory]
    [InlineData("Closed")]
    [InlineData("RequestToJoin")]
    public void ARoleIsRefusedWhenThePolicyDoesNotAutoApprove(string policy)
        => WorkspaceLifecycleValidator.ValidateAutoApproveRole("Quakes", policy, "Consumer", NullLogger.Instance)
            .Code.ShouldBeOfType<WorkspaceAutoApproveRoleInvalidCode>();

    [Fact]
    public void NoRoleIsRightWhenThePolicyDoesNotAutoApprove()
        => WorkspaceLifecycleValidator.ValidateAutoApproveRole("Quakes", "RequestToJoin", null, NullLogger.Instance)
            .IsSuccess.ShouldBeTrue();

    [Fact]
    public void TheOwnersOwnMembershipCannotBeRemoved()
        => WorkspaceOwnerMembership.MayRemove(Workspace(), Member(OwnerId, "Owner"), NullLogger.Instance)
            .Code.ShouldBeOfType<WorkspaceOwnerMembershipProtectedCode>();

    [Fact]
    public void TheOwnersOwnMembershipCannotBeDemoted()
        => WorkspaceOwnerMembership.MayChange(Workspace(), Member(OwnerId, "Owner"), WorkspaceMemberRoles.Steward, NullLogger.Instance)
            .Code.ShouldBeOfType<WorkspaceOwnerMembershipProtectedCode>();

    [Fact]
    public void AnotherOwnerMembershipCanBeRemoved()
        => WorkspaceOwnerMembership.MayRemove(Workspace(), Member(Guid.NewGuid(), "Owner"), NullLogger.Instance)
            .IsSuccess.ShouldBeTrue();

    private static IWorkspaceImplementationConfiguration Workspace()
    {
        var workspace = new Mock<IWorkspaceImplementationConfiguration>();
        workspace.SetupGet(d => d.Name).Returns("Quakes");
        workspace.SetupGet(d => d.OwnerUserId).Returns(OwnerId);
        workspace.SetupGet(d => d.Members).Returns(new List<WorkspaceMemberConfiguration>());
        return workspace.Object;
    }

    private static WorkspaceMemberConfiguration Member(Guid userId, string role)
        => new() { Id = Guid.NewGuid(), Name = $"User:{userId}", SubjectType = "User", SubjectId = userId, MemberRole = role, State = "Active" };
}
