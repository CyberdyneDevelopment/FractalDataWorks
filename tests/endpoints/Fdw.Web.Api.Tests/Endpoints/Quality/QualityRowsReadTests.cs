using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Results;
using Fdw.Services.Quality.Endpoints;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Quality;

/// <summary>
/// Tests for <see cref="QualityRuleTarget.ReadRows"/> (FDW-1162): a quality check whose rows the gateway refuses to read is
/// that failure, with its status and reason, and never a completed check reported as "Failed" in a 200.
/// </summary>
public sealed class QualityRowsReadTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARefusedReadIsTheGatewaysFailureAndAnswers400WithTheReason()
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object>>>(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object>>>.Failure(
                DataServiceResultCodes.ByName("HostDeclaredStoreRefused"),
                ResultDetails.Create("dataStore", "PlatformConfiguration", "container", "Secret")));

        var rows = await QualityRuleTarget.ReadRows(gateway.Object, "Secrets", NullLogger.Instance, TestContext.Current.CancellationToken);

        rows.IsFailure.ShouldBeTrue();
        var (status, problem) = ResultHttpStatusMapper.Map(rows, new DefaultHttpContext());
        status.ShouldBe(400);
        problem.Detail.ShouldNotBeNull();
        problem.Detail.ShouldContain("PlatformConfiguration");
        gateway.Verify(
            g => g.Execute<IEnumerable<Dictionary<string, object>>>(It.IsAny<IDataCommand>(), new DataSetTarget("Secrets"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AReadThatRanCarriesItsRows()
    {
        var read = new List<Dictionary<string, object>>
        {
            new() { ["Name"] = "Patriots" },
            new() { ["Name"] = "Jets" },
        };
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object>>>(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object>>>.Success(read));

        var rows = await QualityRuleTarget.ReadRows(gateway.Object, "NflTeams", NullLogger.Instance, TestContext.Current.CancellationToken);

        rows.IsSuccess.ShouldBeTrue();
        rows.Value.ShouldNotBeNull();
        rows.Value.Count.ShouldBe(2);
    }
}
