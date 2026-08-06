using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions.Messages;

namespace Fdw.Services.Connections.Abstractions.Tests.Messages;

/// <summary>
/// Tests for all connection messages.
/// </summary>
public class AllMessagesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidationFailedMessageHasCorrectProperties()
    {
        // Arrange
        var errorMessage = "Connection string is invalid";

        // Act
        var message = new ValidationFailedMessage(errorMessage);

        // Assert
        message.Id.ShouldBe(1007);
        message.Name.ShouldBe("ValidationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe(errorMessage);
        message.Code.ShouldBe("CONN_VALIDATION_FAILED");
        message.Category.ShouldBe("Message");
        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void QueryNullMessageHasCorrectProperties()
    {
        // Act
        var message = new QueryNullMessage();

        // Assert
        message.Id.ShouldBe(1004);
        message.Name.ShouldBe("QueryNull");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Query cannot be null");
        message.Code.ShouldBe("CONN_QUERY_NULL");
        message.Category.ShouldBe("Message");
        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DataSetNullMessageHasCorrectProperties()
    {
        // Act
        var message = new DataSetNullMessage();

        // Assert
        message.Id.ShouldBe(1005);
        message.Name.ShouldBe("DataSetNull");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("DataSet cannot be null");
        message.Code.ShouldBe("CONN_DATASET_NULL");
        message.Category.ShouldBe("Message");
        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ConnectionIdNullOrEmptyMessageHasCorrectProperties()
    {
        // Act
        var message = new ConnectionIdNullOrEmptyMessage();

        // Assert
        message.Id.ShouldBe(1003);
        message.Name.ShouldBe("ConnectionIdNullOrEmpty");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Connection ID cannot be null or empty");
        message.Code.ShouldBe("CONN_ID_NULL");
        message.Category.ShouldBe("Message");
        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DataReaderClosedMessageHasCorrectProperties()
    {
        // Act
        var message = new DataReaderClosedMessage();

        // Assert
        message.Id.ShouldBe(1006);
        message.Name.ShouldBe("DataReaderClosed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("SqlDataReader is closed and cannot be read");
        message.Code.ShouldBe("CONN_READER_CLOSED");
        message.Category.ShouldBe("Message");
        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ExecutionFailedMessageHasCorrectProperties()
    {
        // Act
        var message = new ExecutionFailedMessage();

        // Assert
        message.Id.ShouldBe(3012);
        message.Name.ShouldBe("ExecutionFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Command execution failed");
        message.Code.ShouldBe("CONN_EXECUTION_FAILED");
        message.Category.ShouldBe("Message");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TranslationFailedMessageHasCorrectProperties()
    {
        // Act
        var message = new TranslationFailedMessage();

        // Assert
        message.Id.ShouldBe(3010);
        message.Name.ShouldBe("TranslationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Failed to translate command");
        message.Code.ShouldBe("CONN_TRANSLATION_FAILED");
        message.Category.ShouldBe("Message");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TranslatorReturnedWrongTypeMessageHasCorrectProperties()
    {
        // Act
        var message = new TranslatorReturnedWrongTypeMessage();

        // Assert
        message.Id.ShouldBe(3011);
        message.Name.ShouldBe("TranslatorReturnedWrongType");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Translator returned '{0}' but expected '{1}'");
        message.Code.ShouldBe("CONN_WRONG_COMMAND_TYPE");
        message.Category.ShouldBe("Message");
    }
}
