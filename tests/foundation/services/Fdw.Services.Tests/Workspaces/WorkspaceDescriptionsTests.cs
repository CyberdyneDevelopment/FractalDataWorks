using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// A workspace's description lives in the descriptions table and not in <c>workspace.WorkspaceConfiguration</c> (FDW-6208): the workspace provider describes the
/// workspace in the unit of its save, fills the text once for every set it reads, and removes it with the workspace; the list and detail routes map the filled text.
/// The writer and reader are stand-ins; <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
[Trait("Category", "Descriptions")]
public sealed class WorkspaceDescriptionsTests
{
    private static readonly Guid Quakes = Guid.Parse("00000000-0000-0000-0000-0000000000c1");
    private static readonly Guid Floods = Guid.Parse("00000000-0000-0000-0000-0000000000c2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private WorkspaceConfigurationProvider Provider()
        => new(
            NullLogger<WorkspaceConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>().Object, "PlatformConfiguration", "workspace", "Workspace",
            _writer.Object, _reader.Object);

    private static WorkspaceImplementationConfiguration Workspace(Guid id, string name, string? description = null)
        => new() { Id = id, Name = name, Description = description, OrganizationId = Guid.Parse("00000000-0000-0000-0000-0000000000e1") };

    // The reader answers with the texts it holds, put on each workspace through the provider's own Id and fill delegates.
    private void TheReaderHolds(Dictionary<Guid, string> texts)
        => _reader
            .Setup(r => r.Fill(
                "Workspace", "Text", It.IsAny<IReadOnlyList<IWorkspaceImplementationConfiguration>>(), It.IsAny<Func<IWorkspaceImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IWorkspaceImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<IWorkspaceImplementationConfiguration>, Func<IWorkspaceImplementationConfiguration, Guid>, Action<IWorkspaceImplementationConfiguration, string>, CancellationToken>(
                (_, _, workspaces, idOf, fill, _) =>
                {
                    foreach (var workspace in workspaces)
                    {
                        if (texts.TryGetValue(idOf(workspace), out var text)) fill(workspace, text);
                    }
                })
            .ReturnsAsync(GenericResult.Success());

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteAndReadObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<IWorkspaceImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationReadObserver<IWorkspaceImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<IWorkspaceImplementationConfiguration>>();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("Where the earthquake data lives")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedWorkspaceIsDescribedUnderTheWorkspaceTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("Workspace", "Text", Quakes, "Quakes", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Provider().Written(Workspace(Quakes, "Quakes", text), null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("Workspace", "Text", Quakes, "Quakes", "Where the earthquake data lives", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(Workspace(Quakes, "Quakes", "Where the earthquake data lives"), null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryWorkspaceOfTheSetAndAWorkspaceWithNoneStaysEmpty()
    {
        var workspaces = new List<IWorkspaceImplementationConfiguration> { Workspace(Quakes, "Quakes"), Workspace(Floods, "Floods") };
        TheReaderHolds(new Dictionary<Guid, string> { [Quakes] = "Where the earthquake data lives" });

        var read = await Provider().Read(workspaces, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        workspaces[0].Description.ShouldBe("Where the earthquake data lives");
        workspaces[1].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheWorkspaceRead()
    {
        _reader
            .Setup(r => r.Fill(
                "Workspace", "Text", It.IsAny<IReadOnlyList<IWorkspaceImplementationConfiguration>>(), It.IsAny<Func<IWorkspaceImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IWorkspaceImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var read = await Provider().Read([Workspace(Quakes, "Quakes")], null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task TheReadPassesTheCallersTokenToTheFill()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        _reader
            .Setup(r => r.Fill(
                "Workspace", "Text", It.IsAny<IReadOnlyList<IWorkspaceImplementationConfiguration>>(), It.IsAny<Func<IWorkspaceImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IWorkspaceImplementationConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());

        await Provider().Read([Workspace(Quakes, "Quakes")], null, cancelled.Token);

        _reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingAWorkspaceRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("Workspace", Quakes, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        var removed = await Provider().RemoveReferences(Quakes, null!, TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheListAndDetailRoutesCarryTheDescriptionTheProviderFilled()
    {
        var filled = Workspace(Quakes, "Quakes", "Where the earthquake data lives");
        var ownership = new WorkspaceOwnership(Guid.Parse("00000000-0000-0000-0000-0000000000f1"), null);

        WorkspaceResponseMapper.ToSummary(filled, ownership).Description.ShouldBe("Where the earthquake data lives");
        WorkspaceResponseMapper.ToDetail(filled, ownership).Description.ShouldBe("Where the earthquake data lives");
    }
}
