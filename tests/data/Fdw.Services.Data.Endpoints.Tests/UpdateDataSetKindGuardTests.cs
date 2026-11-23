using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6129: <c>PATCH /datasets/{name}</c> moves a data set between Simple, Compound and Federated as its sources change. A Graph data set and a Variation have no
/// sources that make their kind, so the route refuses them with <c>ImplementationChangeNotAllowed</c> and writes nothing: the kind the sources make would otherwise
/// move them to Simple and drop what Simple has no place for. The data set provider is a stand-in, so what is asserted is the refusal and that nothing is saved.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataIntegrity")]
public sealed class UpdateDataSetKindGuardTests
{
    private readonly Mock<IDataSetConfigurationProvider> _dataSets = new();

    private sealed class Route(IDataSetConfigurationProvider dataSets)
        : UpdateDataSetEndpointBase(NullLogger<UpdateDataSetEndpointBase>.Instance, dataSets, new Mock<ICategoryConfigurationProvider>().Object, new Mock<IDataStoreConfigurationProvider>().Object)
    {
        public Task<IGenericResult<DataSetDetailResponse>> Patch(UpdateDataSetRequest request) => SaveUpdatedDataSet(request, CancellationToken.None);
    }

    private async Task<IGenericResult<DataSetDetailResponse>> Patch(IDataSetImplementationConfiguration stored, UpdateDataSetRequest request)
    {
        _dataSets
            .Setup(p => p.Get(stored.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(stored));
        return await new Route(_dataSets.Object).Patch(request);
    }

    [Fact]
    public async Task APatchOnAVariationIsRefusedAndNothingIsSaved()
    {
        var variation = new VariationDataSetConfiguration { Name = "OrdersEast", Implementation = VariationDataSetConfiguration.ImplementationName };

        var patched = await Patch(variation, new UpdateDataSetRequest { Name = "OrdersEast", DisplayName = "Orders, east" });

        patched.IsFailure.ShouldBeTrue();
        patched.Code!.Name.ShouldBe("ImplementationChangeNotAllowed");
        _dataSets.Verify(
            p => p.Update(It.IsAny<IDataSetImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task APatchOnAGraphDataSetIsRefusedAndNothingIsSaved()
    {
        var graph = new GraphDataSetConfiguration { Name = "GasPrices", Implementation = GraphDataSetConfiguration.ImplementationName };

        var patched = await Patch(graph, new UpdateDataSetRequest { Name = "GasPrices", Abbreviation = "GP" });

        patched.IsFailure.ShouldBeTrue();
        patched.Code!.Name.ShouldBe("ImplementationChangeNotAllowed");
        _dataSets.Verify(
            p => p.Update(It.IsAny<IDataSetImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Simple", true)]
    [InlineData("Compound", true)]
    [InlineData("Federated", true)]
    [InlineData("Graph", false)]
    [InlineData("Variation", false)]
    public void OnlyTheKindsTheSourcesMakeFollowTheirSources(string implementation, bool follows)
    {
        DataSetKindDerivation.FollowsItsSources(implementation).ShouldBe(follows);
    }
}
