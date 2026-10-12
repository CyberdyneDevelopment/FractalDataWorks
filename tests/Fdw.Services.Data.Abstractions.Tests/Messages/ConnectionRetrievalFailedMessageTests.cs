using Fdw.Messages;
using Fdw.Services.Data.Abstractions.Messages;

namespace Fdw.Services.Data.Abstractions.Tests.Messages;

public class ConnectionRetrievalFailedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        // Arrange & Act
        var result = new ConnectionRetrievalFailedMessage();

        // Assert
        result.Id.ShouldBe(1002);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        // Arrange & Act
        var result = new ConnectionRetrievalFailedMessage();

        // Assert
        result.Name.ShouldBe("ConnectionRetrievalFailed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSeverityToError()
    {
        // Arrange & Act
        var result = new ConnectionRetrievalFailedMessage();

        // Assert
        result.Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsMessageTemplate()
    {
        // Arrange & Act
        var result = new ConnectionRetrievalFailedMessage();

        // Assert
        result.Message.ShouldBe("Failed to retrieve connection '{0}'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsCode()
    {
        // Arrange & Act
        var result = new ConnectionRetrievalFailedMessage();

        // Assert
        result.Code.ShouldBe("DG_CONN_RETRIEVAL_FAILED");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromDataGatewayMessage()
    {
        // Arrange & Act
        var result = new ConnectionRetrievalFailedMessage();

        // Assert
        result.ShouldBeAssignableTo<DataGatewayMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSource()
    {
        // Arrange & Act
        var result = new ConnectionRetrievalFailedMessage();

        // Assert
        result.Source.ShouldBe("DataGateway");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryReturnsMessage()
    {
        // Arrange & Act
        var result = new ConnectionRetrievalFailedMessage();

        // Assert
        result.Category.ShouldBe("Message");
    }
}
