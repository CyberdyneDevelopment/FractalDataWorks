using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Connections.RoslynWorkspace;
using Fdw.Services.Connections.RoslynWorkspace.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Workspace.Roslyn;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.RoslynWorkspace.Tests;

/// <summary>
/// Tests for <see cref="RoslynWorkspaceConnectionFactory"/> covering validation
/// and mode-specific connection creation via a mock IRoslynWorkspaceFactory.
/// </summary>
public class RoslynWorkspaceConnectionFactoryTests
{
    private static readonly string SyntheticSlnPath =
        Path.Combine(
            AppContext.BaseDirectory,
            "tests", "_Fixtures", "SyntheticSolution", "SyntheticSolution.sln");

    private static RoslynWorkspaceConnectionConfiguration ValidLiveConfig() =>
        new()
        {
            SolutionPath = SyntheticSlnPath,
            ModeName = "Live"
        };

    private static RoslynWorkspaceConnectionConfiguration ValidSnapshotConfig() =>
        new()
        {
            SolutionPath = SyntheticSlnPath,
            ModeName = "Snapshot"
        };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceCore")]
    public async Task Create_MissingSolutionPath_ReturnsFailure()
    {
        var factory = new RoslynWorkspaceConnectionFactory(
            new Mock<IRoslynWorkspaceFactory>().Object,
            NullLogger<RoslynWorkspaceConnectionFactory>.Instance,
            NullLogger<RoslynWorkspaceConnection>.Instance);

        var config = new RoslynWorkspaceConnectionConfiguration
        {
            SolutionPath = "",
            ModeName = "Snapshot"
        };

        var result = await factory.Create(config, (ISecretManager?)null, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceCore")]
    public async Task Create_SolutionFileNotOnDisk_ReturnsFailure()
    {
        var factory = new RoslynWorkspaceConnectionFactory(
            new Mock<IRoslynWorkspaceFactory>().Object,
            NullLogger<RoslynWorkspaceConnectionFactory>.Instance,
            NullLogger<RoslynWorkspaceConnection>.Instance);

        var config = new RoslynWorkspaceConnectionConfiguration
        {
            SolutionPath = "/no/such/path/missing.sln",
            ModeName = "Snapshot"
        };

        var result = await factory.Create(config, (ISecretManager?)null, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceCore")]
    public async Task Create_MissingModeName_ReturnsFailure()
    {
        var factory = new RoslynWorkspaceConnectionFactory(
            new Mock<IRoslynWorkspaceFactory>().Object,
            NullLogger<RoslynWorkspaceConnectionFactory>.Instance,
            NullLogger<RoslynWorkspaceConnection>.Instance);

        var config = new RoslynWorkspaceConnectionConfiguration
        {
            SolutionPath = SyntheticSlnPath,
            ModeName = ""
        };

        var result = await factory.Create(config, (ISecretManager?)null, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceCore")]
    public async Task Create_UnknownModeName_ReturnsFailure()
    {
        var factory = new RoslynWorkspaceConnectionFactory(
            new Mock<IRoslynWorkspaceFactory>().Object,
            NullLogger<RoslynWorkspaceConnectionFactory>.Instance,
            NullLogger<RoslynWorkspaceConnection>.Instance);

        var config = new RoslynWorkspaceConnectionConfiguration
        {
            SolutionPath = SyntheticSlnPath,
            ModeName = "NotAMode"
        };

        var result = await factory.Create(config, (ISecretManager?)null, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceCore")]
    public async Task Create_SnapshotMode_ReturnsSnapshotConnectionWithoutLoadingWorkspace()
    {
        var workspaceFactoryMock = new Mock<IRoslynWorkspaceFactory>();

        var factory = new RoslynWorkspaceConnectionFactory(
            workspaceFactoryMock.Object,
            NullLogger<RoslynWorkspaceConnectionFactory>.Instance,
            NullLogger<RoslynWorkspaceConnection>.Instance);

        var result = await factory.Create(ValidSnapshotConfig(), (ISecretManager?)null, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeAssignableTo<IRoslynWorkspaceConnection>();
        var conn = (IRoslynWorkspaceConnection)result.Value!;
        conn.Mode.Name.ShouldBe("Snapshot");

        // Snapshot mode must NOT have loaded the workspace at creation time
        workspaceFactoryMock.Verify(
            f => f.CreateFromSolution(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceCore")]
    public async Task Create_SnapshotMode_GetGraph_LoadsAndDisposes()
    {
        var mockWorkspace = new Mock<IRoslynWorkspace>();
        mockWorkspace.As<IDisposable>().Setup(d => d.Dispose());
        mockWorkspace.Setup(w => w.CurrentSolution)
            .Returns(new Microsoft.CodeAnalysis.AdhocWorkspace().CurrentSolution);

        var workspaceFactoryMock = new Mock<IRoslynWorkspaceFactory>();
        workspaceFactoryMock
            .Setup(f => f.CreateFromSolution(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockWorkspace.Object);

        var factory = new RoslynWorkspaceConnectionFactory(
            workspaceFactoryMock.Object,
            NullLogger<RoslynWorkspaceConnectionFactory>.Instance,
            NullLogger<RoslynWorkspaceConnection>.Instance);

        var result = await factory.Create(ValidSnapshotConfig(), (ISecretManager?)null, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();

        var conn = (IRoslynWorkspaceConnection)result.Value!;

        // Execute GetGraph — this should load workspace once, then dispose it
        await conn.Client.GetGraph(CancellationToken.None);

        // Workspace was loaded exactly once
        workspaceFactoryMock.Verify(
            f => f.CreateFromSolution(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);

        mockWorkspace.As<IDisposable>().Verify(d => d.Dispose(), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceCore")]
    public async Task Create_SnapshotMode_CancellationPropagates()
    {
        using var cts = new CancellationTokenSource();

        var workspaceFactoryMock = new Mock<IRoslynWorkspaceFactory>();
        workspaceFactoryMock
            .Setup(f => f.CreateFromSolution(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, IReadOnlyList<string> _, CancellationToken ct) =>
            {
                await Task.Delay(5000, ct); // will be cancelled
                return (IRoslynWorkspace)new Mock<IRoslynWorkspace>().Object;
            });

        var factory = new RoslynWorkspaceConnectionFactory(
            workspaceFactoryMock.Object,
            NullLogger<RoslynWorkspaceConnectionFactory>.Instance,
            NullLogger<RoslynWorkspaceConnection>.Instance);

        var result = await factory.Create(ValidSnapshotConfig(), (ISecretManager?)null, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();

        var conn = (IRoslynWorkspaceConnection)result.Value!;

        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await conn.Client.GetGraph(cts.Token));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceCore")]
    public async Task Create_LiveMode_LoadsWorkspaceAtCreation()
    {
        var mockWorkspace = new Mock<IRoslynWorkspace>();
        mockWorkspace.Setup(w => w.CurrentSolution)
            .Returns(new Microsoft.CodeAnalysis.AdhocWorkspace().CurrentSolution);

        var workspaceFactoryMock = new Mock<IRoslynWorkspaceFactory>();
        workspaceFactoryMock
            .Setup(f => f.CreateFromSolution(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockWorkspace.Object);

        var factory = new RoslynWorkspaceConnectionFactory(
            workspaceFactoryMock.Object,
            NullLogger<RoslynWorkspaceConnectionFactory>.Instance,
            NullLogger<RoslynWorkspaceConnection>.Instance);

        var result = await factory.Create(ValidLiveConfig(), (ISecretManager?)null, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeAssignableTo<IRoslynWorkspaceConnection>();

        var conn = (IRoslynWorkspaceConnection)result.Value!;
        conn.Mode.Name.ShouldBe("Live");

        // Live mode loads workspace at factory-create time, not lazily
        workspaceFactoryMock.Verify(
            f => f.CreateFromSolution(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceCore")]
    public void SyncCreate_AlwaysReturnsFailure()
    {
        var factory = new RoslynWorkspaceConnectionFactory(
            new Mock<IRoslynWorkspaceFactory>().Object,
            NullLogger<RoslynWorkspaceConnectionFactory>.Instance,
            NullLogger<RoslynWorkspaceConnection>.Instance);

        var result = factory.Create(ValidSnapshotConfig());

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }
}
