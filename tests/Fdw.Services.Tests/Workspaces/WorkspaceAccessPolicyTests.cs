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
using Fdw.Services.Workspaces.Results;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-876: the read side of the workspace access decision, and that sharing one decision with
/// the write side did not change what the write side answers.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class WorkspaceAccessPolicyTests
{
    private static readonly Guid CallerId = Guid.NewGuid();
    private static readonly Guid AnalystsRoleId = Guid.NewGuid();
    private static readonly Guid VisibleSetId = Guid.Parse("0197a3c4-5b6d-7e8f-9a0b-2d3e4f5a6b7c");
    private static readonly Guid HiddenSetId = Guid.Parse("0197a3c4-5b6d-7e8f-9a0b-3e4f5a6b7c8d");

    [Fact]
    public async Task MayReadAllowsTheOwner()
    {
        var policy = Policy(CallerId.ToString());

        (await policy.MayRead(Workspace(owner: CallerId), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ConsumerMayReadButNotWrite()
    {
        var policy = Policy(CallerId.ToString());
        var workspace = Workspace(Member("User", CallerId, "Consumer", "Active"));

        (await policy.MayRead(workspace, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await policy.MayWrite(workspace, TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task ContributorMayReadAndWrite()
    {
        var policy = Policy(CallerId.ToString());
        var workspace = Workspace(Member("User", CallerId, "Contributor", "Active"));

        (await policy.MayRead(workspace, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await policy.MayWrite(workspace, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Invited")]
    [InlineData("Suspended")]
    [InlineData("Left")]
    public async Task MayReadRefusesAMembershipNotInEffect(string state)
    {
        var policy = Policy(CallerId.ToString());

        (await policy.MayRead(Workspace(Member("User", CallerId, "Consumer", state)), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task MayReadRefusesAnotherUsersMembership()
    {
        var policy = Policy(CallerId.ToString());

        (await policy.MayRead(Workspace(Member("User", Guid.NewGuid(), "Consumer", "Active")), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task MayReadRefusesAnUnknownRoleName()
    {
        var policy = Policy(CallerId.ToString());

        (await policy.MayRead(Workspace(Member("User", CallerId, "Auditor", "Active")), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task MayReadRefusesADeletedMembership()
    {
        var policy = Policy(CallerId.ToString());
        var member = Member("User", CallerId, "Consumer", "Active");
        member.IsDeleted = true;

        (await policy.MayRead(Workspace(member), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task MayReadAllowsAMemberByRoleTheCallerHolds()
    {
        var policy = Policy(CallerId.ToString(), "Analysts");

        (await policy.MayRead(Workspace(Member("Role", AnalystsRoleId, "Consumer", "Active")), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task MayReadRefusesWithNoAuthenticationContext()
    {
        var policy = Policy(callerUserId: null);

        (await policy.MayRead(Workspace(owner: CallerId), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void MayReadThrowsForANullWorkspaceBeforeAnyAwait()
    {
        var policy = Policy(CallerId.ToString());

        Should.Throw<ArgumentNullException>(() => policy.MayRead(null!, TestContext.Current.CancellationToken));
    }

    // ── FDW-936: additive access, visibility, per-workspace permissions, 404 vs 403 ──────────

    [Theory]
    [InlineData("workspaces:read")]
    [InlineData("workspaces:*")]
    public async Task APlatformReadPermissionViewsAndSeesAWorkspaceTheCallerIsNotIn(string permission)
    {
        var policy = PolicyWith(permissions: [permission]);
        var workspace = Workspace(visibility: "Private");

        (await policy.MayView(workspace, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await policy.MaySee(workspace, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task APlatformQueryPermissionReadsRowsOfAWorkspaceTheCallerIsNotIn()
    {
        var policy = PolicyWith(permissions: ["workspaces:query"]);

        (await policy.MayRead(Workspace(visibility: "Private"), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task APlatformWritePermissionChangesAWorkspaceTheCallerIsNotIn()
    {
        var policy = PolicyWith(permissions: ["workspaces:write"]);

        (await policy.MayWrite(Workspace(visibility: "Private"), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ThePermissionForOneWorkspaceGrantsThatOneOnly()
    {
        var vendorShare = Workspace(visibility: "Private");
        var policy = PolicyWith(permissions: [WorkspacePermissionNames.For(vendorShare.Id, WorkspacePermissionNames.QueryAction)]);

        (await policy.MayRead(vendorShare, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await policy.MayView(vendorShare, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await policy.MayRead(Workspace(visibility: "Private", name: "Other"), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task APermissionNamingTheWorkspaceByNameGrantsNothing()
    {
        // FDW-937: per-workspace permissions are keyed by Id. A name-keyed one would collide across
        // tenants, which name workspaces independently.
        var policy = PolicyWith(permissions: ["workspaces:Vendor Share:query", "workspaces:Vendor Share:view"]);

        (await policy.MayView(Workspace(visibility: "Private"), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task TheAllResourcesWildcardGrantsLikeThePlatformGate()
    {
        var policy = PolicyWith(permissions: ["*:*"]);

        (await policy.MayRead(Workspace(visibility: "Private"), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task OpenGrantsViewButNeverRows()
    {
        var policy = PolicyWith(permissions: []);
        var workspace = Workspace(visibility: "Open");

        (await policy.MayView(workspace, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await policy.MayRead(workspace, TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        (await policy.MayWrite(workspace, TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task AnObserverViewsButNeverReadsRowsOrWrites()
    {
        var policy = Policy(CallerId.ToString());
        var workspace = Workspace(Member("User", CallerId, "Observer", "Active"));

        (await policy.MayView(workspace, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await policy.MayRead(workspace, TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        (await policy.MayWrite(workspace, TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task DiscoverableCanBeAskedToJoinButIsHiddenFromEveryoneWhoDoesNotHaveIt()
    {
        // FDW-937: listing is having it. Finding a Discoverable workspace to ask to join is the only
        // thing it grants; a caller who may not view it is refused with a 404 for everything else.
        var policy = PolicyWith(permissions: []);
        var workspace = Workspace(visibility: "Discoverable");

        (await policy.MaySee(workspace, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        var view = await policy.MayView(workspace, TestContext.Current.CancellationToken);
        var write = await policy.MayWrite(workspace, TestContext.Current.CancellationToken);
        view.IsFailure.ShouldBeTrue();
        WorkspaceAccessRefusals.Hides(view).ShouldBeTrue();
        WorkspaceAccessRefusals.Hides(write).ShouldBeTrue();
    }

    [Fact]
    public async Task ARefusedChangeToAWorkspaceTheCallerMayViewIsA403NotAHiddenAbsence()
    {
        var policy = PolicyWith(permissions: []);

        var write = await policy.MayWrite(Workspace(visibility: "Open"), TestContext.Current.CancellationToken);

        write.IsFailure.ShouldBeTrue();
        WorkspaceAccessRefusals.Hides(write).ShouldBeFalse();
    }

    [Fact]
    public async Task PrivateIsInvisibleToANonMemberSoEveryRefusalHidesIt()
    {
        var policy = PolicyWith(permissions: []);
        var workspace = Workspace(visibility: "Private");

        var see = await policy.MaySee(workspace, TestContext.Current.CancellationToken);
        var view = await policy.MayView(workspace, TestContext.Current.CancellationToken);
        var write = await policy.MayWrite(workspace, TestContext.Current.CancellationToken);

        see.IsFailure.ShouldBeTrue();
        WorkspaceAccessRefusals.Hides(see).ShouldBeTrue();
        WorkspaceAccessRefusals.Hides(view).ShouldBeTrue();
        WorkspaceAccessRefusals.Hides(write).ShouldBeTrue();
    }

    [Fact]
    public async Task AConsumerRefusedAWriteIsToldSoRatherThanHiddenFrom()
    {
        var policy = Policy(CallerId.ToString());
        var workspace = Workspace(Member("User", CallerId, "Consumer", "Active"));

        var write = await policy.MayWrite(workspace, TestContext.Current.CancellationToken);
        write.IsFailure.ShouldBeTrue();
        WorkspaceAccessRefusals.Hides(write).ShouldBeFalse();
    }

    [Fact]
    public async Task AnUnreadableRoleStoreIsAnUndeterminedDecisionNotARefusal()
    {
        var accessor = new Mock<IAuthenticationContextAccessor>();
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.UserId).Returns(CallerId.ToString());
        caller.SetupGet(c => c.Roles).Returns(Array.Empty<string>());
        caller.SetupGet(c => c.Permissions).Returns(Array.Empty<string>());
        accessor.SetupGet(a => a.Current).Returns(caller.Object);
        var roles = new Mock<IRoleConfigurationProvider>();
        roles.Setup(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IRoleImplementationConfiguration>>.Success(null!));
        var policy = new WorkspaceAccessPolicy(accessor.Object, roles.Object, new Mock<IDataSetConfigurationProvider>().Object);

        var view = await policy.MayView(Workspace(visibility: "Open"), TestContext.Current.CancellationToken);

        view.IsFailure.ShouldBeTrue();
        WorkspaceAccessRefusals.IsUndetermined(view).ShouldBeTrue();
        WorkspaceAccessRefusals.Hides(view).ShouldBeFalse();
    }

    [Fact]
    public async Task ThePlatformQueryPermissionViewsTooSoRowsNeverComeFromAWorkspaceTheCallerCannotList()
    {
        var policy = PolicyWith(permissions: ["workspaces:query"]);

        (await policy.MayView(Workspace(visibility: "Private"), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    // ── FDW-937: granting a data set through a workspace ─────────────────────────────────────

    [Theory]
    [InlineData("datasets:grant")]
    [InlineData("datasets/0197a3c4-5b6d-7e8f-9a0b-2d3e4f5a6b7c:grant")]
    [InlineData("datasets:*")]
    [InlineData("*:*")]
    public async Task AWriterHoldingAGrantForAVisibleDataSetMayAttachIt(string grant)
    {
        var policy = PolicyWith(permissions: ["workspaces:write", grant]);

        (await policy.MayAttachDataSet(Workspace(visibility: "Private"), VisibleSetId, TestContext.Current.CancellationToken))
            .IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task AWriterWithoutAGrantMayNotAttachADataSetTheyCanSee()
    {
        var policy = PolicyWith(permissions: ["workspaces:write", "datasets:read"]);

        var attach = await policy.MayAttachDataSet(Workspace(visibility: "Private"), VisibleSetId, TestContext.Current.CancellationToken);

        attach.IsFailure.ShouldBeTrue();
        attach.Code.ShouldBeOfType<DataSetGrantNotPermittedCode>();
    }

    [Fact]
    public async Task AGrantForAnotherDataSetGrantsNothing()
    {
        var policy = PolicyWith(permissions: ["workspaces:write", "datasets/0197a3c4-5b6d-7e8f-9a0b-3e4f5a6b7c8d:grant"]);

        (await policy.MayAttachDataSet(Workspace(visibility: "Private"), VisibleSetId, TestContext.Current.CancellationToken))
            .IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task ADataSetRowLevelSecurityHidesFromTheCallerCannotBeAttachedEvenWithAGrant()
    {
        var policy = PolicyWith(permissions: ["workspaces:write", "datasets:grant"]);

        var attach = await policy.MayAttachDataSet(Workspace(visibility: "Private"), HiddenSetId, TestContext.Current.CancellationToken);

        attach.IsFailure.ShouldBeTrue();
        attach.Code.ShouldBeOfType<DataSetNotVisibleCode>();
    }

    [Fact]
    public async Task AGrantDoesNotLetACallerAttachToAWorkspaceTheyMayNotChange()
    {
        var policy = PolicyWith(permissions: ["datasets:grant"]);

        (await policy.MayAttachDataSet(Workspace(visibility: "Open"), VisibleSetId, TestContext.Current.CancellationToken))
            .IsFailure.ShouldBeTrue();
    }

    private static WorkspaceAccessPolicy PolicyWith(string[] permissions)
        => Policy(CallerId.ToString(), permissions, []);

    private static IWorkspaceImplementationConfiguration Workspace(string visibility, string name = "Vendor Share")
    {
        var workspace = new Mock<IWorkspaceImplementationConfiguration>();
        workspace.SetupGet(d => d.Id).Returns(Guid.NewGuid());
        workspace.SetupGet(d => d.Name).Returns(name);
        workspace.SetupGet(d => d.OwnerUserId).Returns(Guid.NewGuid());
        workspace.SetupGet(d => d.Visibility).Returns(visibility);
        workspace.SetupGet(d => d.Members).Returns(new List<WorkspaceMemberConfiguration>());
        return workspace.Object;
    }

    private static WorkspaceAccessPolicy Policy(string? callerUserId, params string[] callerRoles)
        => Policy(callerUserId, [], callerRoles);

    private static WorkspaceAccessPolicy Policy(string? callerUserId, string[] permissions, string[] callerRoles)
    {
        var accessor = new Mock<IAuthenticationContextAccessor>();
        if (callerUserId is not null)
        {
            var caller = new Mock<IAuthenticationContext>();
            caller.SetupGet(c => c.UserId).Returns(callerUserId);
            caller.SetupGet(c => c.Roles).Returns(callerRoles);
            caller.SetupGet(c => c.Permissions).Returns(permissions);
            accessor.SetupGet(a => a.Current).Returns(caller.Object);
        }

        var analysts = new Mock<IRoleImplementationConfiguration>();
        analysts.SetupGet(r => r.Id).Returns(AnalystsRoleId);
        analysts.SetupGet(r => r.Name).Returns("Analysts");
        analysts.SetupGet(r => r.IsCurrent).Returns(true);
        analysts.SetupGet(r => r.IsDeleted).Returns(false);

        var roles = new Mock<IRoleConfigurationProvider>();
        roles.Setup(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IRoleImplementationConfiguration>>.Success([analysts.Object]));

        var sales = new Mock<IDataSetImplementationConfiguration>();
        sales.SetupGet(s => s.Id).Returns(VisibleSetId);
        sales.SetupGet(s => s.Name).Returns("Sales");

        // Row-level security as the connection applies it: a set the caller cannot see comes back as
        // no value, exactly like one that does not exist.
        var dataSets = new Mock<IDataSetConfigurationProvider>();
        dataSets.Setup(d => d.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(null!));
        dataSets.Setup(d => d.Get(VisibleSetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(sales.Object));

        return new WorkspaceAccessPolicy(accessor.Object, roles.Object, dataSets.Object);
    }

    private static IWorkspaceImplementationConfiguration Workspace(params WorkspaceMemberConfiguration[] members)
        => Workspace(Guid.NewGuid(), members);

    private static IWorkspaceImplementationConfiguration Workspace(Guid owner, params WorkspaceMemberConfiguration[] members)
    {
        var workspace = new Mock<IWorkspaceImplementationConfiguration>();
        workspace.SetupGet(d => d.Name).Returns("Vendor Share");
        workspace.SetupGet(d => d.OwnerUserId).Returns(owner);
        workspace.SetupGet(d => d.Members).Returns(new List<WorkspaceMemberConfiguration>(members));
        return workspace.Object;
    }

    private static WorkspaceMemberConfiguration Member(string subjectType, Guid subjectId, string role, string state)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = $"{subjectType}:{subjectId}",
            SubjectType = subjectType,
            SubjectId = subjectId,
            MemberRole = role,
            State = state,
        };
}
