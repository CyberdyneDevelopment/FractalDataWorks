using Fdw.Messages;
using Fdw.Services.Execution.Abstractions.Messages.Configuration;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests.Messages.Configuration;

public class ProcessConfigurationMissingMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultConstructorCreatesMessage()
    {
        // Arrange & Act
        var message = new ProcessConfigurationMissingMessage();

        // Assert
        message.Id.ShouldBe(2002);
        message.Name.ShouldBe("ProcessConfigurationMissing");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Required process configuration is missing");
        message.Code.ShouldBe("EXEC_CONFIG_MISSING");
        message.OriginatedIn.ShouldBe("Execution");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessTypeIncludesType()
    {
        // Arrange & Act
        var message = new ProcessConfigurationMissingMessage("DataSync");

        // Assert
        message.Message.ShouldContain("DataSync");
        message.Message.ShouldBe("Required configuration is missing for process type: DataSync");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessTypeAndDetailsIncludesBoth()
    {
        // Arrange & Act
        var message = new ProcessConfigurationMissingMessage("DataSync", "ConnectionString");

        // Assert
        message.Message.ShouldContain("DataSync");
        message.Message.ShouldContain("ConnectionString");
        message.Message.ShouldBe("Required configuration is missing for process type: DataSync, missing: ConnectionString");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorWithProcessIdTypeAndDetailsIncludesAll()
    {
        // Arrange & Act
        var message = new ProcessConfigurationMissingMessage("proc-456", "DataSync", "TargetDatabase");

        // Assert
        message.Message.ShouldContain("proc-456");
        message.Message.ShouldContain("DataSync");
        message.Message.ShouldContain("TargetDatabase");
        message.Message.ShouldBe("Required configuration is missing for process: proc-456 (type: DataSync), missing: TargetDatabase");
    }
}
