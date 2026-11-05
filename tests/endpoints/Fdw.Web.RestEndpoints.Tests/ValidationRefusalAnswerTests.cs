using FluentValidation.Results;
using Fdw.Results;
using Fdw.Validation;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Web.RestEndpoints.Tests;

/// <summary>
/// A request FluentValidation refused, turned into a result with <c>ToGenericResult</c>, is answered 400 with the failed rules as the
/// reason (FDW-3539). It was a 500 UNKNOWN_ERROR, because the refusal's message code "VALIDATION" carried no number.
/// </summary>
public sealed class ValidationRefusalAnswerTests
{
    private static ValidationResult Refused() => new(
    [
        new ValidationFailure("Name", "Name is required"),
        new ValidationFailure("Port", "Port must be greater than 0"),
    ]);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ARefusedRequestIsA400ThatNamesEveryFailedRule()
    {
        var (statusCode, response) = ResultHttpStatusMapper.Map(Refused().ToGenericResult(NullLogger.Instance), new DefaultHttpContext());

        statusCode.ShouldBe(400);
        response.Title.ShouldBe("The request was invalid");
        response.Detail.ShouldNotBeNull().ShouldContain("Name: Name is required");
        response.Detail.ShouldContain("Port: Port must be greater than 0");
        response.Extensions["code"].ShouldBe("VALIDATION-20001");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ARefusedRequestWithAValueIsA400TooNotA500()
    {
        var result = Refused().ToGenericResult("value", NullLogger.Instance);

        var (statusCode, response) = ResultHttpStatusMapper.Map(result, new DefaultHttpContext());

        statusCode.ShouldBe(400);
        response.Extensions["code"].ShouldNotBe("UNKNOWN_ERROR");
    }
}
