using Fdw.Services.Connections.Http.Abstractions.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Abstractions.Tests;

public class HttpResultCodesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllCodesHaveUniqueNames()
    {
        // Act
        var all = HttpResultCodes.All();
        var names = all.Select(c => c.Name).ToList();

        // Assert
        names.Distinct().Count().ShouldBe(names.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllCodesHaveUniqueCodes()
    {
        // Act
        var all = HttpResultCodes.All();
        var codes = all.Select(c => c.Code).ToList();

        // Assert
        codes.Distinct().Count().ShouldBe(codes.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllCodesHaveUniqueEventIds()
    {
        // Act
        var all = HttpResultCodes.All();
        var eventIds = all.Select(c => c.EventId).ToList();

        // Assert
        eventIds.Distinct().Count().ShouldBe(eventIds.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllCodesFollowCatalogInvariants()
    {
        // Codes are categorized numbers (resultcode-catalog): Code == "HTTP-{number}",
        // Id == EventId == number, Domain == "HTTP". Assert the invariants rather than a
        // fixed EventId band so a future renumber does not re-break this test.
        foreach (var code in HttpResultCodes.All())
        {
            if (string.Equals(code.Name, "NotFound", System.StringComparison.Ordinal))
            {
                continue;
            }

            code.Code.ShouldBe($"HTTP-{code.Id}");
            code.EventId.ShouldBe(code.Id);
            code.Domain.ShouldBe("HTTP");
        }
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    [InlineData("RequestTimeout", true)]
    [InlineData("RequestFailed", true)]
    [InlineData("HttpErrorResponse", true)]
    [InlineData("SoapHttpError", true)]
    [InlineData("GraphQLHttpError", true)]
    [InlineData("RequestCancelled", false)]
    [InlineData("UnexpectedError", false)]
    public void RetryableFlagsAreCorrect(string codeName, bool expectedRetryable)
    {
        // Act
        var code = HttpResultCodes.ByName(codeName);

        // Assert
        code.ShouldNotBeNull();
        code.IsRetryable.ShouldBe(expectedRetryable);
    }
}
