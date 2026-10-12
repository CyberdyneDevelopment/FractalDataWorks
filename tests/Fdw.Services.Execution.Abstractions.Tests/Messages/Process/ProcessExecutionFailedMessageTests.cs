using Fdw.Messages;
using Fdw.Services.Execution.Abstractions.Messages.Process;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests.Messages.Process;

public class ProcessExecutionFailedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultConstructorCreatesMessage()
    {
        // Arrange & Act
        var message = new ProcessExecutionFailedMessage();

        // Assert
        message.Id.ShouldBe(1001);
        message.Name.ShouldBe("ProcessExecutionFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Process execution failed");
        message.Code.ShouldBe("EXEC_PROCESS_FAILED");
        message.OriginatedIn.ShouldBe("Execution");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessIdIncludesId()
    {
        // Arrange & Act
        var message = new ProcessExecutionFailedMessage("proc-700");

        // Assert
        message.Message.ShouldContain("proc-700");
        message.Message.ShouldBe("Process execution failed for process: proc-700");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessIdAndErrorIncludesBoth()
    {
        // Arrange & Act
        var message = new ProcessExecutionFailedMessage("proc-701", "Connection lost");

        // Assert
        message.Message.ShouldContain("proc-701");
        message.Message.ShouldContain("Connection lost");
        message.Message.ShouldBe("Process execution failed for process: proc-701 with error: Connection lost");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessIdOperationAndErrorIncludesAll()
    {
        // Arrange & Act
        var message = new ProcessExecutionFailedMessage("proc-702", "Export", "Disk full");

        // Assert
        message.Message.ShouldContain("proc-702");
        message.Message.ShouldContain("Export");
        message.Message.ShouldContain("Disk full");
        message.Message.ShouldBe("Process execution failed for process: proc-702, operation: Export with error: Disk full");
    }
}
