using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-7024: detaching a resource retires its row by Id through <see cref="IWorkspaceChildRetirement.DetachResource"/>, as on the 1.5.14 patch. The
/// workspace's resource list is upsert-only, so the old detach (save the workspace without the row) kept the row current and answered 204 for nothing.
/// </summary>
public sealed class WorkspaceResourceDetachTests
{
    private static readonly Guid AttachmentId = Guid.Parse("0193d000-0000-7000-8000-000000000001");
    private static readonly Guid ContainerId = Guid.Parse("0193d000-0000-7000-8000-000000000002");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADetachRetiresTheOwnsRowByItsIdAndWritesNothingElse()
    {
        var gateway = new RecordingGateway(succeeds: true);
        var workspace = Workspace();
        var resource = workspace.Resources[0];

        var detached = await Retirement(gateway).DetachResource(workspace, resource, TestContext.Current.CancellationToken);

        detached.IsSuccess.ShouldBeTrue(detached.CurrentMessage);
        var (command, target) = gateway.Writes.ShouldHaveSingleItem();
        var retire = command.ShouldBeOfType<ConfigurationRetireCommand>();
        retire.KeyValues.ShouldHaveSingleItem().ShouldBe(AttachmentId);
        retire.MarkDeleted.ShouldBeTrue();
        target.Container.ShouldBe("WorkspaceResource");
        workspace.Resources.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ADetachWhoseRetireFailsLeavesTheResourceAndSaysSo()
    {
        var workspace = Workspace();

        var detached = await Retirement(new RecordingGateway(succeeds: false)).DetachResource(workspace, workspace.Resources[0], TestContext.Current.CancellationToken);

        detached.IsFailure.ShouldBeTrue();
        workspace.Resources.ShouldHaveSingleItem().Id.ShouldBe(AttachmentId);
    }

    private static WorkspaceChildRetirement Retirement(RecordingGateway gateway)
    {
        var gateways = new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict);
        gateways.Setup(p => p.Get("ConfigurationDb")).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Gateway));
        return new WorkspaceChildRetirement(gateways.Object, "ConfigurationDb", "workspace", NullLogger<WorkspaceChildRetirement>.Instance);
    }

    private static WorkspaceImplementationConfiguration Workspace() => new()
    {
        Id = Guid.CreateVersion7(),
        Name = "VendorTeam",
        Resources =
        [
            new WorkspaceResourceConfiguration { Id = AttachmentId, ResourceType = "DataContainer", ResourceId = ContainerId, Relationship = "Owns", Name = "AcmeFeed" },
        ],
    };

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
                        : GenericResult<ConfigurationWriteResult>.Failure(WorkspaceChildRetirementLog.PathNotDeclared(NullLogger.Instance, target.DataStore, target.Container));
                });
            Gateway = gateway.Object;
        }

        public IConfigurationGateway Gateway { get; }

        public List<(IDataCommand Command, DataStoreTarget Target)> Writes { get; } = [];
    }
}
