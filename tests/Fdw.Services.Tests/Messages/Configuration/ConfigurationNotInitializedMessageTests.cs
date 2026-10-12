using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Configuration;

public class ConfigurationNotInitializedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructorInitializesWithCorrectProperties()
    {
        var message = new ConfigurationNotInitializedMessage();

        message.Id.ShouldBe(1006);
        message.Name.ShouldBe("ConfigurationNotInitialized");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Message.ShouldBe("Configuration registry not initialized");
        message.Code.ShouldBe("CONFIG_NOT_INITIALIZED");
        message.OriginatedIn.ShouldBe("Services");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithCustomMessageSetsMessage()
    {
        var customMessage = "Custom init failure description";

        var message = new ConfigurationNotInitializedMessage(customMessage);

        message.Id.ShouldBe(1006);
        message.Name.ShouldBe("ConfigurationNotInitialized");
        message.Message.ShouldBe(customMessage);
        message.Code.ShouldBe("CONFIG_NOT_INITIALIZED");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageImplementsIServiceMessage()
    {
        var message = new ConfigurationNotInitializedMessage();

        message.ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageInheritsFromServiceMessage()
    {
        var message = new ConfigurationNotInitializedMessage();

        message.ShouldBeAssignableTo<ServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageIsSealed()
    {
        typeof(ConfigurationNotInitializedMessage).IsSealed.ShouldBeTrue();
    }
}
