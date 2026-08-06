using Fdw.Messages;
using Fdw.Services.Connections.Http.Abstractions.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Abstractions.Tests.Messages;

public class HttpServerErrorMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultConstructorSetsProperties()
    {
        var msg = new HttpServerErrorMessage();

        msg.Id.ShouldBe(4201);
        msg.Name.ShouldBe("HttpServerError");
        msg.Severity.ShouldBe(MessageSeverity.Error);
        msg.Code.ShouldBe("HTTP_SERVER_ERROR");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void StatusCodeConstructorIncludesCode()
    {
        var msg = new HttpServerErrorMessage(500);

        msg.Message.ShouldContain("500");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void StatusCodeAndEndpointConstructorIncludesBoth()
    {
        var msg = new HttpServerErrorMessage(503, "https://api.example.com/data");

        msg.Message.ShouldContain("503");
        msg.Message.ShouldContain("https://api.example.com/data");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FullContextConstructorIncludesErrorMessage()
    {
        var msg = new HttpServerErrorMessage(502, "https://api.example.com", "Bad Gateway");

        msg.Message.ShouldContain("502");
        msg.Message.ShouldContain("Bad Gateway");
    }
}
