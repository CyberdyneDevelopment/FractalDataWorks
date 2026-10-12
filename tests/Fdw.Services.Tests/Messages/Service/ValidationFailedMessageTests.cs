using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Service;

public class ValidationFailedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructor_InitializesWithCorrectProperties()
    {
        // Arrange & Act
        var message = new ValidationFailedMessage();

        // Assert
        message.ShouldNotBeNull();
        message.Id.ShouldBe(1002);
        message.Name.ShouldBe("ValidationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Validation failed");
        message.Code.ShouldBe("VALIDATION_FAILED");
        message.OriginatedIn.ShouldBe("Services");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithErrors_IncludesErrorsInMessage()
    {
        // Arrange
        var errors = "Field1 is required, Field2 must be positive";

        // Act
        var message = new ValidationFailedMessage(errors);

        // Assert
        message.ShouldNotBeNull();
        message.Id.ShouldBe(1002);
        message.Name.ShouldBe("ValidationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldContain(errors);
        message.Message.ShouldContain("Validation failed");
        message.Code.ShouldBe("VALIDATION_FAILED");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithFieldNameAndError_IncludesBothInMessage()
    {
        // Arrange
        var fieldName = "EmailAddress";
        var error = "must be a valid email format";

        // Act
        var message = new ValidationFailedMessage(fieldName, error);

        // Assert
        message.ShouldNotBeNull();
        message.Message.ShouldContain(fieldName);
        message.Message.ShouldContain(error);
        message.Message.ShouldContain("Validation failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_ImplementsIServiceMessage()
    {
        // Arrange & Act
        var message = new ValidationFailedMessage();

        // Assert
        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_InheritsFromServiceMessage()
    {
        // Arrange & Act
        var message = new ValidationFailedMessage();

        // Assert
        message.ShouldBeAssignableTo<ServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_IsSealed()
    {
        // Arrange & Act
        var type = typeof(ValidationFailedMessage);

        // Assert
        type.IsSealed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Message_HasMessageAttribute()
    {
        // Arrange & Act
        var type = typeof(ValidationFailedMessage);
        var attributes = type.GetCustomAttributes(typeof(Fdw.Messages.MessageAttribute), false);

        // Assert
        attributes.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithMultipleErrors_FormatsProperly()
    {
        // Arrange
        var errors = "Error1, Error2, Error3";

        // Act
        var message = new ValidationFailedMessage(errors);

        // Assert
        message.Message.ShouldContain(errors);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithFieldAndError_FormatsFieldNameProperly()
    {
        // Arrange
        var fieldName = "UserName";
        var error = "is already taken";

        // Act
        var message = new ValidationFailedMessage(fieldName, error);

        // Assert
        message.Message.ShouldContain($"for {fieldName}");
    }
}
