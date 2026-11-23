using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// A pipeline's description, and its parameters', live in the descriptions table and not on <c>pipe.GraphPipelineConfiguration</c> or <c>pipe.PipelineParameter</c> (FDW-7323, a group of
/// FDW-6208): the provider describes the pipeline in the unit of its save, its parameters as the things the pipeline owns, fills both once for every set of composed pipelines it reads,
/// and removes them with the pipeline. The text is kept under the Id of the domain row; a parameter's under its own Id. The writer and reader are strict stand-ins;
/// <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class PipelineDescriptionsTests
{
    private static readonly Guid PipelineId = Guid.Parse("00000000-0000-0000-0000-000000007321");
    private static readonly Guid FirstParameter = Guid.Parse("00000000-0000-0000-0000-000000007322");
    private static readonly Guid SecondParameter = Guid.Parse("00000000-0000-0000-0000-000000007323");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private PipelineServiceConfigurationProvider Provider()
        => new(
            NullLogger<PipelineServiceConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "pipe", "Pipeline",
            new UnavailablePlacement(), _writer.Object, _reader.Object);

    private sealed class Parameter(Guid id, string name, string? description) : IDescribedPipelineParameter
    {
        public Guid Id { get; } = id;

        public string Name { get; } = name;

        public string? Description { get; set; } = description;
    }

    private static Mock<IPipelineImplementationConfiguration> Pipeline(string? text, params Parameter[] parameters)
    {
        var pipeline = new Mock<IPipelineImplementationConfiguration>();
        pipeline.SetupProperty(p => p.Id, PipelineId);
        pipeline.SetupProperty(p => p.Name, "Consolidate");
        pipeline.SetupProperty(p => p.Description, text);
        pipeline.SetupGet(p => p.DescribedParameters).Returns(parameters);
        return pipeline;
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteAndReadObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var provider = Provider();

        provider.ShouldBeAssignableTo<IConfigurationWriteObserver<IPipelineImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IConfigurationReadObserver<IPipelineImplementationConfiguration>>();
        provider.ShouldBeAssignableTo<IReferencedConfigurationProvider<IPipelineImplementationConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASavedPipelineIsDescribedAndItsParametersAreKeptInStepAsThingsItOwns()
    {
        _writer
            .Setup(w => w.Describe("Pipeline", "Text", PipelineId, "Consolidate", "Loads every order and consolidates it", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        IReadOnlyList<OwnedDescription>? kept = null;
        _writer
            .Setup(w => w.DescribeOwned("PipelineParameter", "Text", It.Is<DescriptionOwner>(owner => owner.Id == PipelineId && owner.Name == "Consolidate"), It.IsAny<IReadOnlyList<OwnedDescription>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, DescriptionOwner, IReadOnlyList<OwnedDescription>, ConfigurationWriteScope, CancellationToken>((_, _, _, things, _, _) => kept = things)
            .ReturnsAsync(GenericResult.Success());
        var pipeline = Pipeline("Loads every order and consolidates it", new Parameter(FirstParameter, "Region", "The region to load"), new Parameter(SecondParameter, "Day", null));

        var written = await Provider().Written(pipeline.Object, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        kept.ShouldNotBeNull().ShouldBe([new OwnedDescription(FirstParameter, "Region", "The region to load"), new OwnedDescription(SecondParameter, "Day", null)]);
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionOfThePipelineFailsTheSaveAndNeverReachesTheParameters()
    {
        _writer
            .Setup(w => w.Describe("Pipeline", "Text", PipelineId, "Consolidate", "Loads every order and consolidates it", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Provider().Written(Pipeline("Loads every order and consolidates it").Object, null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnTheComposedPipelineAndOneReadOfTheOwnedPutsItOnItsParameters()
    {
        var region = new Parameter(FirstParameter, "Region", null);
        var day = new Parameter(SecondParameter, "Day", null);
        var pipeline = Pipeline(null, region, day).Object;
        _reader
            .Setup(r => r.Fill("Pipeline", "Text", It.IsAny<IReadOnlyList<IPipelineImplementationConfiguration>>(), It.IsAny<Func<IPipelineImplementationConfiguration, Guid>>(), It.IsAny<Action<IPipelineImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<IPipelineImplementationConfiguration>, Func<IPipelineImplementationConfiguration, Guid>, Action<IPipelineImplementationConfiguration, string>, CancellationToken>(
                (_, _, found, idOf, fill, _) =>
                {
                    foreach (var thing in found) fill(thing, "Loads every order and consolidates it");
                })
            .ReturnsAsync(GenericResult.Success());
        IReadOnlyDictionary<Guid, DescriptionEntry> held = new Dictionary<Guid, DescriptionEntry>
        {
            [FirstParameter] = new(new DescriptionKey("PipelineParameter", "Text", FirstParameter, "en-US"), "Region", new DescriptionOwner(PipelineId, "Consolidate"), "The region to load"),
        };
        _reader
            .Setup(r => r.ReadOwnedBy("PipelineParameter", "Text", It.Is<IReadOnlyCollection<Guid>>(owners => owners.Count == 1 && owners.Contains(PipelineId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(held));

        var read = await Provider().Read([pipeline], null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        pipeline.Description.ShouldBe("Loads every order and consolidates it");
        region.Description.ShouldBe("The region to load");
        day.Description.ShouldBeNull();
        _reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APipelineThatComposedNoParametersCostsNoReadOfTheOwned()
    {
        _reader
            .Setup(r => r.Fill("Pipeline", "Text", It.IsAny<IReadOnlyList<IPipelineImplementationConfiguration>>(), It.IsAny<Func<IPipelineImplementationConfiguration, Guid>>(), It.IsAny<Action<IPipelineImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var read = await Provider().Read([Pipeline(null).Object], null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        _reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheReadOfComposedPipelines()
    {
        _reader
            .Setup(r => r.Fill("Pipeline", "Text", It.IsAny<IReadOnlyList<IPipelineImplementationConfiguration>>(), It.IsAny<Func<IPipelineImplementationConfiguration, Guid>>(), It.IsAny<Action<IPipelineImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var read = await Provider().Read([Pipeline(null).Object], null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingAPipelineRemovesItsDescriptionsAndItsParametersInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("Pipeline", PipelineId, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        _writer.Setup(w => w.DeleteOwnedBy("PipelineParameter", PipelineId, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        (await Provider().RemoveReferences(PipelineId, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writer.VerifyAll();
    }
}
