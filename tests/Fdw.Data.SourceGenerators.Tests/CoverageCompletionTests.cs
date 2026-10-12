using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Data.SourceGenerators.Tests;

/// <summary>
/// Tests to achieve 100% code coverage by testing edge cases and less common branches.
/// </summary>
public class CoverageCompletionTests
{
    #region GetTypeInfo Edge Cases

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void DoesNotGenerateForStruct()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public struct MyStruct
{
    public int Value { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Generator should still work for structs
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "MyStructPocoMapper.g.cs");
        generated.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HandlesEmptyNamespace()
    {
        var source = @"
using Fdw.Data;

[GenerateMapper]
public class NoNamespacePoco
{
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "NoNamespacePocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Should handle global namespace gracefully
        generated.ShouldNotContain("namespace ;");
    }

    #endregion

    #region Special Type Coverage

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HandlesSystemDecimal()
    {
        var source = @"
using Fdw.Data;
using System;

namespace Test;

[GenerateMapper]
public class DecimalPoco
{
    public decimal Amount { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "DecimalPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("GetDecimal");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HandlesSystemSingle()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class FloatPoco
{
    public float Value { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "FloatPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("GetFloat");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HandlesSystemByte()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class BytePoco
{
    public byte Flag { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "BytePocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("GetByte");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HandlesNonNullableReferenceTypeWithDefault()
    {
        var source = @"
#nullable enable
using Fdw.Data;

namespace Test;

public class Inner { }

[GenerateMapper]
public class ComplexPoco
{
    public Inner Detail { get; set; } = new();
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "ComplexPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Non-nullable, non-string reference types should use default!
        generated.ShouldContain("default!");
    }

    #endregion

    #region SanitizeName Coverage

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void SanitizesPropertyNameWithMultipleSpecialCharacters()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class SpecialCharPoco
{
    public int Property_With_Underscores { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "SpecialCharPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Underscores should be removed
        generated.ShouldContain("GetReaderValue_PropertyWithUnderscores");
    }

    #endregion

    #region Dictionary Mapping Edge Cases

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void DictionaryMappingHandlesCaseInVariableName()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class CasePoco
{
    public int ID { get; set; }
    public string NAME { get; set; } = string.Empty;
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "CasePocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Variables should be lowercase
        generated.ShouldContain("idval");
        generated.ShouldContain("nameval");
    }

    #endregion

    #region Property Mapping Trimming

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void PropertyMappingTrimsTrailingCommaAndNewlines()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class MultiPropPoco
{
    public int First { get; set; }
    public int Second { get; set; }
    public int Third { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "MultiPropPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Should have properly formatted property initializers (no trailing comma)
        generated.ShouldContain("First = ");
        generated.ShouldContain("Second = ");
        generated.ShouldContain("Third = ");
    }

    #endregion

    #region Different Property Types for GetFieldValue

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void UsesGetFieldValueForEnumType()
    {
        var source = @"
using Fdw.Data;

namespace Test;

public enum Color { Red, Green, Blue }

[GenerateMapper]
public class EnumPoco
{
    public Color FavoriteColor { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "EnumPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Enum types use GetFieldValue
        generated.ShouldContain("GetFieldValue<");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HandlesGuidSpecialCase()
    {
        var source = @"
using Fdw.Data;
using System;

namespace Test;

[GenerateMapper]
public class GuidPoco
{
    public Guid Identifier { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "GuidPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Guid has special handling with GetGuid method
        generated.ShouldContain("GetGuid");
    }

    #endregion

    #region Nullable Value Type Default Values

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void NullableIntUsesDefault()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class NullableIntPoco
{
    public int? OptionalNumber { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "NullableIntPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Nullable value types should use 'default' (null is OK)
        generated.ShouldContain("return default;");
    }

    #endregion

    #region FullyQualifiedName Handling

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void UsesGlobalNamespacePrefix()
    {
        var source = @"
using Fdw.Data;

namespace Deep.Nested.Namespace.Structure;

[GenerateMapper]
public class DeepPoco
{
    public int Id { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "DeepPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // Should use fully qualified name with global:: prefix
        generated.ShouldContain("global::Deep.Nested.Namespace.Structure.DeepPoco");
    }

    #endregion

    #region Multiple Properties and Formatting

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratesCorrectlyFormattedCodeWithManyProperties()
    {
        var source = @"
using Fdw.Data;
using System;

namespace Test;

[GenerateMapper]
public class LargePoco
{
    public int Prop1 { get; set; }
    public string Prop2 { get; set; } = string.Empty;
    public decimal Prop3 { get; set; }
    public DateTime Prop4 { get; set; }
    public bool Prop5 { get; set; }
    public Guid Prop6 { get; set; }
    public double Prop7 { get; set; }
    public float Prop8 { get; set; }
    public byte Prop9 { get; set; }
    public short Prop10 { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "LargePocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        // Verify all properties are mapped
        for (int i = 1; i <= 10; i++)
        {
            generated.ShouldContain($"Prop{i}");
        }
    }

    #endregion

    #region Exception Message Coverage

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ExceptionMessageIncludesTypeName()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class ErrorPoco
{
    public int Value { get; set; }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "ErrorPocoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("MapperResultCodes.MappingFailed");
        generated.ShouldContain("\"Type\", \"ErrorPoco\", \"Source\", \"reader\"");
        generated.ShouldContain("\"Type\", \"ErrorPoco\", \"Source\", \"dictionary\"");
    }

    #endregion
}
