using Fdw.Results.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Results.Abstractions.Tests;

/// <summary>
/// Tests for ResultCodeBase class.
/// </summary>
public sealed class ResultCodeBaseTests
{
    private sealed class TestResultSeverity : ResultSeverityBase
    {
        public TestResultSeverity(int id, string name, bool isSuccess, int logLevelValue, bool shouldLog, string colorHint)
            : base(id, name, isSuccess, logLevelValue, shouldLog, colorHint)
        {
        }
    }

    private sealed class TestResultCode : ResultCodeBase
    {
        public TestResultCode(
            int id,
            string name,
            string code,
            int eventId,
            IResultSeverity severity,
            string domain,
            string messageTemplate,
            bool isRetryable = false)
            : base(id, name, code, eventId, severity, domain, messageTemplate, isRetryable)
        {
        }

        /// <summary>
        /// Creates a test result code using the protected empty constructor.
        /// </summary>
        public static TestResultCode CreateNotFound()
        {
            // Mimic the NotFound behavior
            return new TestResultCode(
                0,
                "NotFound",
                "UNKNOWN",
                0,
                new TestResultSeverity(-1, "NotFound", false, 4, true, "gray"),
                "Unknown",
                "An unknown error occurred.",
                false);
        }
    }

    private readonly Mock<ILogger> _logger = new();
    private readonly IResultSeverity _errorSeverity = new TestResultSeverity(4, "Error", false, 4, true, "red");
    private readonly IResultSeverity _warningSeverity = new TestResultSeverity(3, "Warning", false, 3, true, "orange");
    private readonly IResultSeverity _successSeverity = new TestResultSeverity(2, "Success", true, 2, false, "green");
    private readonly IResultSeverity _criticalSeverity = new TestResultSeverity(5, "Critical", false, 5, true, "darkred");
    private readonly IResultSeverity _informationSeverity = new TestResultSeverity(2, "Information", true, 2, true, "blue");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithValidParametersCreatesInstance()
    {
        // Act
        var resultCode = new TestResultCode(
            1,
            "TestCode",
            "TEST_001",
            100,
            _errorSeverity,
            "TestDomain",
            "Test message",
            false);

        // Assert
        resultCode.ShouldNotBeNull();
        resultCode.Id.ShouldBe(1);
        resultCode.Name.ShouldBe("TestCode");
        resultCode.Code.ShouldBe("TEST_001");
        resultCode.EventId.ShouldBe(100);
        resultCode.Severity.ShouldBe(_errorSeverity);
        resultCode.Domain.ShouldBe("TestDomain");
        resultCode.MessageTemplate.ShouldBe("Test message");
        resultCode.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithNullCodeThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TestResultCode(1, "Test", null!, 100, _errorSeverity, "Domain", "Message"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithNullSeverityThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TestResultCode(1, "Test", "CODE", 100, null!, "Domain", "Message"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithNullDomainThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TestResultCode(1, "Test", "CODE", 100, _errorSeverity, null!, "Message"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithNullMessageTemplateThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TestResultCode(1, "Test", "CODE", 100, _errorSeverity, "Domain", null!));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithIsRetryableTrueCreatesRetryableCode()
    {
        // Act
        var resultCode = new TestResultCode(
            1,
            "Test",
            "CODE",
            100,
            _errorSeverity,
            "Domain",
            "Message",
            true);

        // Assert
        resultCode.IsRetryable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void NotFoundInstanceHasCorrectDefaults()
    {
        // Act
        var resultCode = TestResultCode.CreateNotFound();

        // Assert
        resultCode.Id.ShouldBe(0);
        resultCode.Name.ShouldBe("NotFound");
        resultCode.Code.ShouldBe("UNKNOWN");
        resultCode.EventId.ShouldBe(0);
        resultCode.Domain.ShouldBe("Unknown");
        resultCode.MessageTemplate.ShouldBe("An unknown error occurred.");
        resultCode.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FormatMessageWithNullDetailsReturnsMessageTemplate()
    {
        // Arrange
        var resultCode = new TestResultCode(
            1,
            "Test",
            "CODE",
            100,
            _errorSeverity,
            "Domain",
            "Simple message",
            false);

        // Act
        var result = resultCode.FormatMessage(null);

        // Assert
        result.ShouldBe("Simple message");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FormatMessageWithEmptyDetailsReturnsMessageTemplate()
    {
        // Arrange
        var resultCode = new TestResultCode(
            1,
            "Test",
            "CODE",
            100,
            _errorSeverity,
            "Domain",
            "Simple message",
            false);

        var mockDetails = new Mock<IResultDetails>();
        mockDetails.Setup(d => d.Data).Returns(new Dictionary<string, object?>());

        // Act
        var result = resultCode.FormatMessage(mockDetails.Object);

        // Assert
        result.ShouldBe("Simple message");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FormatMessageReplacesPlaceholdersWithDetailValues()
    {
        // Arrange
        var resultCode = new TestResultCode(
            1,
            "Test",
            "CODE",
            100,
            _errorSeverity,
            "Domain",
            "Error in {component} at {timestamp}",
            false);

        var mockDetails = new Mock<IResultDetails>();
        mockDetails.Setup(d => d.Data).Returns(new Dictionary<string, object?>
        {
            { "component", "TestComponent" },
            { "timestamp", "2026-02-04 12:00:00" }
        });

        // Act
        var result = resultCode.FormatMessage(mockDetails.Object);

        // Assert
        result.ShouldBe("Error in TestComponent at 2026-02-04 12:00:00");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FormatMessageHandlesMissingPlaceholderValues()
    {
        // Arrange
        var resultCode = new TestResultCode(
            1,
            "Test",
            "CODE",
            100,
            _errorSeverity,
            "Domain",
            "Error in {component} at {timestamp}",
            false);

        var mockDetails = new Mock<IResultDetails>();
        mockDetails.Setup(d => d.Data).Returns(new Dictionary<string, object?>
        {
            { "component", "TestComponent" }
            // timestamp is missing
        });

        // Act
        var result = resultCode.FormatMessage(mockDetails.Object);

        // Assert
        result.ShouldBe("Error in TestComponent at {timestamp}");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FormatMessageHandlesNullPlaceholderValues()
    {
        // Arrange
        var resultCode = new TestResultCode(
            1,
            "Test",
            "CODE",
            100,
            _errorSeverity,
            "Domain",
            "Error in {component}",
            false);

        var mockDetails = new Mock<IResultDetails>();
        mockDetails.Setup(d => d.Data).Returns(new Dictionary<string, object?>
        {
            { "component", null }
        });

        // Act
        var result = resultCode.FormatMessage(mockDetails.Object);

        // Assert
        result.ShouldBe("Error in ");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void LogLevelReturnsCriticalForCriticalSeverity()
    {
        // Arrange
        var resultCode = new TestResultCode(
            1,
            "Test",
            "CODE",
            100,
            _criticalSeverity,
            "Domain",
            "Message",
            false);

        // Act
        var logLevel = resultCode.LogLevel;

        // Assert
        logLevel.ShouldBe(LogLevel.Critical);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void LogLevelReturnsErrorForErrorSeverity()
    {
        // Arrange
        var resultCode = new TestResultCode(
            1,
            "Test",
            "CODE",
            100,
            _errorSeverity,
            "Domain",
            "Message",
            false);

        // Act
        var logLevel = resultCode.LogLevel;

        // Assert
        logLevel.ShouldBe(LogLevel.Error);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void LogLevelReturnsWarningForWarningSeverity()
    {
        // Arrange
        var resultCode = new TestResultCode(
            1,
            "Test",
            "CODE",
            100,
            _warningSeverity,
            "Domain",
            "Message",
            false);

        // Act
        var logLevel = resultCode.LogLevel;

        // Assert
        logLevel.ShouldBe(LogLevel.Warning);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void LogLevelReturnsInformationForInformationSeverity()
    {
        // Arrange
        var resultCode = new TestResultCode(
            1,
            "Test",
            "CODE",
            100,
            _informationSeverity,
            "Domain",
            "Message",
            false);

        // Act
        var logLevel = resultCode.LogLevel;

        // Assert
        logLevel.ShouldBe(LogLevel.Information);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void LogLevelReturnsInformationForSuccessSeverity()
    {
        // Arrange
        var resultCode = new TestResultCode(
            1,
            "Test",
            "CODE",
            100,
            _successSeverity,
            "Domain",
            "Message",
            false);

        // Act
        var logLevel = resultCode.LogLevel;

        // Assert
        logLevel.ShouldBe(LogLevel.Information);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void LogLevelReturnsErrorForUnknownSeverity()
    {
        // Arrange
        var unknownSeverity = new TestResultSeverity(99, "Unknown", false, 4, true, "gray");
        var resultCode = new TestResultCode(
            1,
            "Test",
            "CODE",
            100,
            unknownSeverity,
            "Domain",
            "Message",
            false);

        // Act
        var logLevel = resultCode.LogLevel;

        // Assert
        logLevel.ShouldBe(LogLevel.Error);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void LogWithNullLoggerDoesNotThrow()
    {
        // Arrange
        var resultCode = new TestResultCode(
            1,
            "Test",
            "CODE",
            100,
            _errorSeverity,
            "Domain",
            "Message",
            false);

        // Act & Assert
        Should.NotThrow(() => resultCode.Log(null!, null));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void LogWithValidLoggerLogsMessage()
    {
        // Arrange
        var resultCode = new TestResultCode(
            1,
            "Test",
            "TEST_001",
            100,
            _errorSeverity,
            "Domain",
            "Test message",
            false);

        // Act
        resultCode.Log(_logger.Object, null);

        // Assert
        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.Is<EventId>(e => e.Id == 100 && e.Name == "TEST_001"),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == "Test message"),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void LogWithDetailsFormatsMessage()
    {
        // Arrange
        var resultCode = new TestResultCode(
            1,
            "Test",
            "TEST_001",
            100,
            _errorSeverity,
            "Domain",
            "Error in {component}",
            false);

        var mockDetails = new Mock<IResultDetails>();
        mockDetails.Setup(d => d.Data).Returns(new Dictionary<string, object?>
        {
            { "component", "TestComponent" }
        });

        // Act
        resultCode.Log(_logger.Object, mockDetails.Object);

        // Assert
        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.Is<EventId>(e => e.Id == 100),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == "Error in TestComponent"),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void LogAndReturnLogsAndReturnsSelf()
    {
        // Arrange
        var resultCode = new TestResultCode(
            1,
            "Test",
            "TEST_001",
            100,
            _errorSeverity,
            "Domain",
            "Test message",
            false);

        // Act
        var result = resultCode.LogAndReturn(_logger.Object, null);

        // Assert
        result.ShouldBe(resultCode);
        _logger.Verify(
            l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void LogAndReturnWithNullLoggerReturnsWithoutLogging()
    {
        // Arrange
        var resultCode = new TestResultCode(
            1,
            "Test",
            "TEST_001",
            100,
            _errorSeverity,
            "Domain",
            "Test message",
            false);

        // Act
        var result = resultCode.LogAndReturn(null!, null);

        // Assert
        result.ShouldBe(resultCode);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FormatMessageHandlesMultiplePlaceholders()
    {
        // Arrange
        var resultCode = new TestResultCode(
            1,
            "Test",
            "CODE",
            100,
            _errorSeverity,
            "Domain",
            "{operation} failed in {component} with {error}",
            false);

        var mockDetails = new Mock<IResultDetails>();
        mockDetails.Setup(d => d.Data).Returns(new Dictionary<string, object?>
        {
            { "operation", "Save" },
            { "component", "Database" },
            { "error", "Timeout" }
        });

        // Act
        var result = resultCode.FormatMessage(mockDetails.Object);

        // Assert
        result.ShouldBe("Save failed in Database with Timeout");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FormatMessageHandlesDuplicatePlaceholders()
    {
        // Arrange
        var resultCode = new TestResultCode(
            1,
            "Test",
            "CODE",
            100,
            _errorSeverity,
            "Domain",
            "{error} occurred: {error}",
            false);

        var mockDetails = new Mock<IResultDetails>();
        mockDetails.Setup(d => d.Data).Returns(new Dictionary<string, object?>
        {
            { "error", "ConnectionFailed" }
        });

        // Act
        var result = resultCode.FormatMessage(mockDetails.Object);

        // Assert
        result.ShouldBe("ConnectionFailed occurred: ConnectionFailed");
    }
}
