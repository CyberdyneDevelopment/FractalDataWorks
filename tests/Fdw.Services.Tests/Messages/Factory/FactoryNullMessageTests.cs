using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Factory;

public class FactoryNullMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructor_InitializesWithCorrectProperties()
    {
        // Arrange & Act
        var message = new FactoryNullMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Id.ShouldBe(3002);
        message.Name.ShouldBe("FactoryNull");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Factory cannot be null");
        message.Code.ShouldBe("FACTORY_NULL");
        message.OriginatedIn.ShouldBe("Services");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_ImplementsIServiceMessage()
    {
        // Arrange & Act
        var message = new FactoryNullMessage();

        // Assert
        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_InheritsFromFactoryMessage()
    {
        // Arrange & Act
        var message = new FactoryNullMessage();

        // Assert
        message.ShouldBeAssignableTo<FactoryMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_IsSealed()
    {
        // Arrange & Act
        var type = typeof(FactoryNullMessage);

        // Assert
        type.IsSealed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_HasMessageAttribute()
    {
        // Arrange & Act
        var type = typeof(FactoryNullMessage);
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
        var message = new FactoryNullMessage();

        // Act
        var result = message.ToString();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("Factory cannot be null");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_Property_ReturnsMessage()
    {
        // Arrange
        var message = new FactoryNullMessage();

        // Act
        var messageText = message.Message;

        // Assert
        messageText.ShouldBe("Factory cannot be null");
    }
}
