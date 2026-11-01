using Fdw.Messages;
using Fdw.Services.Data.Abstractions.Messages;

namespace Fdw.Services.Data.Abstractions.Tests.Messages;

public class DataGatewayMessageTests
{
    [ExcludeFromCodeCoverage]
    private sealed class TestDataGatewayMessage : DataGatewayMessage
    {
        public TestDataGatewayMessage(
            int id,
            string name,
            MessageSeverity severity,
            string message,
            string? code = null)
            : base(id, name, severity, message, code)
        {
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        // Arrange
        var id = 1001;

        // Act
        var result = new TestDataGatewayMessage(
            id,
            "TestMessage",
            MessageSeverity.Error,
            "Test message");

        // Assert
        result.Id.ShouldBe(id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        // Arrange
        var name = "TestMessage";

        // Act
        var result = new TestDataGatewayMessage(
            1001,
            name,
            MessageSeverity.Error,
            "Test message");

        // Assert
        result.Name.ShouldBe(name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSeverity()
    {
        // Arrange
        var severity = MessageSeverity.Warning;

        // Act
        var result = new TestDataGatewayMessage(
            1001,
            "TestMessage",
            severity,
            "Test message");

        // Assert
        result.Severity.ShouldBe(severity);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsMessage()
    {
        // Arrange
        var message = "Test message with {0} placeholder";

        // Act
        var result = new TestDataGatewayMessage(
            1001,
            "TestMessage",
            MessageSeverity.Error,
            message);

        // Assert
        result.Message.ShouldBe(message);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsCode()
    {
        // Arrange
        var code = "TEST_CODE";

        // Act
        var result = new TestDataGatewayMessage(
            1001,
            "TestMessage",
            MessageSeverity.Error,
            "Test message",
            code);

        // Assert
        result.Code.ShouldBe(code);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsCodeToNullWhenNotProvided()
    {
        // Arrange & Act
        var result = new TestDataGatewayMessage(
            1001,
            "TestMessage",
            MessageSeverity.Error,
            "Test message");

        // Assert
        result.Code.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSourceToDataGateway()
    {
        // Arrange & Act
        var result = new TestDataGatewayMessage(
            1001,
            "TestMessage",
            MessageSeverity.Error,
            "Test message");

        // Assert
        result.Source.ShouldBe("DataGateway");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromMessageTemplate()
    {
        // Arrange & Act
        var result = new TestDataGatewayMessage(
            1001,
            "TestMessage",
            MessageSeverity.Error,
            "Test message");

        // Assert
        result.ShouldBeAssignableTo<MessageTemplate<MessageSeverity>>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryReturnsMessage()
    {
        // Arrange & Act
        var result = new TestDataGatewayMessage(
            1001,
            "TestMessage",
            MessageSeverity.Error,
            "Test message");

        // Assert
        result.Category.ShouldBe("Message");
    }
}
