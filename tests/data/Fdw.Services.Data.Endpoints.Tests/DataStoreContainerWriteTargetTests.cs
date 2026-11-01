using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-1757: a container states where an HTTP destination sends its rows, with which method and as which content type;
/// a part no destination could use is refused, and one that is not stated is never refused or defaulted.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Configuration")]
public sealed class DataStoreContainerWriteTargetTests
{
    private static bool Refused(string? endpoint = null, string? method = null, string? contentType = null)
        => DataStoreContainerWriteTarget.Refusal("BreweryOut", endpoint, method, contentType, NullLogger.Instance) is not null;

    [Fact]
    public void NothingStatedIsNeverRefused() => Refused().ShouldBeFalse();

    [Fact]
    public void AFullyStatedTargetIsTaken() => Refused("/api/v1/ingest", "POST", "application/json").ShouldBeFalse();

    [Theory]
    [InlineData("POST", false)]
    [InlineData("PUT", false)]
    [InlineData("post", false)]
    [InlineData("Put", false)]
    [InlineData("GET", true)]
    [InlineData("DELETE", true)]
    [InlineData("PATCH", true)]
    [InlineData("", true)]
    public void OnlyPostAndPutAreTakenAsTheMethod(string method, bool refused) => Refused(method: method).ShouldBe(refused);

    [Theory]
    [InlineData("/api/v1/ingest", false)]
    [InlineData("api/v1/ingest", false)]
    [InlineData("/ingest?source=etl", false)]
    [InlineData("https://other.example.com/ingest", true)]
    [InlineData("http://other.example.com/ingest", true)]
    [InlineData("//other.example.com/ingest", true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    public void TheEndpointIsAPathAfterTheConnectionsAddressNeverAFullWebAddress(string endpoint, bool refused)
        => Refused(endpoint: endpoint).ShouldBe(refused);

    [Theory]
    [InlineData("application/json", false)]
    [InlineData("text/csv", false)]
    [InlineData("text/csv; charset=utf-8", false)]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", false)]
    [InlineData("json", true)]
    [InlineData("", true)]
    [InlineData("not a type", true)]
    public void TheContentTypeIsAMediaType(string contentType, bool refused) => Refused(contentType: contentType).ShouldBe(refused);
}
