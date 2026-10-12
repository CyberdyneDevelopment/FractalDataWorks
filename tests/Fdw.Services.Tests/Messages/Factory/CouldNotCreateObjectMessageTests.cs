using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Factory;

public class CouldNotCreateObjectMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructorInitializesWithCorrectProperties()
    {
        var message = new CouldNotCreateObjectMessage();

        message.Id.ShouldBe(2004);
        message.Name.ShouldBe("CouldNotCreateObject");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Code.ShouldBe("COULD_NOT_CREATE_OBJECT");
        message.Message.ShouldContain("Could not create object of type {0}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithObjectTypeSetsFormattedMessage()
    {
        var message = new CouldNotCreateObjectMessage("MsSqlConnectionFactory");

        message.Message.ShouldContain("MsSqlConnectionFactory");
        message.Message.ShouldContain("Could not create object of type");
        message.Id.ShouldBe(2004);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithObjectTypeAndReasonSetsFormattedMessage()
    {
        var message = new CouldNotCreateObjectMessage("RestConnectionFactory", "Missing constructor");

        message.Message.ShouldContain("RestConnectionFactory");
        message.Message.ShouldContain("Missing constructor");
        message.Id.ShouldBe(2004);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageImplementsIServiceMessage()
    {
        new CouldNotCreateObjectMessage().ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageInheritsFromFactoryMessage()
    {
        new CouldNotCreateObjectMessage().ShouldBeAssignableTo<FactoryMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageIsSealed()
    {
        typeof(CouldNotCreateObjectMessage).IsSealed.ShouldBeTrue();
    }
}
