using Fdw.Messages;
using Fdw.Services.Data.Abstractions.Messages;

namespace Fdw.Services.Data.Abstractions.Tests.Messages;

public class ConnectionNotFoundMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        // Arrange & Act
        var result = new ConnectionNotFoundMessage();

        // Assert
        result.Id.ShouldBe(1001);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        // Arrange & Act
        var result = new ConnectionNotFoundMessage();

        // Assert
        result.Name.ShouldBe("ConnectionNotFound");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSeverityToError()
    {
        // Arrange & Act
        var result = new ConnectionNotFoundMessage();

        // Assert
        result.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsMessageTemplate()
    {
        // Arrange & Act
        var result = new ConnectionNotFoundMessage();

        // Assert
        result.Message.ShouldBe("Connection '{0}' not found");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsCode()
    {
        // Arrange & Act
        var result = new ConnectionNotFoundMessage();

        // Assert
        result.Code.ShouldBe("CONN_NOT_FOUND");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromDataGatewayMessage()
    {
        // Arrange & Act
        var result = new ConnectionNotFoundMessage();

        // Assert
        result.ShouldBeAssignableTo<DataGatewayMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSource()
    {
        // Arrange & Act
        var result = new ConnectionNotFoundMessage();

        // Assert
        result.Source.ShouldBe("DataGateway");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryReturnsMessage()
    {
        // Arrange & Act
        var result = new ConnectionNotFoundMessage();

        // Assert
        result.Category.ShouldBe("Message");
    }
}
