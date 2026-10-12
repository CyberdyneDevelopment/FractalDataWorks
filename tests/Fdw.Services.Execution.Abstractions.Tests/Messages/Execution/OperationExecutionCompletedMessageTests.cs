using Fdw.Messages;
using Fdw.Services.Execution.Abstractions.Messages.Execution;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests.Messages.Execution;

public class OperationExecutionCompletedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultConstructorCreatesMessage()
    {
        // Arrange & Act
        var message = new OperationExecutionCompletedMessage();

        // Assert
        message.Id.ShouldBe(3003);
        message.Name.ShouldBe("OperationExecutionCompleted");
        message.Severity.ShouldBe(MessageSeverity.Information);
        message.Message.ShouldBe("Operation execution completed successfully");
        message.Code.ShouldBe("EXEC_OPERATION_COMPLETED");
        message.OriginatedIn.ShouldBe("Execution");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithOperationNameIncludesName()
    {
        // Arrange & Act
        var message = new OperationExecutionCompletedMessage("Extract");

        // Assert
        message.Message.ShouldContain("Extract");
        message.Message.ShouldBe("Operation 'Extract' completed successfully");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithOperationNameAndProcessIdIncludesBoth()
    {
        // Arrange & Act
        var message = new OperationExecutionCompletedMessage("Transform", "proc-789");

        // Assert
        message.Message.ShouldContain("Transform");
        message.Message.ShouldContain("proc-789");
        message.Message.ShouldBe("Operation 'Transform' completed successfully for process: proc-789");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithOperationNameProcessIdAndDurationIncludesAll()
    {
        // Arrange & Act
        var message = new OperationExecutionCompletedMessage("Load", "proc-101", 1500);

        // Assert
        message.Message.ShouldContain("Load");
        message.Message.ShouldContain("proc-101");
        message.Message.ShouldContain("1500ms");
        message.Message.ShouldBe("Operation 'Load' completed successfully for process: proc-101 in 1500ms");
    }
}
