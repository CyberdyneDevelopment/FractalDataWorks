using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Providers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Projects.Tests;

/// <summary>
/// An orchestration node's description lives in the descriptions table and not on <c>pipe.OrchestrationNodeConfiguration</c> (FDW-7323, a group of FDW-6208): the provider describes the member in the unit of its
/// save, fills the text once for every set of composed members it reads, and removes it with the member. The text is kept under the Id of the domain row. The writer and reader
/// are strict stand-ins; <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class OrchestrationNodeDescriptionsTests
{
    private static readonly Guid First = Guid.Parse("00000000-0000-0000-0000-000000007371");
    private static readonly Guid Second = Guid.Parse("00000000-0000-0000-0000-000000007372");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private OrchestrationNodeConfigurationProvider Provider()
        => new(
            NullLogger<OrchestrationNodeConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "pipe", "OrchestrationNode",
            _writer.Object, _reader.Object);

    private static OrchestrationNodeImplementationConfiguration Record(Guid id, string name, string? text = null)
        => new() { Id = id, Name = name, Description = text };

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteAndReadObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<IOrchestrationNodeImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationReadObserver<IOrchestrationNodeImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<IOrchestrationNodeImplementationConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheImplementationsMapperHasNoDescriptionSoNoReadSelectsTheColumnAndNoSaveWritesIt()
    {
        var mapper = PocoMapperCollection.ByName(nameof(OrchestrationNodeImplementationConfiguration));

        mapper.ShouldNotBeSameAs(PocoMapperCollection.NotFound);
        mapper.GetPropertyNames().ShouldNotContain("Description");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("Runs the nightly stage in order")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedMemberIsDescribedUnderTheTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("OrchestrationNode", "Text", First, "Nightly", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Provider().Written(Record(First, "Nightly", text), null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("OrchestrationNode", "Text", First, "Nightly", "Runs the nightly stage in order", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(Record(First, "Nightly", "Runs the nightly stage in order"), null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryComposedMemberAndOneWithNoneStaysEmpty()
    {
        var things = new List<IOrchestrationNodeImplementationConfiguration> { Record(First, "Nightly"), Record(Second, "other") };
        _reader
            .Setup(r => r.Fill("OrchestrationNode", "Text", It.IsAny<IReadOnlyList<IOrchestrationNodeImplementationConfiguration>>(), It.IsAny<Func<IOrchestrationNodeImplementationConfiguration, Guid>>(), It.IsAny<Action<IOrchestrationNodeImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<IOrchestrationNodeImplementationConfiguration>, Func<IOrchestrationNodeImplementationConfiguration, Guid>, Action<IOrchestrationNodeImplementationConfiguration, string>, CancellationToken>(
                (_, _, found, idOf, fill, _) =>
                {
                    foreach (var thing in found)
                    {
                        if (idOf(thing) == First) fill(thing, "Runs the nightly stage in order");
                    }
                })
            .ReturnsAsync(GenericResult.Success());

        var read = await Provider().Read(things, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        things[0].Description.ShouldBe("Runs the nightly stage in order");
        things[1].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheReadOfComposedMembers()
    {
        _reader
            .Setup(r => r.Fill("OrchestrationNode", "Text", It.IsAny<IReadOnlyList<IOrchestrationNodeImplementationConfiguration>>(), It.IsAny<Func<IOrchestrationNodeImplementationConfiguration, Guid>>(), It.IsAny<Action<IOrchestrationNodeImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var read = await Provider().Read([Record(First, "Nightly")], null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingAMemberRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("OrchestrationNode", First, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        (await Provider().RemoveReferences(First, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writer.VerifyAll();
    }
}
