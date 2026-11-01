using Fdw.Results.Abstractions;

namespace Fdw.Results.Abstractions.Tests;

/// <summary>
/// Tests for IResultCode interface contract.
/// </summary>
public class IResultCodeTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockedIResultCodeCanBeCreated()
    {
        // Arrange
        var mockSeverity = new Mock<IResultSeverity>();
        mockSeverity.Setup(s => s.Name).Returns("Error");

        var mock = new Mock<IResultCode>();
        mock.Setup(r => r.Name).Returns("TestCode");
        mock.Setup(r => r.Code).Returns("TEST_001");
        mock.Setup(r => r.EventId).Returns(100);
        mock.Setup(r => r.Severity).Returns(mockSeverity.Object);
        mock.Setup(r => r.MessageTemplate).Returns("Test message");
        mock.Setup(r => r.IsRetryable).Returns(false);

        // Act
        var resultCode = mock.Object;

        // Assert
        resultCode.ShouldNotBeNull();
        resultCode.Name.ShouldBe("TestCode");
        resultCode.Code.ShouldBe("TEST_001");
        resultCode.EventId.ShouldBe(100);
        resultCode.Severity.ShouldNotBeNull();
        resultCode.MessageTemplate.ShouldBe("Test message");
        resultCode.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultCodeHasCodeProperty()
    {
        // Assert - Code is defined directly on IResultCode
        typeof(IResultCode).GetProperty(nameof(IResultCode.Code)).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultCodeHasEventIdProperty()
    {
        // Assert
        typeof(IResultCode).GetProperty(nameof(IResultCode.EventId)).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultCodeHasSeverityProperty()
    {
        // Assert
        typeof(IResultCode).GetProperty(nameof(IResultCode.Severity)).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultCodeHasMessageTemplateProperty()
    {
        // Assert
        typeof(IResultCode).GetProperty(nameof(IResultCode.MessageTemplate)).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultCodeHasIsRetryableProperty()
    {
        // Assert
        typeof(IResultCode).GetProperty(nameof(IResultCode.IsRetryable)).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultCodeHasDomainProperty()
    {
        // Assert
        typeof(IResultCode).GetProperty(nameof(IResultCode.Domain)).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultCodeHasLogLevelProperty()
    {
        // Assert
        typeof(IResultCode).GetProperty(nameof(IResultCode.LogLevel)).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultCodeInheritsFromITypeOption()
    {
        // Assert - IResultCode should implement ITypeOption<int, ResultCodeBase>
        var interfaces = typeof(IResultCode).GetInterfaces();
        interfaces.ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("ITypeOption"));
    }
}
