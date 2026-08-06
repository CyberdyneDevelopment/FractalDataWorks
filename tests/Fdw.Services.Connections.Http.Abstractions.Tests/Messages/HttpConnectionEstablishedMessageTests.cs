using Fdw.Messages;
using Fdw.Services.Connections.Http.Abstractions.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Abstractions.Tests.Messages;

public class HttpConnectionEstablishedMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultConstructorSetsProperties()
    {
        var msg = new HttpConnectionEstablishedMessage();

        msg.Id.ShouldBe(4002);
        msg.Name.ShouldBe("HttpConnectionEstablished");
        msg.Severity.ShouldBe(MessageSeverity.Information);
        msg.Code.ShouldBe("HTTP_CONNECTED");
        msg.Message.ShouldBe("HTTP connection established successfully");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointConstructorIncludesEndpoint()
    {
        var msg = new HttpConnectionEstablishedMessage("https://api.example.com");

        msg.Message.ShouldContain("https://api.example.com");
    }
}
