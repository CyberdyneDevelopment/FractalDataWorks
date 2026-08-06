using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Abstractions.Tests.Messages;

/// <summary>
/// Tests for data command message classes.
/// </summary>
public sealed class MessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CommandRequiredMessageHasCorrectProperties()
    {
        // Act
        var message = new CommandRequiredMessage();

        // Assert
        message.Id.ShouldBe(1);
        message.Name.ShouldBe("CommandRequired");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Command is required");
        message.Code.ShouldBe("DATACMD_001");
        message.Category.ShouldBe("Message");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ContainerNameRequiredMessageHasCorrectProperties()
    {
        // Act
        var message = new ContainerNameRequiredMessage();

        // Assert
        message.Id.ShouldBe(2);
        message.Name.ShouldBe("ContainerNameRequired");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Container name is required for data command");
        message.Code.ShouldBe("DATACMD_002");
        message.Category.ShouldBe("Message");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TranslationFailedMessageDefaultConstructorHasCorrectProperties()
    {
        // Act
        var message = new TranslationFailedMessage();

        // Assert
        message.Id.ShouldBe(100);
        message.Name.ShouldBe("TranslationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Failed to translate command");
        message.Code.ShouldBe("DATACMD_100");
        message.Category.ShouldBe("Message");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TranslationFailedMessageWithCommandTypeHasCorrectMessage()
    {
        // Act
        var message = new TranslationFailedMessage("Query");

        // Assert
        message.Id.ShouldBe(100);
        message.Name.ShouldBe("TranslationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Failed to translate Query command");
        message.Code.ShouldBe("DATACMD_100");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TranslationFailedMessageWithCommandTypeAndErrorHasCorrectMessage()
    {
        // Act
        var message = new TranslationFailedMessage("Query", "Invalid filter expression");

        // Assert
        message.Id.ShouldBe(100);
        message.Name.ShouldBe("TranslationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Failed to translate Query command: Invalid filter expression");
        message.Code.ShouldBe("DATACMD_100");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TranslatorNotFoundMessageDefaultConstructorHasCorrectProperties()
    {
        // Act
        var message = new TranslatorNotFoundMessage();

        // Assert
        message.Id.ShouldBe(101);
        message.Name.ShouldBe("TranslatorNotFound");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Translator not found");
        message.Code.ShouldBe("DATACMD_101");
        message.Category.ShouldBe("Message");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TranslatorNotFoundMessageWithTranslatorNameHasCorrectMessage()
    {
        // Act
        var message = new TranslatorNotFoundMessage("TSql");

        // Assert
        message.Id.ShouldBe(101);
        message.Name.ShouldBe("TranslatorNotFound");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Translator 'TSql' not found");
        message.Code.ShouldBe("DATACMD_101");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllMessagesInheritFromDataCommandMessage()
    {
        // Act & Assert
        new CommandRequiredMessage().ShouldBeAssignableTo<DataCommandMessage>();
        new ContainerNameRequiredMessage().ShouldBeAssignableTo<DataCommandMessage>();
        new TranslationFailedMessage().ShouldBeAssignableTo<DataCommandMessage>();
        new TranslatorNotFoundMessage().ShouldBeAssignableTo<DataCommandMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllMessagesHaveErrorSeverity()
    {
        // Act & Assert - All data command messages are errors
        new CommandRequiredMessage().Severity.ShouldBe(MessageSeverity.Error);
        new ContainerNameRequiredMessage().Severity.ShouldBe(MessageSeverity.Error);
        new TranslationFailedMessage().Severity.ShouldBe(MessageSeverity.Error);
        new TranslatorNotFoundMessage().Severity.ShouldBe(MessageSeverity.Error);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllMessagesHaveUniqueIds()
    {
        // Arrange
        var messages = new DataCommandMessage[]
        {
            new CommandRequiredMessage(),
            new ContainerNameRequiredMessage(),
            new TranslationFailedMessage(),
            new TranslatorNotFoundMessage()
        };

        // Act
        var ids = messages.Select(m => m.Id).ToList();

        // Assert
        ids.Distinct().Count().ShouldBe(ids.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllMessagesHaveUniqueCodes()
    {
        // Arrange
        var messages = new DataCommandMessage[]
        {
            new CommandRequiredMessage(),
            new ContainerNameRequiredMessage(),
            new TranslationFailedMessage(),
            new TranslatorNotFoundMessage()
        };

        // Act
        var codes = messages.Select(m => m.Code).Where(c => c != null).ToList();

        // Assert
        codes.Distinct().Count().ShouldBe(codes.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllMessagesHaveMessageCategory()
    {
        // Act & Assert - Category is inherited from MessageTemplate base class
        new CommandRequiredMessage().Category.ShouldBe("Message");
        new ContainerNameRequiredMessage().Category.ShouldBe("Message");
        new TranslationFailedMessage().Category.ShouldBe("Message");
        new TranslatorNotFoundMessage().Category.ShouldBe("Message");
    }
}
