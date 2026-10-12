using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Factory;

public class FastGenericCreationFailedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructorInitializesWithCorrectProperties()
    {
        var message = new FastGenericCreationFailedMessage();

        message.Id.ShouldBe(2002);
        message.Name.ShouldBe("FastGenericCreationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Code.ShouldBe("FASTGENERIC_CREATION_FAILED");
        message.Message.ShouldContain("FastGeneric failed to create service of type {0}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithServiceTypeSetsFormattedMessage()
    {
        var message = new FastGenericCreationFailedMessage("MyService");

        message.Message.ShouldContain("MyService");
        message.Message.ShouldContain("FastGeneric failed to create service of type");
        message.Id.ShouldBe(2002);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageImplementsIServiceMessage()
    {
        new FastGenericCreationFailedMessage().ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageInheritsFromFactoryMessage()
    {
        new FastGenericCreationFailedMessage().ShouldBeAssignableTo<FactoryMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageIsSealed()
    {
        typeof(FastGenericCreationFailedMessage).IsSealed.ShouldBeTrue();
    }
}
