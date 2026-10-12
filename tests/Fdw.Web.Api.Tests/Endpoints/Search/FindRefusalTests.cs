using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.Search.Clients.Models;
using Fdw.Web.Search.Endpoints;
using Microsoft.AspNetCore.Http;

namespace Fdw.Web.Api.Tests.Endpoints.Search;

/// <summary>
/// Tests for <see cref="FindEndpoint"/> (FDW-1162): a find the gateway refuses is the gateway's own failure, so it answers
/// 400 with the reason instead of a bare 404 that says the container does not exist.
/// </summary>
public sealed class FindRefusalTests
{
    private static readonly FindRequest Request = new()
    {
        DataStoreName = "PlatformConfiguration",
        PathName = "sec",
        ContainerName = "Secret",
        SearchTerm = "key",
        MaxResults = 5,
    };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARefusedFindIsTheGatewaysFailureAndAnswers400WithTheReason()
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<FindResult<Dictionary<string, object?>>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<FindResult<Dictionary<string, object?>>>>.Failure(
                DataServiceResultCodes.ByName("HostDeclaredStoreRefused"),
                ResultDetails.Create("dataStore", "PlatformConfiguration", "container", "Secret")));

        var found = await new FindEndpoint(Provider(gateway)).Find(Request, TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
        var (status, problem) = ResultHttpStatusMapper.Map(found, new DefaultHttpContext());
        status.ShouldBe(400);
        problem.Detail.ShouldNotBeNull();
        problem.Detail.ShouldContain("PlatformConfiguration");
        gateway.Verify(
            g => g.Execute<IEnumerable<FindResult<Dictionary<string, object?>>>>(
                It.IsAny<IDataCommand>(), new DataStoreTarget("PlatformConfiguration", "sec", "Secret"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AFindThatRanCarriesItsMatches()
    {
        IEnumerable<FindResult<Dictionary<string, object?>>> matches =
        [
            new() { Record = new Dictionary<string, object?> { ["Name"] = "Patriots" }, MatchedFields = ["Name"] },
        ];
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<FindResult<Dictionary<string, object?>>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<FindResult<Dictionary<string, object?>>>>.Success(matches));

        var found = await new FindEndpoint(Provider(gateway)).Find(Request, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue();
        var response = found.Value;
        response.ShouldNotBeNull();
        response.TotalCount.ShouldBe(1);
        response.ContainerName.ShouldBe("Secret");
        response.Results.Single().MatchedFields.ShouldBe(new[] { "Name" });
    }

    private static IDataGatewayProvider Provider(Mock<IDataGateway> gateway)
    {
        var provider = new Mock<IDataGatewayProvider>();
        provider.Setup(p => p.ByName("Main")).Returns(gateway.Object);
        return provider.Object;
    }
}
