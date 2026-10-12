using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Service;

public class ConfigurationNotFoundMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructorInitializesWithCorrectProperties()
    {
        var message = new ConfigurationNotFoundMessage();

        message.Id.ShouldBe(1007);
        message.Name.ShouldBe("ConfigurationNotFound");
        message.Severity.ShouldBe(MessageSeverity.Error);
        message.Code.ShouldBe("CONFIGURATION_NOT_FOUND");
        message.Message.ShouldContain("Configuration not found");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithConfigurationNameSetsFormattedMessage()
    {
        var message = new ConfigurationNotFoundMessage("MyConnectionConfig");

        message.Message.ShouldContain("MyConnectionConfig");
        message.Message.ShouldContain("Configuration not found");
        message.Id.ShouldBe(1007);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithConfigurationNameAndServiceContextSetsFormattedMessage()
    {
        var message = new ConfigurationNotFoundMessage("DbConfig", "MsSqlConnectionFactory");

        message.Message.ShouldContain("DbConfig");
        message.Message.ShouldContain("MsSqlConnectionFactory");
        message.Id.ShouldBe(1007);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageImplementsIServiceMessage()
    {
        new ConfigurationNotFoundMessage().ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageInheritsFromServiceMessage()
    {
        new ConfigurationNotFoundMessage().ShouldBeAssignableTo<ServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageIsSealed()
    {
        typeof(ConfigurationNotFoundMessage).IsSealed.ShouldBeTrue();
    }
}
