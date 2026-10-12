using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Service;

public class ServiceTypeUnknownMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructor_InitializesWithCorrectProperties()
    {
        // Arrange & Act
        var message = new ServiceTypeUnknownMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Id.ShouldBe(1006);
        message.Name.ShouldBe("ServiceTypeUnknown");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Unknown service type: {0}");
        message.Code.ShouldBe("SERVICE_TYPE_UNKNOWN");
        message.OriginatedIn.ShouldBe("Services");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithServiceType_IncludesTypeInMessage()
    {
        // Arrange
        var serviceType = "UnknownService";

        // Act
        var message = new ServiceTypeUnknownMessage(serviceType);

        // Assert
        message.ShouldNotBeNull();
        message.Id.ShouldBe(1006);
        message.Message.ShouldContain(serviceType);
        message.Message.ShouldContain("Unknown service type");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithServiceTypeAndContext_IncludesBothInMessage()
    {
        // Arrange
        var serviceType = "MyService";
        var context = "configuration section 'Services'";

        // Act
        var message = new ServiceTypeUnknownMessage(serviceType, context);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain(serviceType);
        message.Message.ShouldContain(context);
        message.Message.ShouldContain("Unknown service type");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_ImplementsIServiceMessage()
    {
        // Arrange & Act
        var message = new ServiceTypeUnknownMessage();

        // Assert
        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_InheritsFromServiceMessage()
    {
        // Arrange & Act
        var message = new ServiceTypeUnknownMessage();

        // Assert
        message.ShouldBeAssignableTo<ServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_IsSealed()
    {
        // Arrange & Act
        var type = typeof(ServiceTypeUnknownMessage);

        // Assert
        type.IsSealed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_HasMessageAttribute()
    {
        // Arrange & Act
        var type = typeof(ServiceTypeUnknownMessage);
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
        var message = new ServiceTypeUnknownMessage();
        var serviceType = "TestService";

        // Act
        var formatted = message.Format(serviceType);

        // Assert
        formatted.ShouldContain(serviceType);
        formatted.ShouldContain("Unknown service type");
    }
}
