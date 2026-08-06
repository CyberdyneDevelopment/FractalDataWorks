using Fdw.Messages;
using Fdw.Services.Execution.Abstractions.Messages.Process;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests.Messages.Process;

public class ProcessTimeoutMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultConstructorCreatesMessage()
    {
        // Arrange & Act
        var message = new ProcessTimeoutMessage();

        // Assert
        message.Id.ShouldBe(1002);
        message.Name.ShouldBe("ProcessTimeout");
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Message.ShouldBe("Process operation timed out");
        message.Code.ShouldBe("EXEC_PROCESS_TIMEOUT");
        message.OriginatedIn.ShouldBe("Execution");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessIdIncludesId()
    {
        // Arrange & Act
        var message = new ProcessTimeoutMessage("proc-900");

        // Assert
        message.Message.ShouldContain("proc-900");
        message.Message.ShouldBe("Process operation timed out for process: proc-900");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessIdAndTimeoutIncludesBoth()
    {
        // Arrange & Act
        var message = new ProcessTimeoutMessage("proc-901", 30);

        // Assert
        message.Message.ShouldContain("proc-901");
        message.Message.ShouldContain("30 seconds");
        message.Message.ShouldBe("Process operation timed out for process: proc-901 after 30 seconds");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessIdOperationAndTimeoutIncludesAll()
    {
        // Arrange & Act
        var message = new ProcessTimeoutMessage("proc-902", "Query", 60);

        // Assert
        message.Message.ShouldContain("proc-902");
        message.Message.ShouldContain("Query");
        message.Message.ShouldContain("60 seconds");
        message.Message.ShouldBe("Process operation 'Query' timed out for process: proc-902 after 60 seconds");
    }
}
