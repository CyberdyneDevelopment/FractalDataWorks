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
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-7024, FDW-7093: removing a member, detaching a relationship or detaching a resource retires that row by its Id. The lists are upsert-only, so the old route (save the
/// workspace without the row) kept a removed member current, with everything the membership gave them.
/// </summary>
public sealed class WorkspaceChildRetirementTests
{
    private static readonly Guid CallerId = Guid.Parse("0193d200-0000-7000-8000-0000000000c1");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task RemovingAMemberRetiresItsRowByIdOnTheMemberContainer()
    {
        var gateway = new RecordingGateway(succeeds: true);
        var member = Member();
        var workspace = Workspace(member);

        var removed = await Retirement(gateway).RemoveMember(workspace, member, TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue(removed.CurrentMessage);
        var (command, target) = gateway.Writes.ShouldHaveSingleItem();
        command.ShouldBeOfType<ConfigurationRetireCommand>().KeyValues.ShouldHaveSingleItem().ShouldBe(member.Id);
        target.Container.ShouldBe("WorkspaceMember");
        workspace.Members.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task DetachingARelationshipRetiresItsRowByIdOnTheRelationshipContainer()
    {
        var gateway = new RecordingGateway(succeeds: true);
        var relationship = new WorkspaceRelationshipConfiguration { Id = Guid.CreateVersion7(), Cardinality = "OneToMany" };
        var workspace = Workspace();
        workspace.Relationships.Add(relationship);

        var detached = await Retirement(gateway).DetachRelationship(workspace, relationship, TestContext.Current.CancellationToken);

        detached.IsSuccess.ShouldBeTrue(detached.CurrentMessage);
        var (command, target) = gateway.Writes.ShouldHaveSingleItem();
        command.ShouldBeOfType<ConfigurationRetireCommand>().KeyValues.ShouldHaveSingleItem().ShouldBe(relationship.Id);
        target.Container.ShouldBe("WorkspaceRelationship");
        workspace.Relationships.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task DetachingAResourceRetiresItsRowByIdOnTheResourceContainer()
    {
        var gateway = new RecordingGateway(succeeds: true);
        var resource = new WorkspaceResourceConfiguration { Id = Guid.CreateVersion7(), ResourceType = "DataSet", ResourceId = Guid.CreateVersion7(), Relationship = "Owns", Name = "Sales" };
        var workspace = Workspace();
        workspace.Resources.Add(resource);

        var detached = await Retirement(gateway).DetachResource(workspace, resource, TestContext.Current.CancellationToken);

        detached.IsSuccess.ShouldBeTrue(detached.CurrentMessage);
        var (command, target) = gateway.Writes.ShouldHaveSingleItem();
        command.ShouldBeOfType<ConfigurationRetireCommand>().KeyValues.ShouldHaveSingleItem().ShouldBe(resource.Id);
        target.Container.ShouldBe("WorkspaceResource");
        workspace.Resources.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARetireThatFailsLeavesTheMemberInPlaceAndSaysSo()
    {
        var member = Member();
        var workspace = Workspace(member);

        var removed = await Retirement(new RecordingGateway(succeeds: false)).RemoveMember(workspace, member, TestContext.Current.CancellationToken);

        removed.IsFailure.ShouldBeTrue();
        workspace.Members.ShouldHaveSingleItem().ShouldBe(member);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARemovedMemberMayNoLongerWriteTheWorkspace()
    {
        var member = Member();
        var workspace = Workspace(member);
        var policy = Policy();
        var ct = TestContext.Current.CancellationToken;

        var before = await policy.MayWrite(workspace, ct);
        var removed = await Retirement(new RecordingGateway(succeeds: true)).RemoveMember(workspace, member, ct);
        var after = await policy.MayWrite(workspace, ct);

        before.IsSuccess.ShouldBeTrue(before.CurrentMessage);
        removed.IsSuccess.ShouldBeTrue(removed.CurrentMessage);
        after.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARemovedMemberMayNoLongerReadTheWorkspacesDataSets()
    {
        var member = Member();
        var workspace = Workspace(member);
        var policy = Policy();
        var ct = TestContext.Current.CancellationToken;

        var before = await policy.MayRead(workspace, ct);
        var removed = await Retirement(new RecordingGateway(succeeds: true)).RemoveMember(workspace, member, ct);
        var after = await policy.MayRead(workspace, ct);

        before.IsSuccess.ShouldBeTrue(before.CurrentMessage);
        removed.IsSuccess.ShouldBeTrue(removed.CurrentMessage);
        after.IsFailure.ShouldBeTrue();
    }

    private static WorkspaceChildRetirement Retirement(RecordingGateway gateway)
    {
        var gateways = new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict);
        gateways.Setup(p => p.Get("ConfigurationDb")).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Gateway));
        return new WorkspaceChildRetirement(gateways.Object, "ConfigurationDb", "workspace", NullLogger<WorkspaceChildRetirement>.Instance);
    }

    private static WorkspaceMemberConfiguration Member() => new()
    {
        Id = Guid.CreateVersion7(),
        Name = $"User:{CallerId}",
        SubjectType = "User",
        SubjectId = CallerId,
        MemberRole = "Contributor",
        State = "Active",
    };

    private static WorkspaceImplementationConfiguration Workspace(params WorkspaceMemberConfiguration[] members) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = "VendorTeam",
        OwnerUserId = Guid.CreateVersion7(),
        Status = "Active",
        Visibility = "Private",
        Members = [.. members],
    };

    // The caller holds no platform permission: whatever they may do in the workspace comes from their membership alone. The caller's context is a
    // plain data stand-in; the role provider, a result-returning FDW contract, is strict and states its answer.
    private static WorkspaceAccessPolicy Policy()
    {
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.UserId).Returns(CallerId.ToString());
        caller.SetupGet(c => c.Roles).Returns([]);
        caller.SetupGet(c => c.Permissions).Returns([]);
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(caller.Object);

        var roles = new Mock<IRoleConfigurationProvider>(MockBehavior.Strict);
        roles.Setup(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IRoleImplementationConfiguration>>.Success([]));

        return new WorkspaceAccessPolicy(accessor.Object, roles.Object, new Mock<IDataSetConfigurationProvider>(MockBehavior.Strict).Object);
    }

    // A gateway that records each write and its target, and answers each with success or a coded failure.
    private sealed class RecordingGateway
    {
        public RecordingGateway(bool succeeds)
        {
            var gateway = new Mock<IConfigurationGateway>(MockBehavior.Strict);
            gateway
                .Setup(g => g.Execute<ConfigurationWriteResult>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IDataCommand command, DataStoreTarget target, CancellationToken _) =>
                {
                    Writes.Add((command, target));
                    return succeeds
                        ? GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], null))
                        : GenericResult<ConfigurationWriteResult>.Failure(
                            Fdw.Services.Workspaces.Logging.WorkspaceChildRetirementLog.PathNotDeclared(NullLogger.Instance, target.DataStore, target.Container));
                });
            Gateway = gateway.Object;
        }

        public IConfigurationGateway Gateway { get; }

        public List<(IDataCommand Command, DataStoreTarget Target)> Writes { get; } = [];
    }
}
