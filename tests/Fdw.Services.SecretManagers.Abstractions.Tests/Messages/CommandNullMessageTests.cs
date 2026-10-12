using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Abstractions.Tests.Messages;

public class CommandNullMessageTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        var message = new CommandNullMessage();

        message.Id.ShouldBe(1001);
        message.Name.ShouldBe("CommandNull");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Command cannot be null.");
        message.Code.ShouldBe("SM_CMD_NULL");
        message.OriginatedIn.ShouldBe("SecretManager");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ImplementsIServiceMessage()
    {
        var message = new CommandNullMessage();

        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void InheritsFromSecretManagerMessage()
    {
        var message = new CommandNullMessage();

        message.ShouldBeAssignableTo<SecretManagerMessage>();
    }
}
