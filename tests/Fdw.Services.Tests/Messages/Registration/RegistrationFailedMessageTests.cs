using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Registration;

public class RegistrationFailedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructorInitializesWithCorrectProperties()
    {
        var message = new RegistrationFailedMessage();

        message.Id.ShouldBe(2201);
        message.Name.ShouldBe("RegistrationFailed");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Code.ShouldBe("REG002");
        message.Message.ShouldBe("Service registration failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithServiceTypeAndReasonSetsFormattedMessage()
    {
        var message = new RegistrationFailedMessage("MsSql", "Duplicate type name");

        message.Message.ShouldContain("MsSql");
        message.Message.ShouldContain("Duplicate type name");
        message.Id.ShouldBe(2201);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageImplementsIServiceMessage()
    {
        new RegistrationFailedMessage().ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageInheritsFromRegistrationMessage()
    {
        new RegistrationFailedMessage().ShouldBeAssignableTo<RegistrationMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageIsSealed()
    {
        typeof(RegistrationFailedMessage).IsSealed.ShouldBeTrue();
    }
}
