using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Factory;

public class ServiceTypeCastFailedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructor_InitializesWithCorrectProperties()
    {
        // Arrange & Act
        var message = new ServiceTypeCastFailedMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Id.ShouldBe(2003);
        message.Name.ShouldBe("ServiceTypeCastFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Service type cast failed from {0} to {1}");
        message.Code.ShouldBe("SERVICE_TYPE_CAST_FAILED");
        message.OriginatedIn.ShouldBe("Services");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithSourceAndTargetTypes_IncludesBothInMessage()
    {
        // Arrange
        var sourceType = "DatabaseService";
        var targetType = "IApiService";

        // Act
        var message = new ServiceTypeCastFailedMessage(sourceType, targetType);

        // Assert
        message.ShouldNotBeNull();
        message.Id.ShouldBe(2003);
        message.Name.ShouldBe("ServiceTypeCastFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldContain(sourceType);
        message.Message.ShouldContain(targetType);
        message.Message.ShouldContain("cast failed");
        message.Code.ShouldBe("SERVICE_TYPE_CAST_FAILED");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_ImplementsIServiceMessage()
    {
        // Arrange & Act
        var message = new ServiceTypeCastFailedMessage();

        // Assert
        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_InheritsFromFactoryMessage()
    {
        // Arrange & Act
        var message = new ServiceTypeCastFailedMessage();

        // Assert
        message.ShouldBeAssignableTo<FactoryMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_IsSealed()
    {
        // Arrange & Act
        var type = typeof(ServiceTypeCastFailedMessage);

        // Assert
        type.IsSealed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_HasMessageAttribute()
    {
        // Arrange & Act
        var type = typeof(ServiceTypeCastFailedMessage);
        var attributes = type.GetCustomAttributes(typeof(Fdw.Messages.MessageAttribute), false);

        // Assert
        attributes.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Format_WithBothTypes_ReturnsFormattedMessage()
    {
        // Arrange
        var message = new ServiceTypeCastFailedMessage();
        var sourceType = "Service1";
        var targetType = "Service2";

        // Act
        var formatted = message.Format(sourceType, targetType);

        // Assert
        formatted.ShouldContain(sourceType);
        formatted.ShouldContain(targetType);
        formatted.ShouldContain("cast failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithEmptyTypes_StillCreatesValidMessage()
    {
        // Arrange & Act
        var message = new ServiceTypeCastFailedMessage("", "");

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain("cast failed");
    }
}
