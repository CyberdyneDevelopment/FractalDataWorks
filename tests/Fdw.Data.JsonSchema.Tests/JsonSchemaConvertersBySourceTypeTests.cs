using Fdw.Data.JsonSchema;

namespace Fdw.Data.JsonSchema.Tests;

/// <summary>
/// Tests the BySourceType() lookup logic in JsonSchemaConverters.
/// This is custom logic (not source-generated) and needs test coverage.
/// </summary>
public class JsonSchemaConvertersBySourceTypeTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void BySourceTypeShouldFindSimpleType()
    {
        // Act
        var result = JsonSchemaConverters.BySourceType("string");

        // Assert
        result.ShouldNotBe(JsonSchemaConverters.NotFound);
        result.SourceType.ShouldBe("string");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void BySourceTypeShouldFindCompositeKey()
    {
        // Act
        var result = JsonSchemaConverters.BySourceType("integer+int64");

        // Assert
        result.ShouldNotBe(JsonSchemaConverters.NotFound);
        result.SourceType.ShouldBe("integer+int64");
    }
}
