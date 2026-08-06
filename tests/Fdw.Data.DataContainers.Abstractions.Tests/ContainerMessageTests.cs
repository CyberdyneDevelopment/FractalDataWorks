using Fdw.Data.DataContainers.Abstractions.Messages;
using Fdw.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataContainers.Abstractions.Tests;

public class ContainerMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReadAccessValidationFailedMessageHasCorrectProperties()
    {
        // Act
        var message = new ReadAccessValidationFailedMessage();

        // Assert
        message.Id.ShouldBe(2001);
        message.Name.ShouldBe("ReadAccessValidationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Read access validation failed for container '{0}': {1}");
        message.Code.ShouldBe("CONT_READ_ACCESS_FAILED");
        message.Source.ShouldBe("Container");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReaderCreationFailedMessageHasCorrectProperties()
    {
        // Act
        var message = new ReaderCreationFailedMessage();

        // Assert
        message.Id.ShouldBe(2004);
        message.Name.ShouldBe("ReaderCreationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Failed to create reader for container '{0}': {1}");
        message.Code.ShouldBe("CONT_READER_FAILED");
        message.Source.ShouldBe("Container");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SchemaDiscoveryFailedMessageHasCorrectProperties()
    {
        // Act
        var message = new SchemaDiscoveryFailedMessage();

        // Assert
        message.Id.ShouldBe(2003);
        message.Name.ShouldBe("SchemaDiscoveryFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Schema discovery failed for container '{0}': {1}");
        message.Code.ShouldBe("CONT_SCHEMA_DISC_FAILED");
        message.Source.ShouldBe("Container");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WriteAccessValidationFailedMessageHasCorrectProperties()
    {
        // Act
        var message = new WriteAccessValidationFailedMessage();

        // Assert
        message.Id.ShouldBe(2002);
        message.Name.ShouldBe("WriteAccessValidationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Write access validation failed for container '{0}': {1}");
        message.Code.ShouldBe("CONT_WRITE_ACCESS_FAILED");
        message.Source.ShouldBe("Container");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WriterCreationFailedMessageHasCorrectProperties()
    {
        // Act
        var message = new WriterCreationFailedMessage();

        // Assert
        message.Id.ShouldBe(2005);
        message.Name.ShouldBe("WriterCreationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Failed to create writer for container '{0}': {1}");
        message.Code.ShouldBe("CONT_WRITER_FAILED");
        message.Source.ShouldBe("Container");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllContainerMessagesHaveUniqueIds()
    {
        // Arrange
        var messages = new ContainerMessage[]
        {
            new ReadAccessValidationFailedMessage(),
            new ReaderCreationFailedMessage(),
            new SchemaDiscoveryFailedMessage(),
            new WriteAccessValidationFailedMessage(),
            new WriterCreationFailedMessage()
        };

        // Act
        var ids = messages.Select(m => m.Id).ToList();

        // Assert
        ids.Distinct().Count().ShouldBe(ids.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllContainerMessagesHaveContainerSource()
    {
        // Arrange
        var messages = new ContainerMessage[]
        {
            new ReadAccessValidationFailedMessage(),
            new ReaderCreationFailedMessage(),
            new SchemaDiscoveryFailedMessage(),
            new WriteAccessValidationFailedMessage(),
            new WriterCreationFailedMessage()
        };

        // Act & Assert
        foreach (var message in messages)
        {
            message.Source.ShouldBe("Container");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllContainerMessagesHaveErrorSeverity()
    {
        // Arrange
        var messages = new ContainerMessage[]
        {
            new ReadAccessValidationFailedMessage(),
            new ReaderCreationFailedMessage(),
            new SchemaDiscoveryFailedMessage(),
            new WriteAccessValidationFailedMessage(),
            new WriterCreationFailedMessage()
        };

        // Act & Assert
        foreach (var message in messages)
        {
            message.Severity.ShouldBe(MessageSeverity.Error);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllContainerMessagesHaveNonEmptyCode()
    {
        // Arrange
        var messages = new ContainerMessage[]
        {
            new ReadAccessValidationFailedMessage(),
            new ReaderCreationFailedMessage(),
            new SchemaDiscoveryFailedMessage(),
            new WriteAccessValidationFailedMessage(),
            new WriterCreationFailedMessage()
        };

        // Act & Assert
        foreach (var message in messages)
        {
            message.Code.ShouldNotBeNullOrEmpty();
            message.Code!.ShouldStartWith("CONT_");
        }
    }
}
