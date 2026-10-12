using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Abstractions.Tests.Messages;

public class SecretValueRequiredMessageTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        var message = new SecretValueRequiredMessage("SetSecret");

        message.Id.ShouldBe(1003);
        message.Name.ShouldBe("SecretValueRequired");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("SecretValue parameter is required for SetSecret operation.");
        message.Code.ShouldBe("SM_VALUE_REQUIRED");
        message.OriginatedIn.ShouldBe("SecretManager");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorFormatsMessageWithOperation()
    {
        var message = new SecretValueRequiredMessage("UpdateSecret");

        message.Message.ShouldContain("UpdateSecret");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ImplementsIServiceMessage()
    {
        var message = new SecretValueRequiredMessage("SetSecret");

        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void InheritsFromSecretManagerMessage()
    {
        var message = new SecretValueRequiredMessage("SetSecret");

        message.ShouldBeAssignableTo<SecretManagerMessage>();
    }
}
