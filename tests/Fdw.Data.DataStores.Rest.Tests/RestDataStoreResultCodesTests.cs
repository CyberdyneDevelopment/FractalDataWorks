using System.Linq;
using Fdw.Data.DataStores.Rest.Results;
using Xunit;
using Shouldly;

namespace Fdw.Data.DataStores.Rest.Tests;

/// <summary>
/// Tests for the <see cref="RestDataStoreResultCodes"/> TypeCollection.
/// </summary>
public sealed class RestDataStoreResultCodesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AllReturnsAllResultCodes()
    {
        // Act
        var all = RestDataStoreResultCodes.All();

        // Assert
        all.ShouldNotBeNull();
        all.ShouldNotBeEmpty();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData("ODataServiceUrlRequired")]
    [InlineData("InvalidODataSource")]
    [InlineData("ODataEntitySetPathFailed")]
    [InlineData("ODataImportFailed")]
    [InlineData("ODataMetadataFetchFailed")]
    [InlineData("ODataMetadataParsingFailed")]
    [InlineData("InvalidOpenApiSource")]
    [InlineData("OpenApiEndpointPathFailed")]
    [InlineData("OpenApiFileNotFound")]
    [InlineData("OpenApiImportFailed")]
    [InlineData("OpenApiParsingFailed")]
    [InlineData("OpenApiSpecFetchFailed")]
    [InlineData("OpenApiSpecRequired")]
    public void ByNameResolvesEachDocumentedCode(string name)
    {
        // Act
        var code = RestDataStoreResultCodes.ByName(name);

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe(name);
        code.ShouldNotBe(RestDataStoreResultCodes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsNotFoundSentinelForUnknownName()
    {
        // Act
        var code = RestDataStoreResultCodes.ByName("SomethingThatDoesNotExist");

        // Assert
        // Why: TypeCollection lookups return the NotFound sentinel on miss, never null.
        code.ShouldBe(RestDataStoreResultCodes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllResultCodesFollowCatalogInvariants()
    {
        // Codes carry categorized numbers (resultcode-catalog): Code == "REST-{number}",
        // Id == EventId == number, Domain == "REST".
        foreach (var result in RestDataStoreResultCodes.All())
        {
            if (string.Equals(result.Name, "NotFound", System.StringComparison.Ordinal))
            {
                continue;
            }

            result.Code.ShouldBe($"REST-{result.Id}");
            result.EventId.ShouldBe(result.Id);
            result.Domain.ShouldBe("REST");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllCodesHaveUniqueIds()
    {
        // Act
        var ids = RestDataStoreResultCodes.All().Select(rc => rc.Id).ToList();

        // Assert
        ids.ShouldBeUnique();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllCodesHaveUniqueNames()
    {
        // Act
        var names = RestDataStoreResultCodes.All().Select(rc => rc.Name).ToList();

        // Assert
        names.ShouldBeUnique();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void OpenApiFileNotFoundCodeFormatsFilePathPlaceholder()
    {
        // Arrange
        var code = RestDataStoreResultCodes.ByName("OpenApiFileNotFound");
        using var details = Fdw.Results.ResultDetails.Create("FilePath", "/tmp/missing-spec.json");

        // Act
        var message = code.FormatMessage(details);

        // Assert
        message.ShouldBe("File not found: /tmp/missing-spec.json");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ODataServiceUrlRequiredCodeIsNotRetryable()
    {
        // Act
        var code = RestDataStoreResultCodes.ByName("ODataServiceUrlRequired");

        // Assert
        code.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ODataMetadataFetchFailedCodeIsRetryable()
    {
        // Act
        var code = RestDataStoreResultCodes.ByName("ODataMetadataFetchFailed");

        // Assert
        // Why: transient network/endpoint failures are retryable; validation-style guard failures
        // (e.g. ODataServiceUrlRequired above) are not.
        code.IsRetryable.ShouldBeTrue();
    }
}
