using Fdw.Messages;
using Fdw.Services.Execution.Abstractions.Messages.Configuration;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests.Messages.Configuration;

public class ProcessConfigurationInvalidMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultConstructorCreatesMessage()
    {
        // Arrange & Act
        var message = new ProcessConfigurationInvalidMessage();

        // Assert
        message.Id.ShouldBe(2001);
        message.Name.ShouldBe("ProcessConfigurationInvalid");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Process configuration is invalid");
        message.Code.ShouldBe("EXEC_CONFIG_INVALID");
        message.OriginatedIn.ShouldBe("Execution");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessTypeIncludesType()
    {
        // Arrange & Act
        var message = new ProcessConfigurationInvalidMessage("ETL");

        // Assert
        message.Message.ShouldContain("ETL");
        message.Message.ShouldBe("Configuration is invalid for process type: ETL");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessTypeAndDetailsIncludesBoth()
    {
        // Arrange & Act
        var message = new ProcessConfigurationInvalidMessage("ETL", "Missing required field");

        // Assert
        message.Message.ShouldContain("ETL");
        message.Message.ShouldContain("Missing required field");
        message.Message.ShouldBe("Configuration is invalid for process type: ETL, details: Missing required field");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessIdTypeAndDetailsIncludesAll()
    {
        // Arrange & Act
        var message = new ProcessConfigurationInvalidMessage("proc-123", "ETL", "Invalid timeout");

        // Assert
        message.Message.ShouldContain("proc-123");
        message.Message.ShouldContain("ETL");
        message.Message.ShouldContain("Invalid timeout");
        message.Message.ShouldBe("Configuration is invalid for process: proc-123 (type: ETL), details: Invalid timeout");
    }
}
