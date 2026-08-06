using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Service;

public class NoServiceTypesRegisteredMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructorInitializesWithCorrectProperties()
    {
        var message = new NoServiceTypesRegisteredMessage();

        message.Id.ShouldBe(1005);
        message.Name.ShouldBe("NoServiceTypesRegistered");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Code.ShouldBe("NO_SERVICE_TYPES");
        message.Message.ShouldBe("No service types registered");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageImplementsIServiceMessage()
    {
        new NoServiceTypesRegisteredMessage().ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageInheritsFromServiceMessage()
    {
        new NoServiceTypesRegisteredMessage().ShouldBeAssignableTo<ServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageIsSealed()
    {
        typeof(NoServiceTypesRegisteredMessage).IsSealed.ShouldBeTrue();
    }
}
