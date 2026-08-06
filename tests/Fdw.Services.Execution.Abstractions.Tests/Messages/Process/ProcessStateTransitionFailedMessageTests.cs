using Fdw.Messages;
using Fdw.Services.Execution.Abstractions.Messages.Process;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests.Messages.Process;

public class ProcessStateTransitionFailedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultConstructorCreatesMessage()
    {
        // Arrange & Act
        var message = new ProcessStateTransitionFailedMessage();

        // Assert
        message.Id.ShouldBe(1004);
        message.Name.ShouldBe("ProcessStateTransitionFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Process state transition failed");
        message.Code.ShouldBe("EXEC_STATE_TRANSITION_FAILED");
        message.OriginatedIn.ShouldBe("Execution");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessIdIncludesId()
    {
        // Arrange & Act
        var message = new ProcessStateTransitionFailedMessage("proc-800");

        // Assert
        message.Message.ShouldContain("proc-800");
        message.Message.ShouldBe("Process state transition failed for process: proc-800");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessIdAndStatesIncludesAll()
    {
        // Arrange & Act
        var message = new ProcessStateTransitionFailedMessage("proc-801", "Running", "Completed");

        // Assert
        message.Message.ShouldContain("proc-801");
        message.Message.ShouldContain("Running");
        message.Message.ShouldContain("Completed");
        message.Message.ShouldBe("Process state transition failed for process: proc-801 from 'Running' to 'Completed'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessIdStatesAndReasonIncludesAll()
    {
        // Arrange & Act
        var message = new ProcessStateTransitionFailedMessage("proc-802", "Pending", "Running", "Resource unavailable");

        // Assert
        message.Message.ShouldContain("proc-802");
        message.Message.ShouldContain("Pending");
        message.Message.ShouldContain("Running");
        message.Message.ShouldContain("Resource unavailable");
        message.Message.ShouldBe("Process state transition failed for process: proc-802 from 'Pending' to 'Running': Resource unavailable");
    }
}
