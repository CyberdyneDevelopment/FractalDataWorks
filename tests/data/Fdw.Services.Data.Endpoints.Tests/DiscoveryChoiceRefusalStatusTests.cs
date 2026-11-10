using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// A discovery request that leaves out one of its choices is a validation refusal, so it answers 400 with DATA-21102 (FDW-4833).
/// It was DATA-71095, whose leading digit is the Dependency category, and answered 502 as if a downstream service had failed.
/// </summary>
public sealed class DiscoveryChoiceRefusalStatusTests
{
    private static readonly string[] Excluded = ["sys"];

    public static TheoryData<string> Choices => new() { "ExcludedSchemas", "DiscoverViews", "DiscoverIndexes", "DiscoverForeignKeys", "DiscoverDescriptions" };

    [Theory]
    [MemberData(nameof(Choices))]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AChoiceLeftOutAnswers400WithTheValidationCode(string leftOut)
    {
        var options = DataStoreDiscoveryChoices.FromRequest(
            leftOut == "ExcludedSchemas" ? null : Excluded,
            null,
            leftOut == "DiscoverViews" ? null : true,
            leftOut == "DiscoverIndexes" ? null : true,
            leftOut == "DiscoverForeignKeys" ? null : true,
            leftOut == "DiscoverDescriptions" ? null : true,
            "OpsDb",
            NullLogger.Instance);

        var (statusCode, problem) = ResultHttpStatusMapper.Map(options, new DefaultHttpContext());

        statusCode.ShouldBe(400);
        problem.Extensions["code"].ShouldBe("DATA-21102");
    }
}
