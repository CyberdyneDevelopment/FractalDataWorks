using Fdw.Messages;
using Fdw.Services.Execution.Abstractions.Messages.Process;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests.Messages.Process;

public class ProcessCancellationRequestedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultConstructorCreatesMessage()
    {
        // Arrange & Act
        var message = new ProcessCancellationRequestedMessage();

        // Assert
        message.Id.ShouldBe(1003);
        message.Name.ShouldBe("ProcessCancellationRequested");
        message.Severity.ShouldBe(MessageSeverity.Information);
        message.Message.ShouldBe("Process cancellation requested");
        message.Code.ShouldBe("EXEC_PROCESS_CANCELLATION_REQUESTED");
        message.OriginatedIn.ShouldBe("Execution");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessIdIncludesId()
    {
        // Arrange & Act
        var message = new ProcessCancellationRequestedMessage("proc-600");

        // Assert
        message.Message.ShouldContain("proc-600");
        message.Message.ShouldBe("Process cancellation requested for process: proc-600");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessIdAndReasonIncludesBoth()
    {
        // Arrange & Act
        var message = new ProcessCancellationRequestedMessage("proc-601", "User requested");

        // Assert
        message.Message.ShouldContain("proc-601");
        message.Message.ShouldContain("User requested");
        message.Message.ShouldBe("Process cancellation requested for process: proc-601, reason: User requested");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessIdOperationAndReasonIncludesAll()
    {
        // Arrange & Act
        var message = new ProcessCancellationRequestedMessage("proc-602", "Import", "Timeout imminent");

        // Assert
        message.Message.ShouldContain("proc-602");
        message.Message.ShouldContain("Import");
        message.Message.ShouldContain("Timeout imminent");
        message.Message.ShouldBe("Process cancellation requested for process: proc-602, operation: Import, reason: Timeout imminent");
    }
}
