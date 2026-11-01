using Fdw.Messages;
using Fdw.Services.Connections.Http.Abstractions.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Abstractions.Tests.Messages;

public class HttpAuthenticationFailedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultConstructorSetsProperties()
    {
        var msg = new HttpAuthenticationFailedMessage();

        msg.Id.ShouldBe(4001);
        msg.Name.ShouldBe("HttpAuthenticationFailed");
        msg.Severity.ShouldBe(MessageSeverity.Error);
        msg.Code.ShouldBe("HTTP_AUTH_FAILED");
        msg.Message.ShouldBe("HTTP authentication failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AuthenticationTypeConstructorIncludesType()
    {
        var msg = new HttpAuthenticationFailedMessage("Bearer");

        msg.Message.ShouldContain("Bearer");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AuthenticationTypeAndReasonConstructorIncludesBoth()
    {
        var msg = new HttpAuthenticationFailedMessage("Bearer", "token expired");

        msg.Message.ShouldContain("Bearer");
        msg.Message.ShouldContain("token expired");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FullContextConstructorIncludesEndpoint()
    {
        var msg = new HttpAuthenticationFailedMessage("ApiKey", "invalid key", "https://api.example.com");

        msg.Message.ShouldContain("ApiKey");
        msg.Message.ShouldContain("invalid key");
        msg.Message.ShouldContain("https://api.example.com");
    }
}
