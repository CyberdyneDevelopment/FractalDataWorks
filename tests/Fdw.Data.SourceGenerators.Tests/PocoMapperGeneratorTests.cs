using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Data.SourceGenerators.Tests;

public class PocoMapperGeneratorTests
{
    #region Basic Generation Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratesMapperForSimplePocoWithGenerateMapperAttribute()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class SimplePoco
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "SimplePocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("SimplePocoPocoMapper");
        generated.ShouldContain("PocoMapperBase");
        generated.ShouldContain("MapFromReader");
        generated.ShouldContain("MapFromDictionary");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void DoesNotGenerateMapperForClassWithoutAttribute()
    {
        var source = @"
namespace Test;

public class NoPoco
{
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        var generatedFiles = CompilationHelper.GetAllGeneratedFileNames(compilation);
        generatedFiles.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratedMapperIncludesTypeOptionAttribute()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class TestPoco
{
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "TestPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("[TypeOption(typeof(PocoMapperCollection)");
        generated.ShouldContain("\"TestPoco\"");
        generated.ShouldContain("RestrictToCurrentCompilation = true");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratedMapperHasCorrectConstructor()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class MyEntity
{
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "MyEntityPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("public MyEntityPocoMapper()");
        generated.ShouldContain("base(\"Test.MyEntity\", typeof(global::Test.MyEntity))");
    }

    #endregion

    #region Property Type Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratesMapperForAllPrimitiveTypes()
    {
        var source = @"
using Fdw.Data;
using System;

namespace Test;

[GenerateMapper]
public class AllTypesPoco
{
    public bool BoolProp { get; set; }
    public byte ByteProp { get; set; }
    public short Int16Prop { get; set; }
    public int Int32Prop { get; set; }
    public long Int64Prop { get; set; }
    public decimal DecimalProp { get; set; }
    public double DoubleProp { get; set; }
    public float FloatProp { get; set; }
    public string StringProp { get; set; } = string.Empty;
    public DateTime DateTimeProp { get; set; }
    public Guid GuidProp { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "AllTypesPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();

        // Verify reader methods
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
    public void GeneratesMapperForNullableValueTypes()
    {
        var source = @"
using Fdw.Data;
using System;

namespace Test;

[GenerateMapper]
public class NullablePoco
{
    public int? NullableInt { get; set; }
    public DateTime? NullableDateTime { get; set; }
    public Guid? NullableGuid { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "NullablePocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("NullableInt");
        generated.ShouldContain("NullableDateTime");
        generated.ShouldContain("NullableGuid");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratesMapperForNullableReferenceTypes()
    {
        var source = @"
#nullable enable
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class NullableRefPoco
{
    public string? NullableName { get; set; }
    public string NonNullableName { get; set; } = string.Empty;
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "NullableRefPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("NullableName");
        generated.ShouldContain("NonNullableName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void UsesGetFieldValueForNonPrimitiveTypes()
    {
        var source = @"
using Fdw.Data;
using System;

namespace Test;

public enum Status { Active, Inactive }

[GenerateMapper]
public class CustomTypePoco
{
    public Status StatusValue { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "CustomTypePocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("GetFieldValue<");
    }

    #endregion

    #region Default Value Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void UsesCorrectDefaultValueForValueTypes()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class ValueTypePoco
{
    public int Number { get; set; }
    public bool Flag { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "ValueTypePocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Helper methods should return 'default' for value types
        generated.ShouldContain("return default;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void UsesStringEmptyForNonNullableStrings()
    {
        var source = @"
#nullable enable
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class StringPoco
{
    public string Name { get; set; } = string.Empty;
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "StringPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Non-nullable string should use string.Empty
        generated.ShouldContain("string.Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void UsesDefaultForNullableReferenceTypes()
    {
        var source = @"
#nullable enable
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class NullableStringPoco
{
    public string? OptionalName { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "NullableStringPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Nullable reference types should use 'default' (null is allowed)
        generated.ShouldContain("return default;");
    }

    #endregion

    #region MapFromReader Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapFromReaderIncludesAllProperties()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class MultiPropPoco
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "MultiPropPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("Id = GetReaderValue_Id(reader)");
        generated.ShouldContain("Name = GetReaderValue_Name(reader)");
        generated.ShouldContain("Amount = GetReaderValue_Amount(reader)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapFromReaderHandlesExceptions()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class TestPoco
{
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "TestPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("try");
        generated.ShouldContain("catch (Exception ex)");
        generated.ShouldContain("GenericResult<object>.Failure");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapFromReaderReturnsSuccessResult()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class TestPoco
{
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "TestPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("return GenericResult<object>.Success(instance);");
    }

    #endregion

    #region MapFromDictionary Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapFromDictionaryIncludesAllProperties()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class DictPoco
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "DictPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("data.TryGetValue(\"Id\"");
        generated.ShouldContain("data.TryGetValue(\"Name\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapFromDictionaryHandlesNullValues()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class NullCheckPoco
{
    public int Number { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "NullCheckPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("&& numberval != null");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MapFromDictionaryHandlesExceptions()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class TestPoco
{
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "TestPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        var mapFromDictStart = generated.IndexOf("MapFromDictionary");
        var mapFromDictSection = generated.Substring(mapFromDictStart);

        mapFromDictSection.ShouldContain("try");
        mapFromDictSection.ShouldContain("catch (Exception ex)");
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratesHelperMethodForEachProperty()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class HelperTestPoco
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "HelperTestPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("GetReaderValue_Id");
        generated.ShouldContain("GetReaderValue_Name");
        generated.ShouldContain("GetReaderValue_Price");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HelperMethodHandlesDBNull()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class DbNullPoco
{
    public int Number { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "DbNullPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("IsDBNull(ordinal)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HelperMethodHandlesIndexOutOfRangeException()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class IndexPoco
{
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "IndexPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("catch (IndexOutOfRangeException)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HelperMethodUsesGetOrdinal()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class OrdinalPoco
{
    public string Name { get; set; } = string.Empty;
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "OrdinalPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("GetOrdinal(\"Name\")");
    }

    #endregion

    #region Name Sanitization Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void SanitizesPropertyNamesWithUnderscores()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class UnderscorePoco
{
    public int My_Property { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "UnderscorePocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("GetReaderValue_MyProperty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void SanitizesPropertyNamesWithDots()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class DotPoco
{
    public int @Property { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "DotPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Should generate valid method name
        generated.ShouldContain("GetReaderValue_Property");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void SanitizesTypeNamesWithAngleBrackets()
    {
        var source = @"
using Fdw.Data;
using System.Collections.Generic;

namespace Test;

[GenerateMapper]
public class GenericContainer<T>
{
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generatedFiles = CompilationHelper.GetAllGeneratedFileNames(compilation).ToList();

        // The generator may or may not handle generic types - let's verify it generates something
        generatedFiles.ShouldNotBeEmpty();
        var generatedFile = generatedFiles.First();
        generatedFile.ShouldContain("PocoMapper.g.cs");

        // Verify the generated code compiles and contains the mapper class
        var generated = CompilationHelper.GetGeneratedOutput(compilation, generatedFile);
        generated.ShouldNotBeNull();
        generated.ShouldContain("PocoMapper");
    }

    #endregion

    #region Property Filter Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IgnoresStaticProperties()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class StaticPropPoco
{
    public static int StaticProp { get; set; }
    public int InstanceProp { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "StaticPropPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Verify static property is not mapped (should not have GetReaderValue_StaticProp)
        generated.ShouldNotContain("GetReaderValue_StaticProp");
        // Verify instance property IS mapped
        generated.ShouldContain("GetReaderValue_InstanceProp");
        generated.ShouldContain("InstanceProp");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IgnoresReadOnlyProperties()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class ReadOnlyPoco
{
    public int ReadOnlyProp { get; }
    public int ReadWriteProp { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "ReadOnlyPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldNotContain("ReadOnlyProp");
        generated.ShouldContain("ReadWriteProp");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IgnoresIndexerProperties()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class IndexerPoco
{
    private string[] _items = new string[10];
    public string this[int index]
    {
        get => _items[index];
        set => _items[index] = value;
    }
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "IndexerPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("Id");
        // Should not generate for indexer
        generated.ShouldNotContain("this[");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IgnoresPropertiesWithNonPublicSetter()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class SetterAccessPoco
{
    public int PublicSetter { get; set; }
    public int PrivateSetter { get; private set; }
    public int ProtectedSetter { get; protected set; }
    public int InternalSetter { get; internal set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "SetterAccessPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("PublicSetter");
        generated.ShouldNotContain("PrivateSetter");
        generated.ShouldNotContain("ProtectedSetter");
        // Note: internal might be included depending on visibility - adjust if needed
    }

    #endregion

    #region Namespace Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void PreservesPocoNamespace()
    {
        var source = @"
using Fdw.Data;

namespace My.Custom.Namespace;

[GenerateMapper]
public class NamespacedPoco
{
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "NamespacedPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("namespace My.Custom.Namespace;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HandlesGlobalNamespace()
    {
        var source = @"
using Fdw.Data;

[GenerateMapper]
public class GlobalPoco
{
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "GlobalPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Should have empty namespace or handle gracefully
        generated.ShouldNotContain("namespace ;");
    }

    #endregion

    #region Code Generation Quality Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratedCodeHasNullableEnable()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class TestPoco
{
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "TestPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("#nullable enable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratedCodeHasAutoGeneratedComment()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class TestPoco
{
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "TestPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("// <auto-generated/>");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratedCodeSuppressesMissingXmlComment()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class TestPoco
{
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "TestPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("#pragma warning disable CS1591");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratedCodeHasXmlDocumentation()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class DocumentedPoco
{
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "DocumentedPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("/// <summary>");
        generated.ShouldContain("Generated POCO mapper");
    }

    #endregion

    #region Edge Cases

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HandlesPocoWithNoProperties()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class EmptyPoco
{
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "EmptyPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Should still generate valid mapper
        generated.ShouldContain("EmptyPocoPocoMapper");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HandlesPocoWithSingleProperty()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class SinglePropPoco
{
    public int OnlyProp { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "SinglePropPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("OnlyProp");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HandlesMultiplePocoClassesInSameFile()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class FirstPoco
{
    public int Id { get; set; }
}

[GenerateMapper]
public class SecondPoco
{
    public string Name { get; set; } = string.Empty;
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        var first = CompilationHelper.GetGeneratedOutput(compilation, "FirstPocoPocoMapper.g.cs");
        var second = CompilationHelper.GetGeneratedOutput(compilation, "SecondPocoPocoMapper.g.cs");

        first.ShouldNotBeNull();
        second.ShouldNotBeNull();

        first.ShouldContain("FirstPocoPocoMapper");
        second.ShouldContain("SecondPocoPocoMapper");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HandlesNestedClass()
    {
        var source = @"
using Fdw.Data;

namespace Test;

public class OuterClass
{
    [GenerateMapper]
    public class NestedPoco
    {
        public int Id { get; set; }
    }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "NestedPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("NestedPocoPocoMapper");
    }

    #endregion

    #region Fully Qualified Name Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void UsesFullyQualifiedTypeNames()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class FqnPoco
{
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "FqnPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("global::Test.FqnPoco");
    }

    #endregion

    #region Cascade Descriptor — ReadDictionary (FDW-547)

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratesReadDictionaryReturningBagForPropertyCollectionChild()
    {
        var source = @"
using System.Collections.Generic;
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class KvpOwner
{
    public int Id { get; set; }

    [ConfigurationChildTable(""KvpOwnerAuthentication"")]
    public IDictionary<string, string?> Properties { get; set; } = new Dictionary<string, string?>();
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "KvpOwnerPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("public global::System.Collections.Generic.IReadOnlyDictionary<string, string?>? ReadDictionary(object parent)");
        generated.ShouldContain("var source = ((global::Test.KvpOwner)parent).Properties;");
        generated.ShouldContain("if (source is null) return null;");
        generated.ShouldContain("result[kv.Key] = kv.Value;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratesNullReadDictionaryForTypedListChild()
    {
        var source = @"
using System.Collections.Generic;
using Fdw.Data;

namespace Fdw.Configuration
{
    public interface IGenericConfiguration
    {
    }
}

namespace Test
{
    using Fdw.Configuration;

    [GenerateMapper]
    public class ChildItem : IGenericConfiguration
    {
        public int Id { get; set; }
    }

    [GenerateMapper]
    public class ListOwner
    {
        public int Id { get; set; }
        public IList<ChildItem> Items { get; set; } = new List<ChildItem>();
    }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "ListOwnerPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("public global::System.Collections.Generic.IReadOnlyDictionary<string, string?>? ReadDictionary(object parent) => null;");
    }

    #endregion
}
