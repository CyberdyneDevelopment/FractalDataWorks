using Fdw.Data.DataContainers.Abstractions.Messages;
using Fdw.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataContainers.Abstractions.Tests;

public class ContainerMessageBaseTests
{
    // Create a test implementation to verify the base class
    private sealed class TestContainerMessage : ContainerMessage
    {
        public TestContainerMessage(int id, string name, MessageSeverity severity, string message, string? code = null)
            : base(id, name, severity, message, code)
        {
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Act
        var message = new TestContainerMessage(100, "TestMessage", MessageSeverity.Warning, "Test message text", "TEST_CODE");

        // Assert
        message.Id.ShouldBe(100);
        message.Name.ShouldBe("TestMessage");
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Message.ShouldBe("Test message text");
        message.Code.ShouldBe("TEST_CODE");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithNullCodeWorks()
    {
        // Act
        var message = new TestContainerMessage(200, "TestMessage2", MessageSeverity.Error, "Test message 2");

        // Assert
        message.Id.ShouldBe(200);
        message.Name.ShouldBe("TestMessage2");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Test message 2");
        message.Code.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithAllSeverityLevels()
    {
        // Arrange & Act
        var info = new TestContainerMessage(1, "Info", MessageSeverity.Information, "Info message");
        var warning = new TestContainerMessage(2, "Warning", MessageSeverity.Warning, "Warning message");
        var error = new TestContainerMessage(3, "Error", MessageSeverity.Error, "Error message");

        // Assert
        info.Severity.ShouldBe(MessageSeverity.Information);
        warning.Severity.ShouldBe(MessageSeverity.Warning);
        error.Severity.ShouldBe(MessageSeverity.Error);
    }
}
