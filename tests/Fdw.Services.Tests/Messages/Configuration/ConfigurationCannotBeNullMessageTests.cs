using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Configuration;

public class ConfigurationCannotBeNullMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructor_InitializesWithCorrectProperties()
    {
        // Arrange & Act
        var message = new ConfigurationCannotBeNullMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Id.ShouldBe(1007);
        message.Name.ShouldBe("ConfigurationCannotBeNull");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Configuration cannot be null");
        message.Code.ShouldBe("CONFIG_NULL");
        message.OriginatedIn.ShouldBe("Services");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithParameterName_IncludesParameterInMessage()
    {
        // Arrange
        var parameterName = "MyParameter";

        // Act
        var message = new ConfigurationCannotBeNullMessage(parameterName);

        // Assert
        message.ShouldNotBeNull();
        message.Id.ShouldBe(1007);
        message.Name.ShouldBe("ConfigurationCannotBeNull");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldContain(parameterName);
        message.Message.ShouldContain("cannot be null");
        message.Code.ShouldBe("CONFIG_NULL");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithParameterAndSection_IncludesBothInMessage()
    {
        // Arrange
        var parameterName = "ConnectionString";
        var sectionName = "Database";

        // Act
        var message = new ConfigurationCannotBeNullMessage(parameterName, sectionName);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain(parameterName);
        message.Message.ShouldContain(sectionName);
        message.Message.ShouldContain("cannot be null");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_ImplementsIServiceMessage()
    {
        // Arrange & Act
        var message = new ConfigurationCannotBeNullMessage();

        // Assert
        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_InheritsFromServiceMessage()
    {
        // Arrange & Act
        var message = new ConfigurationCannotBeNullMessage();

        // Assert
        message.ShouldBeAssignableTo<ServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_IsSealed()
    {
        // Arrange & Act
        var type = typeof(ConfigurationCannotBeNullMessage);

        // Assert
        type.IsSealed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_HasMessageAttribute()
    {
        // Arrange & Act
        var type = typeof(ConfigurationCannotBeNullMessage);
        var attributes = type.GetCustomAttributes(typeof(Fdw.Messages.MessageAttribute), false);

        // Assert
        attributes.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToString_ReturnsMessage()
    {
        // Arrange
        var message = new ConfigurationCannotBeNullMessage("TestParam");

        // Act
        var result = message.ToString();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("TestParam");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_Property_ReturnsFormattedMessage()
    {
        // Arrange
        var message = new ConfigurationCannotBeNullMessage("DatabaseConfig");

        // Act
        var messageText = message.Message;

        // Assert
        messageText.ShouldNotBeNullOrEmpty();
        messageText.ShouldContain("DatabaseConfig");
    }
}
