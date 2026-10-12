using Fdw.Messages;
using Fdw.Services.Execution.Abstractions.Messages.Execution;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests.Messages.Execution;

public class OperationExecutionStartedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultConstructorCreatesMessage()
    {
        // Arrange & Act
        var message = new OperationExecutionStartedMessage();

        // Assert
        message.Id.ShouldBe(3002);
        message.Name.ShouldBe("OperationExecutionStarted");
        message.Severity.ShouldBe(MessageSeverity.Information);
        message.Message.ShouldBe("Operation execution started");
        message.Code.ShouldBe("EXEC_OPERATION_STARTED");
        message.OriginatedIn.ShouldBe("Execution");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithOperationNameIncludesName()
    {
        // Arrange & Act
        var message = new OperationExecutionStartedMessage("Initialize");

        // Assert
        message.Message.ShouldContain("Initialize");
        message.Message.ShouldBe("Operation 'Initialize' execution started");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithOperationNameAndProcessIdIncludesBoth()
    {
        // Arrange & Act
        var message = new OperationExecutionStartedMessage("Process", "proc-303");

        // Assert
        message.Message.ShouldContain("Process");
        message.Message.ShouldContain("proc-303");
        message.Message.ShouldBe("Operation 'Process' execution started for process: proc-303");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithOperationNameProcessIdAndTypeIncludesAll()
    {
        // Arrange & Act
        var message = new OperationExecutionStartedMessage("Cleanup", "proc-404", "Maintenance");

        // Assert
        message.Message.ShouldContain("Cleanup");
        message.Message.ShouldContain("proc-404");
        message.Message.ShouldContain("Maintenance");
        message.Message.ShouldBe("Operation 'Cleanup' execution started for process: proc-404 (type: Maintenance)");
    }
}
