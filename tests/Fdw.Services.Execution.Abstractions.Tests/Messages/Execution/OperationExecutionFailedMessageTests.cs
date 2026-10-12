using Fdw.Messages;
using Fdw.Services.Execution.Abstractions.Messages.Execution;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests.Messages.Execution;

public class OperationExecutionFailedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultConstructorCreatesMessage()
    {
        // Arrange & Act
        var message = new OperationExecutionFailedMessage();

        // Assert
        message.Id.ShouldBe(3004);
        message.Name.ShouldBe("OperationExecutionFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Operation execution failed");
        message.Code.ShouldBe("EXEC_OPERATION_FAILED");
        message.OriginatedIn.ShouldBe("Execution");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithOperationNameIncludesName()
    {
        // Arrange & Act
        var message = new OperationExecutionFailedMessage("Validate");

        // Assert
        message.Message.ShouldContain("Validate");
        message.Message.ShouldBe("Operation 'Validate' execution failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithOperationNameAndErrorIncludesBoth()
    {
        // Arrange & Act
        var message = new OperationExecutionFailedMessage("Parse", "Invalid format");

        // Assert
        message.Message.ShouldContain("Parse");
        message.Message.ShouldContain("Invalid format");
        message.Message.ShouldBe("Operation 'Parse' execution failed: Invalid format");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithOperationNameProcessIdAndErrorIncludesAll()
    {
        // Arrange & Act
        var message = new OperationExecutionFailedMessage("Execute", "proc-202", "Timeout exceeded");

        // Assert
        message.Message.ShouldContain("Execute");
        message.Message.ShouldContain("proc-202");
        message.Message.ShouldContain("Timeout exceeded");
        message.Message.ShouldBe("Operation 'Execute' execution failed for process: proc-202: Timeout exceeded");
    }
}
