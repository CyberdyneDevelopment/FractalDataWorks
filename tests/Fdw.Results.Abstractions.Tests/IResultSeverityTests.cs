using Fdw.Results.Abstractions;

namespace Fdw.Results.Abstractions.Tests;

/// <summary>
/// Tests for IResultSeverity interface contract.
/// </summary>
public sealed class IResultSeverityTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockedIResultSeverityCanBeCreated()
    {
        // Arrange
        var mock = new Mock<IResultSeverity>();
        mock.Setup(s => s.Name).Returns("Error");
        mock.Setup(s => s.Id).Returns(4);
        mock.Setup(s => s.IsSuccess).Returns(false);
        mock.Setup(s => s.IsFailure).Returns(true);
        mock.Setup(s => s.LogLevelValue).Returns(4);
        mock.Setup(s => s.ShouldLog).Returns(true);
        mock.Setup(s => s.ColorHint).Returns("red");

        // Act
        var severity = mock.Object;

        // Assert
        severity.ShouldNotBeNull();
        severity.Name.ShouldBe("Error");
        severity.Id.ShouldBe(4);
        severity.IsSuccess.ShouldBeFalse();
        severity.IsFailure.ShouldBeTrue();
        severity.LogLevelValue.ShouldBe(4);
        severity.ShouldLog.ShouldBeTrue();
        severity.ColorHint.ShouldBe("red");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultSeverityHasIsSuccessProperty()
    {
        // Assert
        typeof(IResultSeverity).GetProperty(nameof(IResultSeverity.IsSuccess)).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultSeverityHasIsFailureProperty()
    {
        // Assert
        typeof(IResultSeverity).GetProperty(nameof(IResultSeverity.IsFailure)).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultSeverityHasLogLevelValueProperty()
    {
        // Assert
        typeof(IResultSeverity).GetProperty(nameof(IResultSeverity.LogLevelValue)).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultSeverityHasShouldLogProperty()
    {
        // Assert
        typeof(IResultSeverity).GetProperty(nameof(IResultSeverity.ShouldLog)).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultSeverityHasColorHintProperty()
    {
        // Assert
        typeof(IResultSeverity).GetProperty(nameof(IResultSeverity.ColorHint)).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultSeverityInheritsFromITypeOption()
    {
        // Assert
        var interfaces = typeof(IResultSeverity).GetInterfaces();
        interfaces.ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("ITypeOption"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IsSuccessAndIsFailureAreOpposites()
    {
        // Arrange
        var successMock = new Mock<IResultSeverity>();
        successMock.Setup(s => s.IsSuccess).Returns(true);
        successMock.Setup(s => s.IsFailure).Returns(false);

        var failureMock = new Mock<IResultSeverity>();
        failureMock.Setup(s => s.IsSuccess).Returns(false);
        failureMock.Setup(s => s.IsFailure).Returns(true);

        // Assert
        successMock.Object.IsSuccess.ShouldBeTrue();
        successMock.Object.IsFailure.ShouldBeFalse();
        failureMock.Object.IsSuccess.ShouldBeFalse();
        failureMock.Object.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void LogLevelValueReturnsIntegerValue()
    {
        // Arrange
        var mock = new Mock<IResultSeverity>();
        mock.Setup(s => s.LogLevelValue).Returns(4);

        // Act
        var logLevel = mock.Object.LogLevelValue;

        // Assert
        logLevel.ShouldBe(4);
        logLevel.ShouldBeOfType<int>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ShouldLogCanBeTrue()
    {
        // Arrange
        var mock = new Mock<IResultSeverity>();
        mock.Setup(s => s.ShouldLog).Returns(true);

        // Act
        var shouldLog = mock.Object.ShouldLog;

        // Assert
        shouldLog.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ShouldLogCanBeFalse()
    {
        // Arrange
        var mock = new Mock<IResultSeverity>();
        mock.Setup(s => s.ShouldLog).Returns(false);

        // Act
        var shouldLog = mock.Object.ShouldLog;

        // Assert
        shouldLog.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ColorHintReturnsStringValue()
    {
        // Arrange
        var mock = new Mock<IResultSeverity>();
        mock.Setup(s => s.ColorHint).Returns("blue");

        // Act
        var colorHint = mock.Object.ColorHint;

        // Assert
        colorHint.ShouldBe("blue");
        colorHint.ShouldBeOfType<string>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ColorHintCanBeHexValue()
    {
        // Arrange
        var mock = new Mock<IResultSeverity>();
        mock.Setup(s => s.ColorHint).Returns("#FF0000");

        // Act
        var colorHint = mock.Object.ColorHint;

        // Assert
        colorHint.ShouldBe("#FF0000");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ColorHintCanBeCssColorName()
    {
        // Arrange
        var mock = new Mock<IResultSeverity>();
        mock.Setup(s => s.ColorHint).Returns("red");

        // Act
        var colorHint = mock.Object.ColorHint;

        // Assert
        colorHint.ShouldBe("red");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void SuccessSeverityHasExpectedProperties()
    {
        // Arrange
        var mock = new Mock<IResultSeverity>();
        mock.Setup(s => s.Name).Returns("Success");
        mock.Setup(s => s.IsSuccess).Returns(true);
        mock.Setup(s => s.IsFailure).Returns(false);
        mock.Setup(s => s.LogLevelValue).Returns(2); // Information
        mock.Setup(s => s.ShouldLog).Returns(false);
        mock.Setup(s => s.ColorHint).Returns("green");

        // Act
        var severity = mock.Object;

        // Assert
        severity.IsSuccess.ShouldBeTrue();
        severity.IsFailure.ShouldBeFalse();
        severity.ShouldLog.ShouldBeFalse();
        severity.ColorHint.ShouldBe("green");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ErrorSeverityHasExpectedProperties()
    {
        // Arrange
        var mock = new Mock<IResultSeverity>();
        mock.Setup(s => s.Name).Returns("Error");
        mock.Setup(s => s.IsSuccess).Returns(false);
        mock.Setup(s => s.IsFailure).Returns(true);
        mock.Setup(s => s.LogLevelValue).Returns(4); // Error
        mock.Setup(s => s.ShouldLog).Returns(true);
        mock.Setup(s => s.ColorHint).Returns("red");

        // Act
        var severity = mock.Object;

        // Assert
        severity.IsSuccess.ShouldBeFalse();
        severity.IsFailure.ShouldBeTrue();
        severity.ShouldLog.ShouldBeTrue();
        severity.ColorHint.ShouldBe("red");
    }
}
