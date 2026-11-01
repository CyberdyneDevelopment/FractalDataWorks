using System.Diagnostics.CodeAnalysis;
using Fdw.Data.Abstractions.Messages;
using Fdw.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Messages;

public sealed class DataMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        // Arrange & Act
        var message = new TestDataMessage(1, "TestMessage", MessageSeverity.Information, "Test");

        // Assert
        message.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        // Arrange & Act
        var message = new TestDataMessage(1, "TestMessage", MessageSeverity.Information, "Test");

        // Assert
        message.Name.ShouldBe("TestMessage");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSeverity()
    {
        // Arrange & Act
        var message = new TestDataMessage(1, "TestMessage", MessageSeverity.Warning, "Test");

        // Assert
        message.Severity.ShouldBe(MessageSeverity.Warning);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsMessage()
    {
        // Arrange & Act
        var message = new TestDataMessage(1, "TestMessage", MessageSeverity.Information, "Test message content");

        // Assert
        message.Message.ShouldBe("Test message content");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsCode()
    {
        // Arrange & Act
        var message = new TestDataMessage(1, "TestMessage", MessageSeverity.Error, "Test", "ERR001");

        // Assert
        message.Code.ShouldBe("ERR001");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsCodeToNullByDefault()
    {
        // Arrange & Act
        var message = new TestDataMessage(1, "TestMessage", MessageSeverity.Information, "Test");

        // Assert
        message.Code.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SourceIsSetToData()
    {
        // Arrange & Act
        var message = new TestDataMessage(1, "TestMessage", MessageSeverity.Information, "Test");

        // Assert
        message.Source.ShouldBe("Data");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromMessageTemplate()
    {
        // Arrange
        var message = new TestDataMessage(1, "TestMessage", MessageSeverity.Information, "Test");

        // Act & Assert
        message.ShouldBeAssignableTo<MessageTemplate<MessageSeverity>>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIDataMessage()
    {
        // Arrange
        var message = new TestDataMessage(1, "TestMessage", MessageSeverity.Information, "Test");

        // Act & Assert
        message.ShouldBeAssignableTo<IDataMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MultipleMessagesWithDifferentSeverities()
    {
        // Arrange
        var info = new TestDataMessage(1, "Info", MessageSeverity.Information, "Info");
        var warning = new TestDataMessage(2, "Warning", MessageSeverity.Warning, "Warning");
        var error = new TestDataMessage(3, "Error", MessageSeverity.Error, "Error");

        // Act & Assert
        info.Severity.ShouldBe(MessageSeverity.Information);
        warning.Severity.ShouldBe(MessageSeverity.Warning);
        error.Severity.ShouldBe(MessageSeverity.Error);
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestDataMessage : DataMessage
    {
        public TestDataMessage(int id, string name, MessageSeverity severity, string message, string? code = null)
            : base(id, name, severity, message, code)
        {
        }
    }
}
