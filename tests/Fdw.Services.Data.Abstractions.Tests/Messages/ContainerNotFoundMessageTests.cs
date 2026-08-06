using Fdw.Messages;
using Fdw.Services.Data.Abstractions.Messages;

namespace Fdw.Services.Data.Abstractions.Tests.Messages;

public class ContainerNotFoundMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        // Arrange & Act
        var result = new ContainerNotFoundMessage();

        // Assert
        result.Id.ShouldBe(1002);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        // Arrange & Act
        var result = new ContainerNotFoundMessage();

        // Assert
        result.Name.ShouldBe("ContainerNotFound");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSeverityToError()
    {
        // Arrange & Act
        var result = new ContainerNotFoundMessage();

        // Assert
        result.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsMessageTemplate()
    {
        // Arrange & Act
        var result = new ContainerNotFoundMessage();

        // Assert
        result.Message.ShouldBe("Container '{0}' not found in configuration");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsCode()
    {
        // Arrange & Act
        var result = new ContainerNotFoundMessage();

        // Assert
        result.Code.ShouldBe("DG_CONTAINER_NOT_FOUND");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromDataGatewayMessage()
    {
        // Arrange & Act
        var result = new ContainerNotFoundMessage();

        // Assert
        result.ShouldBeAssignableTo<DataGatewayMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSource()
    {
        // Arrange & Act
        var result = new ContainerNotFoundMessage();

        // Assert
        result.Source.ShouldBe("DataGateway");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryReturnsMessage()
    {
        // Arrange & Act
        var result = new ContainerNotFoundMessage();

        // Assert
        result.Category.ShouldBe("Message");
    }
}
