using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Registration;

public class ServiceRegisteredMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructor_InitializesWithCorrectProperties()
    {
        // Arrange & Act
        var message = new ServiceRegisteredMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Id.ShouldBe(2200);
        message.Name.ShouldBe("ServiceRegistered");
        message.Severity.ShouldBe(MessageSeverity.Information);
        message.Message.ShouldBe("Service registered successfully");
        message.Code.ShouldBe("REG001");
        message.OriginatedIn.ShouldBe("Services");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithServiceTypeAndLifetime_IncludesBothInMessage()
    {
        // Arrange
        var serviceType = "DatabaseService";
        var lifetime = "Singleton";

        // Act
        var message = new ServiceRegisteredMessage(serviceType, lifetime);

        // Assert
        message.ShouldNotBeNull();
        message.Id.ShouldBe(2200);
        message.Name.ShouldBe("ServiceRegistered");
        message.Severity.ShouldBe(MessageSeverity.Information);
        message.Message.ShouldContain(serviceType);
        message.Message.ShouldContain(lifetime);
        message.Message.ShouldContain("registered");
        message.Code.ShouldBe("REG001");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_ImplementsIServiceMessage()
    {
        // Arrange & Act
        var message = new ServiceRegisteredMessage();

        // Assert
        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_InheritsFromRegistrationMessage()
    {
        // Arrange & Act
        var message = new ServiceRegisteredMessage();

        // Assert
        message.ShouldBeAssignableTo<RegistrationMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_IsSealed()
    {
        // Arrange & Act
        var type = typeof(ServiceRegisteredMessage);

        // Assert
        type.IsSealed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_HasMessageAttribute()
    {
        // Arrange & Act
        var type = typeof(ServiceRegisteredMessage);
        var attributes = type.GetCustomAttributes(typeof(Fdw.Messages.MessageAttribute), false);

        // Assert
        attributes.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_IsInformational()
    {
        // Arrange & Act
        var message = new ServiceRegisteredMessage();

        // Assert
        message.Severity.ShouldBe(MessageSeverity.Information);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithTransientLifetime_IncludesLifetimeInMessage()
    {
        // Arrange & Act
        var message = new ServiceRegisteredMessage("MyService", "Transient");

        // Assert
        message.Message.ShouldContain("Transient");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithScopedLifetime_IncludesLifetimeInMessage()
    {
        // Arrange & Act
        var message = new ServiceRegisteredMessage("MyService", "Scoped");

        // Assert
        message.Message.ShouldContain("Scoped");
    }
}
