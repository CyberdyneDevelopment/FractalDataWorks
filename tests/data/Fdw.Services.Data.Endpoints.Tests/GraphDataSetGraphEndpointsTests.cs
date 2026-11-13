using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>FDW-5295: the two routes of the story. The component types list is the catalogue minus the refused families; the graph route refuses what is not a Graph data set.</summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataSets.FDW5295")]
public sealed class GraphDataSetGraphEndpointsTests
{
    private sealed class ComponentTypesEndpoint : ListGraphComponentTypesEndpointBase
    {
        public ComponentTypesEndpoint() : base(NullLogger<ListGraphComponentTypesEndpointBase>.Instance)
        {
        }

        public Task<IGenericResult<List<GraphComponentTypePayload>>> Load(CancellationToken ct) => LoadItems(ct);
    }

    private sealed class GraphEndpoint : GetGraphDataSetGraphEndpointBase
    {
        public GraphEndpoint(IDataSetConfigurationProvider dataSets)
            : base(NullLogger<GetGraphDataSetGraphEndpointBase>.Instance, dataSets, new Mock<ICategoryConfigurationProvider>().Object,
                new Mock<IFrameworkAuthorizationService>().Object)
        {
        }

        public Task<IGenericResult<GraphDataSetGraphPayload?>> Find(string name, CancellationToken ct) => FindByIdentifier(new DataSetNameRequest { Name = name }, ct);
    }

    private static Mock<IDataSetConfigurationProvider> Providing(IDataSetImplementationConfiguration? found)
    {
        var provider = new Mock<IDataSetConfigurationProvider>();
        provider.Setup(p => p.Get("GasPrices", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(found!));
        return provider;
    }

    [Fact]
    public async Task NoRefusedFamilyIsListedAndEveryOtherComponentTypeIs()
    {
        var answer = await new ComponentTypesEndpoint().Load(TestContext.Current.CancellationToken);

        var listed = answer.Value.ShouldNotBeNull().Select(type => type.Name).ToList();
        var refused = GraphRefusedComponentKinds.All().SelectMany(kind => kind.ComponentTypeNames).ToList();
        refused.ShouldNotBeEmpty();
        listed.ShouldNotContain(name => refused.Contains(name, System.StringComparer.OrdinalIgnoreCase));
        listed.ShouldBe([.. ComponentTypes.All().Select(type => type.Name).Where(name => !refused.Contains(name, System.StringComparer.OrdinalIgnoreCase))]);
    }

    [Fact]
    public async Task ASourceAndAJoinAreListed()
    {
        var answer = await new ComponentTypesEndpoint().Load(TestContext.Current.CancellationToken);

        var listed = answer.Value.ShouldNotBeNull().Select(type => type.Name).ToList();
        listed.ShouldContain("DataSetSource");
        listed.ShouldContain("MergeJoin");
    }

    [Fact]
    public async Task ADataSetThatDoesNotExistIsNotFound()
    {
        var answer = await new GraphEndpoint(Providing(null).Object).Find("GasPrices", TestContext.Current.CancellationToken);

        answer.IsSuccess.ShouldBeTrue();
        answer.Value.ShouldBeNull();
    }

    [Fact]
    public async Task ADataSetOfAnotherKindIsRefusedWithItsOwnCode()
    {
        var answer = await new GraphEndpoint(Providing(new SimpleDataSetConfiguration { Name = "GasPrices", Implementation = "Simple" }).Object)
            .Find("GasPrices", TestContext.Current.CancellationToken);

        answer.IsFailure.ShouldBeTrue();
        answer.Code.ShouldBeOfType<GraphDataSetNotGraphCode>();
    }

    [Fact]
    public async Task AVariationOfAGraphDataSetIsRefusedAsNotGraphAndNamesNeitherItsRootNorItsGraph()
    {
        var provider = Providing(new VariationDataSetConfiguration
        {
            Name = "GasPrices", Implementation = VariationDataSetConfiguration.ImplementationName, SourceDataSetName = "RootGraph",
        });

        var answer = await new GraphEndpoint(provider.Object).Find("GasPrices", TestContext.Current.CancellationToken);

        answer.IsFailure.ShouldBeTrue();
        answer.Code.ShouldBeOfType<GraphDataSetNotGraphCode>();
        answer.CurrentMessage.ShouldNotBeNull().ShouldNotContain("RootGraph");
        provider.Verify(p => p.Get(It.Is<string>(name => name != "GasPrices"), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
