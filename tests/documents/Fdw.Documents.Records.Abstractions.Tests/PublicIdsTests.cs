using Fdw.Documents.Records.Abstractions;
using Fdw.Documents.Records.Abstractions.Results;

namespace Fdw.Documents.Records.Abstractions.Tests;

/// <summary>
/// Tests for the key rule of the document record layer (FDW-3395): a public id is a UUIDv7 the application set; an empty id or one of
/// another version or variant is refused, and none is generated in its place.
/// </summary>
public sealed class PublicIdsTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Validation")]
    public void AVersion7IdIsAcceptedAsItIs()
    {
        var id = Guid.CreateVersion7();

        var result = PublicIds.Check(id, "Document");

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBe(id);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Validation")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("3f2504e0-4f89-41d3-9a0c-0305e82c3301")]
    [InlineData("0190a1b2-c3d4-7e5f-c000-000000000001")]
    public void AnEmptyIdAnotherVersionOrAnotherVariantIsRefusedAndNothingIsGenerated(string text)
    {
        var result = PublicIds.Check(Guid.Parse(text), "StoredMedia");

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBeSameAs(DocumentRecordResultCodes.ByName("PublicIdInvalid"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Validation")]
    public void TheRefusalNamesTheEntityAndTheId()
    {
        var id = Guid.Parse("3f2504e0-4f89-41d3-9a0c-0305e82c3301");

        var result = PublicIds.Check(id, "RecordNode");

        var shown = result.Code.ShouldNotBeNull().FormatMessage(result.Details);
        shown.ShouldContain("RecordNode");
        shown.ShouldContain("3f2504e0-4f89-41d3-9a0c-0305e82c3301");
        shown.ShouldContain("version 4");
    }
}
