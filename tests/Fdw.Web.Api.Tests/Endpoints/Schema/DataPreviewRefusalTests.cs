using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Schema.Endpoints.Discovery;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;

namespace Fdw.Web.Api.Tests.Endpoints.Schema;

/// <summary>
/// Tests for <see cref="DataPreviewEndpointBase"/> (FDW-1162): a preview the gateway refuses is the gateway's own failure,
/// so it answers with that failure's status and reason, and is never an empty preview written beside an error.
/// </summary>
public sealed class DataPreviewRefusalTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARefusedContainerPreviewIsTheGatewaysFailureAndAnswers400WithTheReason()
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Refused());

        var preview = await new Probe(Provider(gateway)).Container("PlatformConfiguration", "sec", "Secret", TestContext.Current.CancellationToken);

        preview.IsFailure.ShouldBeTrue();
        var (status, problem) = ResultHttpStatusMapper.Map(preview, new DefaultHttpContext());
        status.ShouldBe(400);
        problem.Detail.ShouldNotBeNull();
        problem.Detail.ShouldContain("PlatformConfiguration");
        gateway.Verify(
            g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
                It.IsAny<IDataCommand>(), new DataStoreTarget("PlatformConfiguration", "sec", "Secret"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARefusedDataSetPreviewIsTheGatewaysFailureAndAnswers400WithTheReason()
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Refused());

        var preview = await new Probe(Provider(gateway)).DataSet("Secrets", TestContext.Current.CancellationToken);

        preview.IsFailure.ShouldBeTrue();
        var (status, problem) = ResultHttpStatusMapper.Map(preview, new DefaultHttpContext());
        status.ShouldBe(400);
        problem.Detail.ShouldNotBeNull();
        problem.Detail.ShouldContain("PlatformConfiguration");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AReadContainerPreviewCarriesItsRowsAndColumns()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "Patriots", ["City"] = "New England" },
            new() { ["Name"] = "Jets", ["City"] = "New York" },
        };
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows));

        var preview = await new Probe(Provider(gateway)).Container("NflDb", "NflData", "Team", TestContext.Current.CancellationToken);

        preview.IsSuccess.ShouldBeTrue();
        var response = preview.Value;
        response.ShouldNotBeNull();
        response.Source.ShouldBe("Container: NflDb/NflData/Team");
        response.RowCount.ShouldBe(2);
        response.Columns.Select(c => c.Name).ShouldBe(new[] { "Name", "City" });
        response.HasMoreRows.ShouldBeFalse();
    }

    // The refusal the main data gateway returns for a store the host declares for itself (FDW-1068).
    private static IGenericResult<IEnumerable<Dictionary<string, object?>>> Refused()
        => GenericResult<IEnumerable<Dictionary<string, object?>>>.Failure(
            DataServiceResultCodes.ByName("HostDeclaredStoreRefused"),
            ResultDetails.Create("dataStore", "PlatformConfiguration", "container", "Secret"));

    private static IDataGatewayProvider Provider(Mock<IDataGateway> gateway)
    {
        var provider = new Mock<IDataGatewayProvider>();
        provider.Setup(p => p.ByName("Main")).Returns(gateway.Object);
        return provider.Object;
    }

    // Opens the two reads to the tests; the endpoint itself answers through HTTP.
    private sealed class Probe(IDataGatewayProvider dataGateways) : DataPreviewEndpointBase(dataGateways)
    {
        public Task<IGenericResult<DataPreviewResponse>> Container(string dataStore, string path, string container, CancellationToken cancellationToken)
            => PreviewFromContainer(dataStore, path, container, 10, null, cancellationToken);

        public Task<IGenericResult<DataPreviewResponse>> DataSet(string dataSet, CancellationToken cancellationToken)
            => PreviewFromDataSet(dataSet, 10, null, cancellationToken);
    }
}
