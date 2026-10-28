using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Abstractions.Visualization;
using Fdw.Services.Data.Results;
using Fdw.Services.Data.Visualization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests.Visualization;

/// <summary>
/// FDW-2864: a statistic set the gateway refuses is answered with the gateway's own failure, so the status and the reason of its
/// code reach the caller, and a caller's bad input is not turned into an internal error.
/// </summary>
public sealed class StatSetServiceTests
{
    private const int ValidationCategory = 2;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AStatSetTheGatewayRefusesKeepsTheGatewaysCode()
    {
        var service = ServiceAnswering(Refusal());

        var result = await service.ComputeStatSet(
            new StatSetRequest { ContainerName = "Orders", DataStoreName = "Sales" }, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeSameAs(DataServiceResultCodes.ByName("DataStoreNameRequired"));
        (result.Code!.Id / 10000).ShouldBe(ValidationCategory);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AGroupedStatSetTheGatewayRefusesKeepsTheGatewaysCode()
    {
        var service = ServiceAnswering(Refusal());

        var result = await service.ComputeGroupedStatSet(
            new GroupedStatSetRequest { ContainerName = "Orders", DataStoreName = "Sales" }, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeSameAs(DataServiceResultCodes.ByName("DataStoreNameRequired"));
        (result.Code!.Id / 10000).ShouldBe(ValidationCategory);
    }

    private static IGenericResult<IEnumerable<Dictionary<string, object?>>> Refusal()
        => GenericResult<IEnumerable<Dictionary<string, object?>>>.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired"));

    private static StatSetService ServiceAnswering(IGenericResult<IEnumerable<Dictionary<string, object?>>> answer)
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(answer);
        var gateways = new Mock<IDataGatewayProvider>();
        gateways.Setup(p => p.ByName("Main")).Returns(gateway.Object);
        return new StatSetService(NullLogger<StatSetService>.Instance, gateways.Object);
    }
}
