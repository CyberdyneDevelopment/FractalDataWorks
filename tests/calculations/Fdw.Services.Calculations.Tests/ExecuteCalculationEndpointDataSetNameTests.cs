using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Calculations.Endpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Web.Calculations.Clients.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// Tests for how POST /calculations/execute treats the data set name it is given (FDW-3238): the name is resolved the way
/// /data resolves it, so the name of a view is found as well as the name of a data set, and an unknown name is a 404.
/// </summary>
public sealed class ExecuteCalculationEndpointDataSetNameTests
{
    private static ExecuteCalculationRequest Request(string dataSetName)
        => new() { CalculationType = "Sum", DataSetName = dataSetName, Values = [1m, 2m, 3m] };

    private static (ExecuteProbe Endpoint, Mock<IDataSetResolver> Resolver) Create()
    {
        var resolver = new Mock<IDataSetResolver>();
        return (Factory.Create<ExecuteProbe>(resolver.Object), resolver);
    }

    [Fact]
    public async Task TheNameOfADataSetOrOfAViewIsFoundAndTheCalculationRuns()
    {
        var (endpoint, resolver) = Create();
        resolver.Setup(r => r.Resolve("AfcQuarterbacks", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ResolvedDataSet?>.Success(new ResolvedDataSet(Mock.Of<IDataSetImplementationConfiguration>(), null, "AfcPlayers", [])));

        await endpoint.HandleAsync(Request("AfcQuarterbacks"), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        resolver.Verify(r => r.Resolve("AfcQuarterbacks", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ANameNothingHasIsAnswered404()
    {
        var (endpoint, resolver) = Create();
        resolver.Setup(r => r.Resolve("Nothing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ResolvedDataSet?>.Success(null));

        await endpoint.HandleAsync(Request("Nothing"), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task AResolveThatFailsIsAnsweredFromItsFailureNotAsANotFound()
    {
        var (endpoint, resolver) = Create();
        resolver.Setup(r => r.Resolve("Both", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ResolvedDataSet?>.Failure(new GenericMessage("a data set and a view both have the name")));

        await endpoint.HandleAsync(Request("Both"), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldNotBe(200);
        endpoint.HttpContext.Response.StatusCode.ShouldNotBe(404);
    }

    [Fact]
    public async Task ARequestWithNoDataSetNameResolvesNothing()
    {
        var (endpoint, resolver) = Create();

        await endpoint.HandleAsync(Request(string.Empty), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        resolver.Verify(r => r.Resolve(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class ExecuteProbe(IDataSetResolver resolver) : ExecuteCalculationEndpointBase(NullLogger.Instance, resolver)
    {
        protected override void ConfigureEndpoint()
        {
        }
    }
}
