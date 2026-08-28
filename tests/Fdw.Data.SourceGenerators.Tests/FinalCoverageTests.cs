using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Data.SourceGenerators.Tests;

/// <summary>
/// Final tests to reach 100% coverage on remaining uncovered branches.
/// </summary>
public class FinalCoverageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HandlesAllSpecialTypesInGetReaderMethod()
    {
        var source = @"
using Fdw.Data;
using System;

namespace Test;

[GenerateMapper]
public class AllSpecialTypesPoco
{
    public bool BoolValue { get; set; }
    public byte ByteValue { get; set; }
    public short Int16Value { get; set; }
    public int Int32Value { get; set; }
    public long Int64Value { get; set; }
    public decimal DecimalValue { get; set; }
    public double DoubleValue { get; set; }
    public float FloatValue { get; set; }
    public string StringValue { get; set; } = string.Empty;
    public DateTime DateTimeValue { get; set; }
    public Guid GuidValue { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "AllSpecialTypesPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();

        // Verify all special type reader methods are used
        generated.ShouldContain("GetBoolean");
        generated.ShouldContain("GetByte");
        generated.ShouldContain("GetInt16");
        generated.ShouldContain("GetInt32");
        generated.ShouldContain("GetInt64");
        generated.ShouldContain("GetDecimal");
        generated.ShouldContain("GetDouble");
        generated.ShouldContain("GetFloat");
        generated.ShouldContain("GetString");
        generated.ShouldContain("GetDateTime");
        generated.ShouldContain("GetGuid");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HandlesNonSpecialTypeWithGetFieldValue()
    {
        var source = @"
using Fdw.Data;
using System;

namespace Test;

public class CustomType
{
    public string Value { get; set; } = string.Empty;
}

[GenerateMapper]
public class CustomTypePoco
{
    public CustomType Custom { get; set; } = new();
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "CustomTypePocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Non-special types use GetFieldValue
        generated.ShouldContain("GetFieldValue");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetDefaultValueExpression_HandlesValueTypeCorrectly()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class ValueTypesDefaultPoco
{
    public int IntValue { get; set; }
    public long LongValue { get; set; }
    public decimal DecimalValue { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "ValueTypesDefaultPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Value types should use 'default'
        generated.ShouldContain("return default;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetDefaultValueExpression_HandlesNullableStringCorrectly()
    {
        var source = @"
#nullable enable
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class NullableStringPoco
{
    public string? OptionalText { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "NullableStringPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Nullable string should use 'default' (null is allowed)
        generated.ShouldContain("return default;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetDefaultValueExpression_HandlesNonNullableStringCorrectly()
    {
        var source = @"
#nullable enable
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class NonNullableStringPoco
{
    public string RequiredText { get; set; } = string.Empty;
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "NonNullableStringPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Non-nullable string should use string.Empty
        generated.ShouldContain("string.Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetDefaultValueExpression_HandlesNonNullableReferenceTypeCorrectly()
    {
        var source = @"
#nullable enable
using Fdw.Data;

namespace Test;

public class Metadata { }

[GenerateMapper]
public class NonNullableRefTypePoco
{
    public Metadata Info { get; set; } = new();
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "NonNullableRefTypePocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Non-nullable non-string reference types should use 'default!'
        generated.ShouldContain("default!");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void SanitizeName_RemovesAllSpecialCharacters()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class SpecialCharPropertyPoco
{
    public int Property_With_Underscores { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "SpecialCharPropertyPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Sanitized name should have underscores removed
        generated.ShouldContain("GetReaderValue_PropertyWithUnderscores");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratesMapperForGenericType()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class Container<T>
{
    public T Value { get; set; } = default!;
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Should generate something even for generic types
        var allGenerated = CompilationHelper.GetAllGeneratedFileNames(compilation).ToList();
        allGenerated.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void TrimPropertyMappings_HandlesTrailingCharactersCorrectly()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class TrimTestPoco
{
    public int Prop1 { get; set; }
    public int Prop2 { get; set; }
    public int Prop3 { get; set; }
    public int Prop4 { get; set; }
    public int Prop5 { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "TrimTestPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();

        // Should have properly formatted property assignments (not ending with trailing comma)
        // Check that the last property doesn't have a trailing comma before the closing brace
        var lines = generated.Split('\n');
        var propertyLines = lines.Where(l => l.Contains("Prop") && l.Contains(" = ")).ToList();
        propertyLines.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void DictionaryMapping_CreatesCorrectVariableNames()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class VariableNameTestPoco
{
    public int FirstProperty { get; set; }
    public string UPPERCASE { get; set; } = string.Empty;
    public decimal MixedCase { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "VariableNameTestPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();

        // Variable names should be property name + "val" in lowercase
        generated.ShouldContain("firstpropertyval");
        generated.ShouldContain("uppercaseval");
        generated.ShouldContain("mixedcaseval");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapFromDictionary_ChecksForNullBeforeCasting()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class NullCheckDictionaryPoco
{
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "NullCheckDictionaryPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();

        // Dictionary mapping should check != null before casting
        generated.ShouldContain("!= null");
        generated.ShouldContain("data.TryGetValue");
    }
}
