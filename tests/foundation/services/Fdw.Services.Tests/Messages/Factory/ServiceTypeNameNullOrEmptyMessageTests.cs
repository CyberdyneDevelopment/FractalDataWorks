using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Factory;

public class ServiceTypeNameNullOrEmptyMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructorInitializesWithCorrectProperties()
    {
        var message = new ServiceTypeNameNullOrEmptyMessage();

        message.Id.ShouldBe(3001);
        message.Name.ShouldBe("ServiceTypeNameNullOrEmpty");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Code.ShouldBe("FACTORY_TYPE_NAME_NULL");
        message.Message.ShouldBe("Service type name cannot be null or empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageImplementsIServiceMessage()
    {
        new ServiceTypeNameNullOrEmptyMessage().ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageInheritsFromFactoryMessage()
    {
        new ServiceTypeNameNullOrEmptyMessage().ShouldBeAssignableTo<FactoryMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageIsSealed()
    {
        typeof(ServiceTypeNameNullOrEmptyMessage).IsSealed.ShouldBeTrue();
    }
}
