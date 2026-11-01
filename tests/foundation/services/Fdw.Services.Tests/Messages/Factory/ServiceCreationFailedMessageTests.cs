using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Factory;

public class ServiceCreationFailedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructor_InitializesWithCorrectProperties()
    {
        // Arrange & Act
        var message = new ServiceCreationFailedMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Id.ShouldBe(2001);
        message.Name.ShouldBe("ServiceCreationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Failed to create service of type {0}");
        message.Code.ShouldBe("SERVICE_CREATION_FAILED");
        message.OriginatedIn.ShouldBe("Services");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithServiceType_IncludesTypeInMessage()
    {
        // Arrange
        var serviceType = "TestService";

        // Act
        var message = new ServiceCreationFailedMessage(serviceType);

        // Assert
        message.ShouldNotBeNull();
        message.Id.ShouldBe(2001);
        message.Name.ShouldBe("ServiceCreationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldContain(serviceType);
        message.Message.ShouldContain("Failed to create service");
        message.Code.ShouldBe("SERVICE_CREATION_FAILED");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithServiceTypeAndReason_IncludesBothInMessage()
    {
        // Arrange
        var serviceType = "DatabaseService";
        var reason = "No suitable constructor found";

        // Act
        var message = new ServiceCreationFailedMessage(serviceType, reason);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain(serviceType);
        message.Message.ShouldContain(reason);
        message.Message.ShouldContain("Failed to create service");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_ImplementsIServiceMessage()
    {
        // Arrange & Act
        var message = new ServiceCreationFailedMessage();

        // Assert
        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_InheritsFromFactoryMessage()
    {
        // Arrange & Act
        var message = new ServiceCreationFailedMessage();

        // Assert
        message.ShouldBeAssignableTo<FactoryMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_IsSealed()
    {
        // Arrange & Act
        var type = typeof(ServiceCreationFailedMessage);

        // Assert
        type.IsSealed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_HasMessageAttribute()
    {
        // Arrange & Act
        var type = typeof(ServiceCreationFailedMessage);
        var attributes = type.GetCustomAttributes(typeof(Fdw.Messages.MessageAttribute), false);

        // Assert
        attributes.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Format_WithServiceType_ReturnsFormattedMessage()
    {
        // Arrange
        var message = new ServiceCreationFailedMessage();
        var serviceType = "MyService";

        // Act
        var formatted = message.Format(serviceType);

        // Assert
        formatted.ShouldContain(serviceType);
        formatted.ShouldContain("Failed to create service");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Format_WithNoArgs_ReturnsOriginalMessage()
    {
        // Arrange
        var message = new ServiceCreationFailedMessage("TestService");

        // Act
        var formatted = message.Format();

        // Assert
        formatted.ShouldBe(message.Message);
    }
}
