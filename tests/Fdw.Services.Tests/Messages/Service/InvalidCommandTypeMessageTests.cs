using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Service;

public class InvalidCommandTypeMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructorInitializesWithCorrectProperties()
    {
        var message = new InvalidCommandTypeMessage();

        message.Id.ShouldBe(1001);
        message.Name.ShouldBe("InvalidCommandType");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Code.ShouldBe("INVALID_COMMAND_TYPE");
        message.Message.ShouldBe("Invalid command type provided");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithCommandTypeSetsFormattedMessage()
    {
        var message = new InvalidCommandTypeMessage("BulkDelete");

        message.Message.ShouldContain("BulkDelete");
        message.Message.ShouldContain("Invalid command type");
        message.Id.ShouldBe(1001);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageImplementsIServiceMessage()
    {
        new InvalidCommandTypeMessage().ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageInheritsFromServiceMessage()
    {
        new InvalidCommandTypeMessage().ShouldBeAssignableTo<ServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageIsSealed()
    {
        typeof(InvalidCommandTypeMessage).IsSealed.ShouldBeTrue();
    }
}
