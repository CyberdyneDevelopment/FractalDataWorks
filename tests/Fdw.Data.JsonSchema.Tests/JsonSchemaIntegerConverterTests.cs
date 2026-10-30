using Fdw.Data.JsonSchema;

namespace Fdw.Data.JsonSchema.Tests;

public sealed class JsonSchemaIntegerConverterTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BySourceTypeFindsTheIntegerWithNoFormatAsInt64()
    {
        var converter = JsonSchemaConverters.BySourceType("integer");

        converter.ShouldNotBe(JsonSchemaConverters.NotFound);
        converter.TargetClrType.ShouldBe(typeof(long));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ToClrConvertsADatabaseValueToInt64()
    {
        new JsonSchemaIntegerConverter().ToClr(7).ShouldBe(7L);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ToClrKeepsANullAsNull()
    {
        new JsonSchemaIntegerConverter().ToClr(null).ShouldBeNull();
    }
}
