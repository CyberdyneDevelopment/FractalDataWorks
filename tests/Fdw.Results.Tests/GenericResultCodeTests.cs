using Fdw.Messages;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Results.Tests;

/// <summary>
/// Tests for GenericResult and GenericResult{T} with IResultCode paths.
/// </summary>
public class GenericResultCodeTests
{
    private readonly ILogger _logger = NullLogger.Instance;

    private static Mock<IResultCode> CreateMockResultCode(bool isSuccess = false, string code = "TEST_CODE", string domain = "Test")
    {
        var mockSeverity = new Mock<IResultSeverity>();
        mockSeverity.Setup(s => s.IsSuccess).Returns(isSuccess);

        var mockCode = new Mock<IResultCode>();
        mockCode.Setup(c => c.Severity).Returns(mockSeverity.Object);
        mockCode.Setup(c => c.Code).Returns(code);
        mockCode.Setup(c => c.Domain).Returns(domain);
        mockCode.Setup(c => c.FormatMessage(It.IsAny<IResultDetails?>())).Returns("Formatted message");
        return mockCode;
    }

    #region GenericResult with IResultCode

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void SuccessWithResultCode_SetsCodeAndIsSuccess()
    {
        // Arrange
        var mockCode = CreateMockResultCode(isSuccess: true);

        // Act
        var result = GenericResult.Success(mockCode.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Code.ShouldBe(mockCode.Object);
        result.Messages.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FailureWithResultCode_SetsCodeAndIsFailure()
    {
        // Arrange
        var mockCode = CreateMockResultCode(isSuccess: false);

        // Act
        var result = GenericResult.Failure(mockCode.Object);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBe(mockCode.Object);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FailureWithResultCodeAndDetails_SetsDetailsProperty()
    {
        // Arrange
        var mockCode = CreateMockResultCode();
        var details = ResultDetails.Create("StatusCode", 500);

        // Act
        var result = GenericResult.Failure(mockCode.Object, details);

        // Assert
        result.Details.ShouldBe(details);
        result.Code.ShouldBe(mockCode.Object);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FailureWithResultCodeAndLogger_CallsLogOnCode()
    {
        // Arrange
        var mockCode = CreateMockResultCode();

        // Act
        var result = GenericResult.Failure(mockCode.Object, _logger);

        // Assert
        result.IsFailure.ShouldBeTrue();
        mockCode.Verify(c => c.Log(_logger, null), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FailureWithResultCodeLoggerAndDetails_CallsLogWithDetails()
    {
        // Arrange
        var mockCode = CreateMockResultCode();
        var details = ResultDetails.Create("Key", "Value");

        // Act
        var result = GenericResult.Failure(mockCode.Object, _logger, details);

        // Assert
        result.IsFailure.ShouldBeTrue();
        mockCode.Verify(c => c.Log(_logger, details), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void SuccessWithResultCode_CreatesMessageWithCodeAndSource()
    {
        // Arrange
        var mockCode = CreateMockResultCode(isSuccess: true, code: "OK", domain: "System");

        // Act
        var result = GenericResult.Success(mockCode.Object);

        // Assert
        result.Messages[0].Code.ShouldBe("OK");
        result.Messages[0].Source.ShouldBe("System");
        // GenericMessage stores severity - cast to verify
        var msg = result.Messages[0].ShouldBeOfType<GenericMessage>();
        msg.Severity.ShouldBe(MessageSeverity.Information);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FailureWithResultCode_CreatesErrorSeverityMessage()
    {
        // Arrange
        var mockCode = CreateMockResultCode(isSuccess: false, code: "ERR", domain: "System");

        // Act
        var result = GenericResult.Failure(mockCode.Object);

        // Assert
        result.Messages[0].Code.ShouldBe("ERR");
        var msg = result.Messages[0].ShouldBeOfType<GenericMessage>();
        msg.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithNullCode_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => GenericResult.Success((IResultCode)null!));
    }

    #endregion

    #region Chain Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Chain_SetsInnerResultAndCode()
    {
        // Arrange
        var innerCode = CreateMockResultCode(isSuccess: false, code: "INNER");
        var outerCode = CreateMockResultCode(isSuccess: false, code: "OUTER");
        var innerResult = GenericResult.Failure(innerCode.Object);

        // Act
        var result = GenericResult.Chain(outerCode.Object, innerResult);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBe(outerCode.Object);
        result.InnerResult.ShouldBe(innerResult);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Chain_CopiesMessagesFromInnerResult()
    {
        // Arrange
        var innerResult = GenericResult.Failure(new GenericMessage("Inner error"));
        var outerCode = CreateMockResultCode(isSuccess: false, code: "OUTER");

        // Act
        var result = GenericResult.Chain(outerCode.Object, innerResult);

        // Assert
        // Should have outer message + inner message
        result.Messages.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ChainWithLogger_CallsLogOnCode()
    {
        // Arrange
        var innerResult = GenericResult.Failure(new GenericMessage("Inner error"));
        var outerCode = CreateMockResultCode(isSuccess: false);

        // Act
        var result = GenericResult.Chain(outerCode.Object, innerResult, _logger);

        // Assert
        outerCode.Verify(c => c.Log(_logger, null), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ChainWithLoggerAndDetails_CallsLogWithDetails()
    {
        // Arrange
        var innerResult = GenericResult.Failure(new GenericMessage("Inner error"));
        var outerCode = CreateMockResultCode(isSuccess: false);
        var details = ResultDetails.Create("Context", "Save");

        // Act
        var result = GenericResult.Chain(outerCode.Object, innerResult, _logger, details);

        // Assert
        outerCode.Verify(c => c.Log(_logger, details), Times.Once);
    }

    #endregion

    #region CodeChain and RootCause Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CodeChain_ReturnsSingleCodeWhenNoInnerResult()
    {
        // Arrange
        var mockCode = CreateMockResultCode(isSuccess: false, code: "ONLY");
        var result = GenericResult.Failure(mockCode.Object);

        // Act
        var chain = result.CodeChain;

        // Assert
        chain.Count.ShouldBe(1);
        chain[0].ShouldBe(mockCode.Object);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CodeChain_ReturnsCodesFromOuterToInner()
    {
        // Arrange
        var innerCode = CreateMockResultCode(isSuccess: false, code: "INNER");
        var outerCode = CreateMockResultCode(isSuccess: false, code: "OUTER");
        var innerResult = GenericResult.Failure(innerCode.Object);
        var outerResult = GenericResult.Chain(outerCode.Object, innerResult);

        // Act
        var chain = outerResult.CodeChain;

        // Assert
        chain.Count.ShouldBe(2);
        chain[0].ShouldBe(outerCode.Object);
        chain[1].ShouldBe(innerCode.Object);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CodeChain_ReturnsEmptyWhenNoCode()
    {
        // Arrange
        var result = GenericResult.Failure(new GenericMessage("no code"));

        // Act
        var chain = result.CodeChain;

        // Assert
        chain.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void RootCause_ReturnsSelfWhenNoInnerResult()
    {
        // Arrange
        var result = GenericResult.Failure(new GenericMessage("error"));

        // Act
        var root = result.RootCause;

        // Assert
        root.ShouldBe(result);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void RootCause_ReturnsInnermostResult()
    {
        // Arrange
        var innerCode = CreateMockResultCode(isSuccess: false, code: "INNER");
        var outerCode = CreateMockResultCode(isSuccess: false, code: "OUTER");
        var innerResult = GenericResult.Failure(innerCode.Object);
        var outerResult = GenericResult.Chain(outerCode.Object, innerResult);

        // Act
        var root = outerResult.RootCause;

        // Assert
        root.ShouldBe(innerResult);
    }

    #endregion

    #region GenericResult<T> with IResultCode

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GenericOfTSuccessWithResultCode_SetsCodeAndValue()
    {
        // Arrange
        var mockCode = CreateMockResultCode(isSuccess: true);

        // Act
        var result = GenericResult<int>.Success(42, mockCode.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        result.Code.ShouldBe(mockCode.Object);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GenericOfTFailureWithResultCode_ThrowsOnValueAccess()
    {
        // Arrange
        var mockCode = CreateMockResultCode(isSuccess: false);

        // Act
        var result = GenericResult<int>.Failure(mockCode.Object);

        // Assert
        result.IsFailure.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GenericOfTFailureWithResultCodeAndLogger_CallsLog()
    {
        // Arrange
        var mockCode = CreateMockResultCode();

        // Act
        var result = GenericResult<string>.Failure(mockCode.Object, _logger);

        // Assert
        result.IsFailure.ShouldBeTrue();
        mockCode.Verify(c => c.Log(_logger, null), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GenericOfTChain_SetsInnerResultAndCode()
    {
        // Arrange
        var innerResult = GenericResult.Failure(new GenericMessage("inner"));
        var outerCode = CreateMockResultCode(isSuccess: false);

        // Act
        var result = GenericResult<int>.Chain(outerCode.Object, innerResult);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.InnerResult.ShouldBe(innerResult);
        result.Code.ShouldBe(outerCode.Object);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GenericOfTChainWithLogger_CallsLog()
    {
        // Arrange
        var innerResult = GenericResult.Failure(new GenericMessage("inner"));
        var outerCode = CreateMockResultCode(isSuccess: false);

        // Act
        var result = GenericResult<int>.Chain(outerCode.Object, innerResult, _logger);

        // Assert
        outerCode.Verify(c => c.Log(_logger, null), Times.Once);
    }

    #endregion
}
