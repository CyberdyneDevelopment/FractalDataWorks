using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Abstractions.Tests.Messages;

public class ValidationFailedMessageTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        var message = new ValidationFailedMessage("Invalid secret key format");

        message.Id.ShouldBe(1004);
        message.Name.ShouldBe("ValidationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Invalid secret key format");
        message.Code.ShouldBe("SM_VALIDATION_FAILED");
        message.OriginatedIn.ShouldBe("SecretManager");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorUsesProvidedErrorMessage()
    {
        var errorMessage = "Custom validation error";
        var message = new ValidationFailedMessage(errorMessage);

        message.Message.ShouldBe(errorMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ImplementsIServiceMessage()
    {
        var message = new ValidationFailedMessage("Error");

        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void InheritsFromSecretManagerMessage()
    {
        var message = new ValidationFailedMessage("Error");

        message.ShouldBeAssignableTo<SecretManagerMessage>();
    }
}
