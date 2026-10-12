using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Execution.Abstractions.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests.Messages;

public class ExecutionMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsAllProperties()
    {
        // Arrange & Act
        var message = new TestableExecutionMessage(
            id: 1,
            name: "TestMessage",
            severity: MessageSeverity.Warning,
            messageText: "Test message text",
            code: "TEST_CODE",
            category: "Test");

        // Assert
        message.Id.ShouldBe(1);
        message.Name.ShouldBe("TestMessage");
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Message.ShouldBe("Test message text");
        message.Code.ShouldBe("TEST_CODE");
        message.OriginatedIn.ShouldBe("Execution"); // Base constructor sets source as OriginatedIn
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ImplementsIServiceMessage()
    {
        // Arrange & Act
        var message = new TestableExecutionMessage(1, "Test", MessageSeverity.Information, "Test");

        // Assert
        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    private sealed class TestableExecutionMessage : ExecutionMessage
    {
        public TestableExecutionMessage(int id, string name, MessageSeverity severity, string messageText,
            string? code = null, string? category = null)
            : base(id, name, severity, messageText, code, category)
        {
        }
    }
}
