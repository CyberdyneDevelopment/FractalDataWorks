using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Abstractions.Tests.Messages;

public class SecretKeyRequiredMessageTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        var message = new SecretKeyRequiredMessage("GetSecret");

        message.Id.ShouldBe(1002);
        message.Name.ShouldBe("SecretKeyRequired");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("SecretKey is required for GetSecret operation.");
        message.Code.ShouldBe("SM_KEY_REQUIRED");
        message.OriginatedIn.ShouldBe("SecretManager");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorFormatsMessageWithOperation()
    {
        var message = new SecretKeyRequiredMessage("SetSecret");

        message.Message.ShouldContain("SetSecret");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ImplementsIServiceMessage()
    {
        var message = new SecretKeyRequiredMessage("GetSecret");

        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void InheritsFromSecretManagerMessage()
    {
        var message = new SecretKeyRequiredMessage("GetSecret");

        message.ShouldBeAssignableTo<SecretManagerMessage>();
    }
}
