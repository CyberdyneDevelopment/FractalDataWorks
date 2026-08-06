using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Service;

public class InvalidCommandMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructorInitializesWithCorrectProperties()
    {
        var message = new InvalidCommandMessage();

        message.Id.ShouldBe(1001);
        message.Name.ShouldBe("InvalidCommand");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Code.ShouldBe("INVALID_COMMAND");
        message.Message.ShouldContain("Invalid command type");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithCommandTypeSetsFormattedMessage()
    {
        var message = new InvalidCommandMessage("DeleteAll");

        message.Message.ShouldContain("DeleteAll");
        message.Message.ShouldContain("Invalid command type");
        message.Id.ShouldBe(1001);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageImplementsIServiceMessage()
    {
        new InvalidCommandMessage().ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageInheritsFromServiceMessage()
    {
        new InvalidCommandMessage().ShouldBeAssignableTo<ServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageIsSealed()
    {
        typeof(InvalidCommandMessage).IsSealed.ShouldBeTrue();
    }
}
