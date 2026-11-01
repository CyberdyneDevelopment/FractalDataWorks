using Fdw.Abstractions;
using Moq;
using System;

namespace Fdw.Abstractions.Tests;

/// <summary>
/// Tests for IGenericCommand interface contract.
/// </summary>
public class IGenericCommandTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericCommandInterfaceExists()
    {
        // Assert
        var type = typeof(IGenericCommand);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericCommandHasCommandIdProperty()
    {
        // Assert
        var type = typeof(IGenericCommand);
        var property = type.GetProperty("CommandId");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(Guid));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericCommandHasCreatedAtProperty()
    {
        // Assert
        var type = typeof(IGenericCommand);
        var property = type.GetProperty("CreatedAt");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(DateTime));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericCommandHasCommandTypeProperty()
    {
        // Assert
        var type = typeof(IGenericCommand);
        var property = type.GetProperty("CommandType");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericCommandHasCategoryProperty()
    {
        // Assert
        var type = typeof(IGenericCommand);
        var property = type.GetProperty("Category");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockCommandCanSetCommandId()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockCommand = new Mock<IGenericCommand>();
        mockCommand.Setup(c => c.CommandId).Returns(expectedId);

        // Act
        var commandId = mockCommand.Object.CommandId;

        // Assert
        commandId.ShouldBe(expectedId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockCommandCanSetCreatedAt()
    {
        // Arrange
        var expectedTime = DateTime.UtcNow;
        var mockCommand = new Mock<IGenericCommand>();
        mockCommand.Setup(c => c.CreatedAt).Returns(expectedTime);

        // Act
        var createdAt = mockCommand.Object.CreatedAt;

        // Assert
        createdAt.ShouldBe(expectedTime);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockCommandCanSetCommandType()
    {
        // Arrange
        var mockCommand = new Mock<IGenericCommand>();
        mockCommand.Setup(c => c.CommandType).Returns("TestCommand");

        // Act
        var commandType = mockCommand.Object.CommandType;

        // Assert
        commandType.ShouldBe("TestCommand");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockCommandCanSetCategory()
    {
        // Arrange
        var mockCommand = new Mock<IGenericCommand>();
        mockCommand.Setup(c => c.Category).Returns("Query");

        // Act
        var category = mockCommand.Object.Category;

        // Assert
        category.ShouldBe("Query");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockCommandSupportsQueryCategory()
    {
        // Arrange
        var mockCommand = new Mock<IGenericCommand>();
        mockCommand.Setup(c => c.Category).Returns("Query");

        // Act & Assert
        mockCommand.Object.Category.ShouldBe("Query");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockCommandSupportsMutationCategory()
    {
        // Arrange
        var mockCommand = new Mock<IGenericCommand>();
        mockCommand.Setup(c => c.Category).Returns("Mutation");

        // Act & Assert
        mockCommand.Object.Category.ShouldBe("Mutation");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockCommandSupportsNotificationCategory()
    {
        // Arrange
        var mockCommand = new Mock<IGenericCommand>();
        mockCommand.Setup(c => c.Category).Returns("Notification");

        // Act & Assert
        mockCommand.Object.Category.ShouldBe("Notification");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockCommandCommandIdIsUnique()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var mockCommand1 = new Mock<IGenericCommand>();
        mockCommand1.Setup(c => c.CommandId).Returns(id1);

        var mockCommand2 = new Mock<IGenericCommand>();
        mockCommand2.Setup(c => c.CommandId).Returns(id2);

        // Act & Assert
        mockCommand1.Object.CommandId.ShouldNotBe(mockCommand2.Object.CommandId);
    }
}
