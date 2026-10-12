using Fdw.Commands.Abstractions.Messages;
using Fdw.Messages;

namespace Fdw.Commands.Abstractions.Tests.Messages;

public sealed class TranslatorNotFoundMessageTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange
        var sourceFormat = "SQL";
        var targetFormat = "REST";

        // Act
        var message = new TranslatorNotFoundMessage(sourceFormat, targetFormat);

        // Assert
        message.Id.ShouldBe(1004);
        message.Name.ShouldBe("TranslatorNotFound");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("No translator found for converting 'SQL' to 'REST'");
        message.Code.ShouldBe("CMD_TRANS_404");
        message.SourceFormat.ShouldBe(sourceFormat);
        message.TargetFormat.ShouldBe(targetFormat);
        message.Category.ShouldBe("Message"); // Category comes from MessageTemplate base
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorHandlesEmptyFormats()
    {
        // Arrange
        var sourceFormat = string.Empty;
        var targetFormat = string.Empty;

        // Act
        var message = new TranslatorNotFoundMessage(sourceFormat, targetFormat);

        // Assert
        message.SourceFormat.ShouldBe(string.Empty);
        message.TargetFormat.ShouldBe(string.Empty);
        message.Message.ShouldBe("No translator found for converting '' to ''");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorIncludesFormatsInMessage()
    {
        // Arrange
        var sourceFormat = "GraphQL";
        var targetFormat = "OData";

        // Act
        var message = new TranslatorNotFoundMessage(sourceFormat, targetFormat);

        // Assert
        message.Message.ShouldContain(sourceFormat);
        message.Message.ShouldContain(targetFormat);
        message.Message.ShouldBe($"No translator found for converting '{sourceFormat}' to '{targetFormat}'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void DifferentFormatsCreateDifferentMessages()
    {
        // Act
        var message1 = new TranslatorNotFoundMessage("SQL", "REST");
        var message2 = new TranslatorNotFoundMessage("GraphQL", "OData");

        // Assert
        message1.SourceFormat.ShouldBe("SQL");
        message1.TargetFormat.ShouldBe("REST");
        message2.SourceFormat.ShouldBe("GraphQL");
        message2.TargetFormat.ShouldBe("OData");
        message1.Message.ShouldNotBe(message2.Message);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void InheritsFromCommandMessage()
    {
        // Act
        var message = new TranslatorNotFoundMessage("SQL", "REST");

        // Assert
        message.ShouldBeAssignableTo<CommandMessage>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ImplementsIGenericMessage()
    {
        // Act
        var message = new TranslatorNotFoundMessage("SQL", "REST");

        // Assert
        message.ShouldBeAssignableTo<IGenericMessage>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void SourceFormatPropertyIsAccessible()
    {
        // Arrange
        var sourceFormat = "Custom";
        var targetFormat = "Target";

        // Act
        var message = new TranslatorNotFoundMessage(sourceFormat, targetFormat);

        // Assert
        message.SourceFormat.ShouldBe(sourceFormat);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TargetFormatPropertyIsAccessible()
    {
        // Arrange
        var sourceFormat = "Source";
        var targetFormat = "Custom";

        // Act
        var message = new TranslatorNotFoundMessage(sourceFormat, targetFormat);

        // Assert
        message.TargetFormat.ShouldBe(targetFormat);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void SameSourceDifferentTargetCreatesDistinctMessages()
    {
        // Act
        var message1 = new TranslatorNotFoundMessage("SQL", "REST");
        var message2 = new TranslatorNotFoundMessage("SQL", "GraphQL");

        // Assert
        message1.SourceFormat.ShouldBe(message2.SourceFormat);
        message1.TargetFormat.ShouldNotBe(message2.TargetFormat);
    }
}
