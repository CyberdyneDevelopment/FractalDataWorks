using System.Collections.Generic;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Web.RestEndpoints.Tests;

/// <summary>
/// Tests for ResultHttpStatusMapper.
/// HTTP status + retryability are derived from the result code's CATEGORY (number / 10000), the
/// authoritative source on <see cref="IResultCategory"/> — not per-code strings. Codes therefore
/// carry a real Id (the categorized number); the string Code is only echoed back for diagnostics.
/// </summary>
public class ResultHttpStatusMapperTests
{
    // Representative code number per handling category (leading digit = category Id).
    private const int ValidationCode = 20001;   // category 2 -> 400
    private const int MissingCode = 30001;      // category 3 -> 404
    private const int ConflictCode = 40001;     // category 4 -> 409
    private const int AuthCode = 50002;         // category 5 -> 401 (real LoginFailedCode number)
    private const int ConfigurationCode = 60001;// category 6 -> 500
    private const int DependencyCode = 70001;   // category 7 -> 502 (retryable)
    private const int TransientCode = 80001;    // category 8 -> 503 (retryable)
    private const int InternalCode = 90001;     // category 9 -> 500
    private const int LegacyCode = 3012;        // < 10000, uncategorized -> default 500

    private static HttpContext CreateHttpContext(string traceId = "test-trace-123")
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = traceId;
        return context;
    }

    // Why: the mapper reads code.Id (the categorized number) to resolve the category; the string
    // Code is echoed to the client. Both are set so the test exercises the real derivation path.
    private static IGenericResult CreateFailureWithCode(int id, string code)
    {
        var mockCode = new Mock<IResultCode>();
        mockCode.Setup(c => c.Id).Returns(id);
        mockCode.Setup(c => c.Code).Returns(code);

        var mockResult = new Mock<IGenericResult>();
        mockResult.Setup(r => r.IsSuccess).Returns(false);
        mockResult.Setup(r => r.IsFailure).Returns(true);
        mockResult.Setup(r => r.Code).Returns(mockCode.Object);
        mockResult.Setup(r => r.CodeChain).Returns(new List<IResultCode>());
        mockResult.Setup(r => r.Messages).Returns(new List<IGenericMessage>());
        return mockResult.Object;
    }

    private static IGenericResult CreateFailureWithNoCode()
    {
        var mockResult = new Mock<IGenericResult>();
        mockResult.Setup(r => r.IsSuccess).Returns(false);
        mockResult.Setup(r => r.IsFailure).Returns(true);
        mockResult.Setup(r => r.Code).Returns((IResultCode?)null);
        mockResult.Setup(r => r.CodeChain).Returns(new List<IResultCode>());
        mockResult.Setup(r => r.Messages).Returns(new List<IGenericMessage>());
        return mockResult.Object;
    }

    #region Category -> Status Code Mapping

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    [InlineData(ValidationCode, 400)]
    [InlineData(MissingCode, 404)]
    [InlineData(ConflictCode, 409)]
    [InlineData(AuthCode, 401)]
    [InlineData(ConfigurationCode, 500)]
    [InlineData(DependencyCode, 502)]
    [InlineData(TransientCode, 503)]
    [InlineData(InternalCode, 500)]
    public void MapDerivesHttpStatusFromCategory(int codeNumber, int expectedStatus)
    {
        // Arrange
        var result = CreateFailureWithCode(codeNumber, $"TEST-{codeNumber}");
        var context = CreateHttpContext();

        // Act
        var (statusCode, response) = ResultHttpStatusMapper.Map(result, context);

        // Assert
        statusCode.ShouldBe(expectedStatus);
        response.Detail.ShouldNotBeNullOrEmpty();
        response.Extensions["code"].ShouldBe($"TEST-{codeNumber}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void MapLegacyUncategorizedCodeReturns500()
    {
        // Arrange -- a code whose number is below the 10000 category band
        var result = CreateFailureWithCode(LegacyCode, "MSSQL_LEGACY");
        var context = CreateHttpContext();

        // Act
        var (statusCode, response) = ResultHttpStatusMapper.Map(result, context);

        // Assert
        statusCode.ShouldBe(500);
        response.Detail.ShouldBe("An unexpected error occurred");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void MapNoCodeReturns500()
    {
        // Arrange
        var result = CreateFailureWithNoCode();
        var context = CreateHttpContext();

        // Act
        var (statusCode, response) = ResultHttpStatusMapper.Map(result, context);

        // Assert
        statusCode.ShouldBe(500);
        response.Extensions["code"].ShouldBe("UNKNOWN_ERROR");
    }

    #endregion

    #region No Sensitive Information

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData(AuthCode)]
    [InlineData(MissingCode)]
    [InlineData(DependencyCode)]
    [InlineData(TransientCode)]
    [InlineData(ConfigurationCode)]
    [InlineData(InternalCode)]
    public void MapNeverLeaksSensitiveInfo(int codeNumber)
    {
        // Arrange
        var result = CreateFailureWithCode(codeNumber, $"TEST-{codeNumber}");
        var context = CreateHttpContext();

        // Act
        var (_, response) = ResultHttpStatusMapper.Map(result, context);

        // Assert -- no server addresses, SQL text, or usernames
        response.Detail.ShouldNotContain("SELECT");
        response.Detail.ShouldNotContain("INSERT");
        response.Detail.ShouldNotContain("DELETE");
        response.Detail.ShouldNotContain("UPDATE");
        response.Detail.ShouldNotContain("10.10.10");
        response.Detail.ShouldNotContain("localhost");
        response.Detail.ShouldNotContain("sa ");
        response.Detail.ShouldNotContain("password", Case.Insensitive);
        response.Detail.ShouldNotContain("connection string", Case.Insensitive);
    }

    #endregion

    #region ReferenceId Population

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void MapReferenceIdPopulatedFromTraceIdentifier()
    {
        // Arrange
        var traceId = "custom-trace-abc-456";
        var result = CreateFailureWithCode(AuthCode, "TEST-AUTH");
        var context = CreateHttpContext(traceId);

        // Act
        var (_, response) = ResultHttpStatusMapper.Map(result, context);

        // Assert
        response.Extensions["referenceId"].ShouldBe(traceId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void MapReferenceIdAlwaysPopulated()
    {
        // Arrange
        var result = CreateFailureWithCode(TransientCode, "TEST-TRANSIENT");
        var context = CreateHttpContext();

        // Act
        var (_, response) = ResultHttpStatusMapper.Map(result, context);

        // Assert
        (response.Extensions["referenceId"] as string).ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region IsRetryable Flag (from category)

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    [InlineData(AuthCode, false)]
    [InlineData(MissingCode, false)]
    [InlineData(ValidationCode, false)]
    [InlineData(ConflictCode, false)]
    [InlineData(ConfigurationCode, false)]
    [InlineData(InternalCode, false)]
    [InlineData(DependencyCode, true)]
    [InlineData(TransientCode, true)]
    public void MapIsRetryableFromCategory(int codeNumber, bool expectedRetryable)
    {
        // Arrange
        var result = CreateFailureWithCode(codeNumber, $"TEST-{codeNumber}");
        var context = CreateHttpContext();

        // Act
        var (_, response) = ResultHttpStatusMapper.Map(result, context);

        // Assert
        response.Extensions["isRetryable"].ShouldBe(expectedRetryable);
    }

    #endregion

    #region Code Extraction Fallbacks

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void MapUsesCodeChainWhenPrimaryCodeNull()
    {
        // Arrange -- no primary Code, but CodeChain has a categorized code
        var mockChainCode = new Mock<IResultCode>();
        mockChainCode.Setup(c => c.Id).Returns(ConflictCode);
        mockChainCode.Setup(c => c.Code).Returns("TEST-CONFLICT");

        var mockResult = new Mock<IGenericResult>();
        mockResult.Setup(r => r.IsFailure).Returns(true);
        mockResult.Setup(r => r.Code).Returns((IResultCode?)null);
        mockResult.Setup(r => r.CodeChain).Returns(new List<IResultCode> { mockChainCode.Object });
        mockResult.Setup(r => r.Messages).Returns(new List<IGenericMessage>());

        var context = CreateHttpContext();

        // Act
        var (statusCode, _) = ResultHttpStatusMapper.Map(mockResult.Object, context);

        // Assert -- should find the code in the chain and map to its category (409)
        statusCode.ShouldBe(409);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void MapMessageOnlyCodeReturns500()
    {
        // Arrange -- no primary Code, no CodeChain; only a message string code (no Id, so no category)
        var mockMessage = new Mock<IGenericMessage>();
        mockMessage.Setup(m => m.Code).Returns("TEST-90001");

        var mockResult = new Mock<IGenericResult>();
        mockResult.Setup(r => r.IsFailure).Returns(true);
        mockResult.Setup(r => r.Code).Returns((IResultCode?)null);
        mockResult.Setup(r => r.CodeChain).Returns(new List<IResultCode>());
        mockResult.Setup(r => r.Messages).Returns(new List<IGenericMessage> { mockMessage.Object });

        var context = CreateHttpContext();

        // Act
        var (statusCode, _) = ResultHttpStatusMapper.Map(mockResult.Object, context);

        // Assert -- a bare message code carries no Id, so it cannot resolve a category -> default 500
        statusCode.ShouldBe(500);
    }

    #endregion
}
