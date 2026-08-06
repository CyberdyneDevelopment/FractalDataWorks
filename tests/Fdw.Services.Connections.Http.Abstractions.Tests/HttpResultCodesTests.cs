using Fdw.Services.Connections.Http.Abstractions.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Abstractions.Tests;

public class HttpResultCodesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllReturnsAllResultCodes()
    {
        // Act
        var all = HttpResultCodes.All();

        // Assert
        all.ShouldNotBeEmpty();
        all.Count.ShouldBeGreaterThan(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByNameReturnsRequestCancelled()
    {
        // Act
        var code = HttpResultCodes.ByName("RequestCancelled");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("RequestCancelled");
        // Catalog invariant: Code == "HTTP-{number}", Id == EventId == number, Domain == "HTTP".
        code.Code.ShouldBe($"HTTP-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("HTTP");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByNameReturnsRequestTimeout()
    {
        // Act
        var code = HttpResultCodes.ByName("RequestTimeout");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("RequestTimeout");
        code.Code.ShouldBe($"HTTP-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("HTTP");
        code.IsRetryable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByNameReturnsRequestFailed()
    {
        // Act
        var code = HttpResultCodes.ByName("RequestFailed");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("RequestFailed");
        code.Code.ShouldBe($"HTTP-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("HTTP");
        code.IsRetryable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByNameReturnsUnexpectedError()
    {
        // Act
        var code = HttpResultCodes.ByName("UnexpectedError");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("UnexpectedError");
        code.Code.ShouldBe($"HTTP-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("HTTP");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByNameReturnsCertificateLoadFailed()
    {
        // Act
        var code = HttpResultCodes.ByName("CertificateLoadFailed");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("CertificateLoadFailed");
        code.Code.ShouldBe($"HTTP-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("HTTP");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByNameReturnsCommandTranslationFailed()
    {
        // Act
        var code = HttpResultCodes.ByName("CommandTranslationFailed");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("CommandTranslationFailed");
        code.Code.ShouldBe($"HTTP-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("HTTP");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByNameReturnsHttpErrorResponse()
    {
        // Act
        var code = HttpResultCodes.ByName("HttpErrorResponse");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("HttpErrorResponse");
        code.Code.ShouldBe($"HTTP-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("HTTP");
        code.IsRetryable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByNameReturnsResponseDeserializationFailed()
    {
        // Act
        var code = HttpResultCodes.ByName("ResponseDeserializationFailed");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("ResponseDeserializationFailed");
        code.Code.ShouldBe($"HTTP-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("HTTP");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByNameReturnsRestRequestBuildFailed()
    {
        // Act
        var code = HttpResultCodes.ByName("RestRequestBuildFailed");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("RestRequestBuildFailed");
        code.Code.ShouldBe($"HTTP-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("HTTP");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByNameReturnsGraphQLError()
    {
        // Act
        var code = HttpResultCodes.ByName("GraphQLError");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("GraphQLError");
        code.Code.ShouldBe($"HTTP-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("HTTP");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByNameReturnsSoapFault()
    {
        // Act
        var code = HttpResultCodes.ByName("SoapFault");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("SoapFault");
        code.Code.ShouldBe($"HTTP-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("HTTP");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByNameReturnsWsSecurityMissingCertificate()
    {
        // Act
        var code = HttpResultCodes.ByName("WsSecurityMissingCertificate");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("WsSecurityMissingCertificate");
        code.Code.ShouldBe($"HTTP-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("HTTP");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var code = HttpResultCodes.ByName("UnknownCode");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("NotFound");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByIdReturnsCorrectCode()
    {
        // Arrange
        var timeoutCode = HttpResultCodes.ByName("RequestTimeout");

        // Act
        var code = HttpResultCodes.ById(timeoutCode.Id);

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("RequestTimeout");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        // Act
        var code = HttpResultCodes.ById(99999);

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("NotFound");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var notFound = HttpResultCodes.NotFound;

        // Assert
        notFound.ShouldNotBeNull();
        notFound.Name.ShouldBe("NotFound");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllCodesHaveUniqueIds()
    {
        // Act
        var all = HttpResultCodes.All();
        var ids = all.Select(c => c.Id).ToList();

        // Assert
        ids.Distinct().Count().ShouldBe(ids.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllCodesHaveUniqueNames()
    {
        // Act
        var all = HttpResultCodes.All();
        var names = all.Select(c => c.Name).ToList();

        // Assert
        names.Distinct().Count().ShouldBe(names.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllCodesHaveUniqueCodes()
    {
        // Act
        var all = HttpResultCodes.All();
        var codes = all.Select(c => c.Code).ToList();

        // Assert
        codes.Distinct().Count().ShouldBe(codes.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllCodesHaveUniqueEventIds()
    {
        // Act
        var all = HttpResultCodes.All();
        var eventIds = all.Select(c => c.EventId).ToList();

        // Assert
        eventIds.Distinct().Count().ShouldBe(eventIds.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllCodesHaveMessageTemplate()
    {
        // Act
        var all = HttpResultCodes.All();

        // Assert
        all.ShouldAllBe(c => !string.IsNullOrWhiteSpace(c.MessageTemplate));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllCodesFollowCatalogInvariants()
    {
        // Codes are categorized numbers (resultcode-catalog): Code == "HTTP-{number}",
        // Id == EventId == number, Domain == "HTTP". Assert the invariants rather than a
        // fixed EventId band so a future renumber does not re-break this test.
        foreach (var code in HttpResultCodes.All())
        {
            if (string.Equals(code.Name, "NotFound", System.StringComparison.Ordinal))
            {
                continue;
            }

            code.Code.ShouldBe($"HTTP-{code.Id}");
            code.EventId.ShouldBe(code.Id);
            code.Domain.ShouldBe("HTTP");
        }
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    [InlineData("RequestTimeout", true)]
    [InlineData("RequestFailed", true)]
    [InlineData("HttpErrorResponse", true)]
    [InlineData("SoapHttpError", true)]
    [InlineData("GraphQLHttpError", true)]
    [InlineData("RequestCancelled", false)]
    [InlineData("UnexpectedError", false)]
    public void RetryableFlagsAreCorrect(string codeName, bool expectedRetryable)
    {
        // Act
        var code = HttpResultCodes.ByName(codeName);

        // Assert
        code.ShouldNotBeNull();
        code.IsRetryable.ShouldBe(expectedRetryable);
    }
}
