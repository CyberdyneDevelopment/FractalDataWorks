using Fdw.Commands.Abstractions;

namespace Fdw.Commands.Abstractions.Tests;

public sealed class CommandExecutionTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorInitializesCommandType()
    {
        // Arrange
        var commandType = Mock.Of<IGenericCommandType>();

        // Act
        var execution = new CommandExecution(commandType);

        // Assert
        execution.CommandType.ShouldBe(commandType);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorInitializesPayload()
    {
        // Arrange
        var commandType = Mock.Of<IGenericCommandType>();
        var payload = new { SqlText = "SELECT * FROM Users" };

        // Act
        var execution = new CommandExecution(commandType, payload);

        // Assert
        execution.Payload.ShouldBe(payload);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithNullPayloadSetsPayloadToNull()
    {
        // Arrange
        var commandType = Mock.Of<IGenericCommandType>();

        // Act
        var execution = new CommandExecution(commandType, null);

        // Assert
        execution.Payload.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorThrowsWhenCommandTypeIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new CommandExecution(null!));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorThrowsWithCorrectParameterName()
    {
        // Act
        var exception = Should.Throw<ArgumentNullException>(() => new CommandExecution(null!));

        // Assert
        exception.ParamName.ShouldBe("commandType");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ExecutionIdIsGeneratedAutomatically()
    {
        // Arrange
        var commandType = Mock.Of<IGenericCommandType>();

        // Act
        var execution = new CommandExecution(commandType);

        // Assert
        execution.ExecutionId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CreatedAtIsSetToApproximatelyNow()
    {
        // Arrange
        var commandType = Mock.Of<IGenericCommandType>();
        var before = DateTime.UtcNow;

        // Act
        var execution = new CommandExecution(commandType);

        // Assert
        var after = DateTime.UtcNow;
        execution.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        execution.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CorrelationIdCanBeSetViaInit()
    {
        // Arrange
        var commandType = Mock.Of<IGenericCommandType>();
        var correlationId = Guid.NewGuid();

        // Act
        var execution = new CommandExecution(commandType) { CorrelationId = correlationId };

        // Assert
        execution.CorrelationId.ShouldBe(correlationId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CorrelationIdIsNullByDefault()
    {
        // Arrange
        var commandType = Mock.Of<IGenericCommandType>();

        // Act
        var execution = new CommandExecution(commandType);

        // Assert
        execution.CorrelationId.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MetadataCanBeSetViaInit()
    {
        // Arrange
        var commandType = Mock.Of<IGenericCommandType>();
        var metadata = new Dictionary<string, object> { { "UserId", 123 }, { "TenantId", "abc" } };

        // Act
        var execution = new CommandExecution(commandType) { Metadata = metadata };

        // Assert
        execution.Metadata.ShouldBe(metadata);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MetadataIsNullByDefault()
    {
        // Arrange
        var commandType = Mock.Of<IGenericCommandType>();

        // Act
        var execution = new CommandExecution(commandType);

        // Assert
        execution.Metadata.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MultipleExecutionsHaveUniqueExecutionIds()
    {
        // Arrange
        var commandType = Mock.Of<IGenericCommandType>();

        // Act
        var execution1 = new CommandExecution(commandType);
        var execution2 = new CommandExecution(commandType);

        // Assert
        execution1.ExecutionId.ShouldNotBe(execution2.ExecutionId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ExecutionIdCanBeOverriddenViaInit()
    {
        // Arrange
        var commandType = Mock.Of<IGenericCommandType>();
        var customId = Guid.NewGuid();

        // Act
        var execution = new CommandExecution(commandType) { ExecutionId = customId };

        // Assert
        execution.ExecutionId.ShouldBe(customId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CreatedAtCanBeOverriddenViaInit()
    {
        // Arrange
        var commandType = Mock.Of<IGenericCommandType>();
        var customTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var execution = new CommandExecution(commandType) { CreatedAt = customTime };

        // Assert
        execution.CreatedAt.ShouldBe(customTime);
    }
}
