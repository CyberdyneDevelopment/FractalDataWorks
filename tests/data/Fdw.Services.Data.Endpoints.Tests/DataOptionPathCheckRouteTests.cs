using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6566: the path check route resolves a stated path and method through the same check the rule service makes when a rule is added, over the real collections, and answers the
/// family and what the leaf takes, or the refusal that names the level that failed.
/// </summary>
public sealed class DataOptionPathCheckRouteTests
{
    private static readonly string[] InPath = ["DataConfigurationRules", "Comparison", "In"];

    [Fact]
    public async Task APathThatResolvesAnswersItsFamilyAndWhatTheLeafTakes()
    {
        var (status, check) = await Call(InPath, "To");

        status.ShouldBe(200);
        check.ShouldNotBeNull().Path.ShouldBe(InPath);
        check.Method.ShouldBe("To");
        check.Family.ShouldBe("Comparison");
        (check.MinValues, check.MaxValues).ShouldBe((1, null));
    }

    [Fact]
    public async Task ABetweenPathTakesExactlyTwoValues()
    {
        var (_, check) = await Call(["DataConfigurationRules", "Comparison", "Between"], "To");

        (check.ShouldNotBeNull().MinValues, check.MaxValues).ShouldBe((2, 2));
    }

    [Theory]
    [InlineData("Nothing", "Comparison", "In", 404)]
    [InlineData("DataConfigurationRules", "Nothing", "In", 404)]
    [InlineData("DataConfigurationRules", "Comparison", "Nothing", 404)]
    public async Task ALevelThatIsNotThereIsRefusedWithTheResolversNotFound(string root, string level, string kind, int expected)
    {
        var (status, check) = await Call([root, level, kind], "To");

        status.ShouldBe(expected);
        check.ShouldBeNull();
    }

    [Fact]
    public async Task APathThatEndsBeforeAKindIsRefusedNamingWhatLiesBelow()
    {
        var (status, _) = await Call(["DataConfigurationRules", "Comparison"], "To");

        status.ShouldBe(400);
    }

    [Fact]
    public async Task AMethodThatIsNotTheKindsIsRefused()
    {
        var (status, _) = await Call(InPath, "From");

        status.ShouldBeInRange(400, 499);
    }

    [Fact]
    public async Task AnEmptyLevelIsRefusedBeforeTheResolverIsAsked()
    {
        var (status, _) = await Call(["DataConfigurationRules", " ", "In"], "To");

        status.ShouldBe(400);
    }

    [Fact]
    public async Task ABodyWithNoPathOrNoMethodIsRefusedWithItsOwnCode()
    {
        var (noBody, _) = await Call(new CheckDataOptionPathRequest());
        var (noPath, _) = await Call(new CheckDataOptionPathRequest { Path = new DataOptionPathCheckBody { Method = "To" } });
        var (noMethod, _) = await Call(new CheckDataOptionPathRequest { Path = new DataOptionPathCheckBody { Path = [.. InPath] } });

        noBody.ShouldBe(400);
        noPath.ShouldBe(400);
        noMethod.ShouldBe(400);
    }

    private static Task<(int Status, DataOptionPathCheckPayload? Body)> Call(string[] path, string method)
        => Call(new CheckDataOptionPathRequest { Path = new DataOptionPathCheckBody { Path = [.. path], Method = method } });

    private static async Task<(int Status, DataOptionPathCheckPayload? Body)> Call(CheckDataOptionPathRequest request)
    {
        var endpoint = Factory.Create<Check>();
        await endpoint.HandleAsync(request, TestContext.Current.CancellationToken);
        var status = endpoint.HttpContext.Response.StatusCode;

        // The response is read only after a success: the getter builds a default payload when none was sent.
        return (status, status is >= 200 and < 300 ? endpoint.Response : null);
    }

    private sealed class Check : CheckDataOptionPathEndpointBase
    {
        public Check()
            : base(NullLogger.Instance)
        {
        }
    }
}
