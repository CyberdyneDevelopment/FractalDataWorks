using System;
using Fdw.Data.JsonSchema;

namespace Fdw.Data.JsonSchema.Tests;

public sealed class JsonSchemaConverterGapTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void NumberFloatConverterShouldConvertToClr()
    {
        var converter = new JsonSchemaNumberFloatConverter();

        var result = converter.ToClr(3.14f);

        result.ShouldBe(3.14f);
        result.ShouldBeOfType<float>();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void NumberFloatConverterShouldConvertFromDouble()
    {
        var converter = new JsonSchemaNumberFloatConverter();

        var result = converter.ToClr(3.14);

        result.ShouldBeOfType<float>();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void NumberFloatConverterShouldReturnNullForNull()
    {
        var converter = new JsonSchemaNumberFloatConverter();

        converter.ToClr(null).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void NumberFloatConverterShouldReturnNullForDBNull()
    {
        var converter = new JsonSchemaNumberFloatConverter();

        converter.ToClr(DBNull.Value).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void NumberFloatConverterToDbPassesThrough()
    {
        var converter = new JsonSchemaNumberFloatConverter();

        converter.ToDb(3.14f).ShouldBe(3.14f);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void NumberDoubleConverterShouldConvertToClr()
    {
        var converter = new JsonSchemaNumberDoubleConverter();

        var result = converter.ToClr(3.14159265);

        result.ShouldBe(3.14159265);
        result.ShouldBeOfType<double>();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void NumberDoubleConverterShouldReturnNullForNull()
    {
        var converter = new JsonSchemaNumberDoubleConverter();

        converter.ToClr(null).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void NumberDoubleConverterShouldReturnNullForDBNull()
    {
        var converter = new JsonSchemaNumberDoubleConverter();

        converter.ToClr(DBNull.Value).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void NumberDoubleConverterToDbPassesThrough()
    {
        var converter = new JsonSchemaNumberDoubleConverter();

        converter.ToDb(3.14).ShouldBe(3.14);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void StringTimeConverterShouldConvertTimeOnly()
    {
        var converter = new JsonSchemaStringTimeConverter();
        var time = new TimeOnly(14, 30, 0);

        var result = converter.ToClr(time);

        result.ShouldBe(time);
        result.ShouldBeOfType<TimeOnly>();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void StringTimeConverterShouldConvertTimeSpan()
    {
        var converter = new JsonSchemaStringTimeConverter();
        var ts = new TimeSpan(14, 30, 0);

        var result = converter.ToClr(ts);

        result.ShouldBeOfType<TimeOnly>();
        ((TimeOnly)result!).Hour.ShouldBe(14);
        ((TimeOnly)result).Minute.ShouldBe(30);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void StringTimeConverterShouldParseString()
    {
        var converter = new JsonSchemaStringTimeConverter();

        var result = converter.ToClr("14:30:00");

        result.ShouldBeOfType<TimeOnly>();
        ((TimeOnly)result!).Hour.ShouldBe(14);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void StringTimeConverterShouldReturnNullForNull()
    {
        var converter = new JsonSchemaStringTimeConverter();

        converter.ToClr(null).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void StringTimeConverterShouldReturnNullForDBNull()
    {
        var converter = new JsonSchemaStringTimeConverter();

        converter.ToClr(DBNull.Value).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ArrayConverterShouldConvertToString()
    {
        var converter = new JsonSchemaArrayConverter();

        var result = converter.ToClr("[1,2,3]");

        result.ShouldBe("[1,2,3]");
        result.ShouldBeOfType<string>();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ArrayConverterShouldReturnNullForNull()
    {
        var converter = new JsonSchemaArrayConverter();

        converter.ToClr(null).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ArrayConverterShouldReturnNullForDBNull()
    {
        var converter = new JsonSchemaArrayConverter();

        converter.ToClr(DBNull.Value).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ArrayConverterToDbPassesThrough()
    {
        var converter = new JsonSchemaArrayConverter();

        converter.ToDb("[1,2]").ShouldBe("[1,2]");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ObjectConverterShouldConvertToString()
    {
        var converter = new JsonSchemaObjectConverter();

        var result = converter.ToClr("{\"key\":\"value\"}");

        result.ShouldBe("{\"key\":\"value\"}");
        result.ShouldBeOfType<string>();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ObjectConverterShouldReturnNullForNull()
    {
        var converter = new JsonSchemaObjectConverter();

        converter.ToClr(null).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ObjectConverterShouldReturnNullForDBNull()
    {
        var converter = new JsonSchemaObjectConverter();

        converter.ToClr(DBNull.Value).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ObjectConverterToDbPassesThrough()
    {
        var converter = new JsonSchemaObjectConverter();

        converter.ToDb("{\"a\":1}").ShouldBe("{\"a\":1}");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void BySourceTypeFindsAllCompositeFormats()
    {
        JsonSchemaConverters.BySourceType("integer+int32").ShouldNotBe(JsonSchemaConverters.NotFound);
        JsonSchemaConverters.BySourceType("integer+int64").ShouldNotBe(JsonSchemaConverters.NotFound);
        JsonSchemaConverters.BySourceType("number+float").ShouldNotBe(JsonSchemaConverters.NotFound);
        JsonSchemaConverters.BySourceType("number+double").ShouldNotBe(JsonSchemaConverters.NotFound);
        JsonSchemaConverters.BySourceType("string+date-time").ShouldNotBe(JsonSchemaConverters.NotFound);
        JsonSchemaConverters.BySourceType("string+date").ShouldNotBe(JsonSchemaConverters.NotFound);
        JsonSchemaConverters.BySourceType("string+time").ShouldNotBe(JsonSchemaConverters.NotFound);
        JsonSchemaConverters.BySourceType("string+uuid").ShouldNotBe(JsonSchemaConverters.NotFound);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void BySourceTypeFindsSimpleTypes()
    {
        JsonSchemaConverters.BySourceType("string").ShouldNotBe(JsonSchemaConverters.NotFound);
        JsonSchemaConverters.BySourceType("number").ShouldNotBe(JsonSchemaConverters.NotFound);
        JsonSchemaConverters.BySourceType("boolean").ShouldNotBe(JsonSchemaConverters.NotFound);
        JsonSchemaConverters.BySourceType("array").ShouldNotBe(JsonSchemaConverters.NotFound);
        JsonSchemaConverters.BySourceType("object").ShouldNotBe(JsonSchemaConverters.NotFound);
    }
}
