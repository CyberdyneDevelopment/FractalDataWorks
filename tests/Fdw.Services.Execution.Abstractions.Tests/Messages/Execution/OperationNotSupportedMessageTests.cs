using Fdw.Messages;
using Fdw.Services.Execution.Abstractions.Messages.Execution;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests.Messages.Execution;

public class OperationNotSupportedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultConstructorCreatesMessage()
    {
        // Arrange & Act
        var message = new OperationNotSupportedMessage();

        // Assert
        message.Id.ShouldBe(3001);
        message.Name.ShouldBe("OperationNotSupported");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Operation is not supported");
        message.Code.ShouldBe("EXEC_OPERATION_NOT_SUPPORTED");
        message.OriginatedIn.ShouldBe("Execution");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithOperationNameIncludesName()
    {
        // Arrange & Act
        var message = new OperationNotSupportedMessage("CustomOp");

        // Assert
        message.Message.ShouldContain("CustomOp");
        message.Message.ShouldBe("Operation 'CustomOp' is not supported");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithOperationNameAndProcessTypeIncludesBoth()
    {
        // Arrange & Act
        var message = new OperationNotSupportedMessage("Rollback", "SimpleJob");

        // Assert
        message.Message.ShouldContain("Rollback");
        message.Message.ShouldContain("SimpleJob");
        message.Message.ShouldBe("Operation 'Rollback' is not supported by process type: SimpleJob");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithOperationNameProcessIdAndTypeIncludesAll()
    {
        // Arrange & Act
        var message = new OperationNotSupportedMessage("Resume", "proc-505", "BatchJob");

        // Assert
        message.Message.ShouldContain("Resume");
        message.Message.ShouldContain("proc-505");
        message.Message.ShouldContain("BatchJob");
        message.Message.ShouldBe("Operation 'Resume' is not supported by process: proc-505 (type: BatchJob)");
    }
}
