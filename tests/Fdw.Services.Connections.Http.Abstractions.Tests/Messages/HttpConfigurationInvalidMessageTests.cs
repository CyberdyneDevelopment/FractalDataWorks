using Fdw.Messages;
using Fdw.Services.Connections.Http.Abstractions.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Abstractions.Tests.Messages;

public class HttpConfigurationInvalidMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultConstructorSetsProperties()
    {
        var msg = new HttpConfigurationInvalidMessage();

        msg.Id.ShouldBe(4301);
        msg.Name.ShouldBe("HttpConfigurationInvalid");
        msg.Severity.ShouldBe(MessageSeverity.Error);
        msg.Code.ShouldBe("HTTP_CONFIG_INVALID");
        msg.Message.ShouldBe("HTTP configuration is invalid");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FieldNameConstructorIncludesField()
    {
        var msg = new HttpConfigurationInvalidMessage("BaseUrl");

        msg.Message.ShouldContain("BaseUrl");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FieldNameAndErrorConstructorIncludesBoth()
    {
        var msg = new HttpConfigurationInvalidMessage("TimeoutSeconds", "must be positive");

        msg.Message.ShouldContain("TimeoutSeconds");
        msg.Message.ShouldContain("must be positive");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FullContextConstructorIncludesExpectedFormat()
    {
        var msg = new HttpConfigurationInvalidMessage("BaseUrl", "invalid URL", "https://host:port/path");

        msg.Message.ShouldContain("BaseUrl");
        msg.Message.ShouldContain("invalid URL");
        msg.Message.ShouldContain("https://host:port/path");
    }
}
