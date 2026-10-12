using Fdw.Messages;
using Fdw.Services.Connections.Http.Abstractions.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Abstractions.Tests.Messages;

public class HttpResponseErrorMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultConstructorSetsProperties()
    {
        var msg = new HttpResponseErrorMessage();

        msg.Id.ShouldBe(4003);
        msg.Name.ShouldBe("HttpResponseError");
        msg.Severity.ShouldBe(MessageSeverity.Error);
        msg.Code.ShouldBe("HTTP_RESPONSE_ERROR");
        msg.Message.ShouldBe("HTTP response error");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void StatusCodeConstructorIncludesCode()
    {
        var msg = new HttpResponseErrorMessage(404);

        msg.Message.ShouldContain("404");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void StatusCodeWithReasonPhraseIncludesBoth()
    {
        var msg = new HttpResponseErrorMessage(404, "Not Found");

        msg.Message.ShouldContain("404");
        msg.Message.ShouldContain("Not Found");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void StatusCodeWithNullReasonPhraseDoesNotThrow()
    {
        var msg = new HttpResponseErrorMessage(500, null);

        msg.Message.ShouldContain("500");
    }
}
