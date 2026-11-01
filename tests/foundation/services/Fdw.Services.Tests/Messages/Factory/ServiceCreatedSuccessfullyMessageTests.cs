using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Factory;

public class ServiceCreatedSuccessfullyMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructorInitializesWithCorrectProperties()
    {
        var message = new ServiceCreatedSuccessfullyMessage();

        message.Id.ShouldBe(2010);
        message.Name.ShouldBe("ServiceCreatedSuccessfully");
        message.Severity.ShouldBe(MessageSeverity.Information);
        message.Code.ShouldBe("SERVICE_CREATED_SUCCESSFULLY");
        message.Message.ShouldContain("Successfully created service of type {0}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithServiceTypeSetsFormattedMessage()
    {
        var message = new ServiceCreatedSuccessfullyMessage("MsSqlConnection");

        message.Message.ShouldContain("MsSqlConnection");
        message.Message.ShouldContain("Successfully created service of type");
        message.Id.ShouldBe(2010);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageImplementsIServiceMessage()
    {
        new ServiceCreatedSuccessfullyMessage().ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageInheritsFromFactoryMessage()
    {
        new ServiceCreatedSuccessfullyMessage().ShouldBeAssignableTo<FactoryMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageIsSealed()
    {
        typeof(ServiceCreatedSuccessfullyMessage).IsSealed.ShouldBeTrue();
    }
}
