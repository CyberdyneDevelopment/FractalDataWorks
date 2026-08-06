using Fdw.Messages;
using Fdw.Services.Connections.Http.Abstractions.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Abstractions.Tests.Messages;

public class HttpRequestTimeoutMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultConstructorSetsProperties()
    {
        var msg = new HttpRequestTimeoutMessage();

        msg.Id.ShouldBe(4101);
        msg.Name.ShouldBe("HttpRequestTimeout");
        msg.Severity.ShouldBe(MessageSeverity.Warning);
        msg.Code.ShouldBe("HTTP_REQUEST_TIMEOUT");
        msg.Message.ShouldBe("HTTP request timed out");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TimeoutSecondsConstructorIncludesDuration()
    {
        var msg = new HttpRequestTimeoutMessage(30);

        msg.Message.ShouldContain("30");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TimeoutAndEndpointConstructorIncludesBoth()
    {
        var msg = new HttpRequestTimeoutMessage(60, "https://api.example.com");

        msg.Message.ShouldContain("60");
        msg.Message.ShouldContain("https://api.example.com");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FullContextConstructorIncludesHttpMethod()
    {
        var msg = new HttpRequestTimeoutMessage(30, "https://api.example.com/data", "POST");

        msg.Message.ShouldContain("POST");
        msg.Message.ShouldContain("30");
        msg.Message.ShouldContain("https://api.example.com/data");
    }
}
