using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Data.SourceGenerators.Tests;

/// <summary>
/// Tests for [GenerateMapper(Direction = ...)] — the mapper direction support added in FDW-403.
/// </summary>
public class PocoMapperGeneratorDirectionTests
{
    #region Input direction (default)

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void DefaultDirection_EmitsInputMethods()
    {
        // Default direction (no Direction property specified) must preserve backward compat —
        // both MapFromReader and MapFromDictionary must be present.
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper]
public class InputDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
";
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "InputDtoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("MapFromReader");
        generated.ShouldContain("MapFromDictionary");
        generated.ShouldContain("MapToParameters");
        generated.ShouldContain("GetPropertyNames");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ExplicitInputDirection_EmitsInputMethods()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper(Direction = MapperDirection.Input)]
public class ExplicitInputDto
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}
";
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "ExplicitInputDtoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("MapFromReader");
        generated.ShouldContain("MapFromDictionary");
        generated.ShouldContain("MapToParameters");
        generated.ShouldContain("GetPropertyNames");
    }

    #endregion

    #region Output direction

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void OutputDirection_EmitsOutputMethodsOnly()
    {
        // Output-only mappers must NOT contain MapFromReader or MapFromDictionary.
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper(Direction = MapperDirection.Output)]
public class OutputDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
";
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "OutputDtoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldNotContain("MapFromReader");
        generated.ShouldNotContain("MapFromDictionary");
        generated.ShouldContain("MapToParameters");
        generated.ShouldContain("GetPropertyNames");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void OutputDirection_DoesNotEmitDbDataReaderUsing()
    {
        // Output-only mappers have no need for System.Data.Common — it should not appear.
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper(Direction = MapperDirection.Output)]
public class WriteOnlyDto
{
    public int Ordinal { get; set; }
    public string Key { get; set; } = string.Empty;
}
";
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "WriteOnlyDtoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldNotContain("System.Data.Common");
    }

    #endregion

    #region Both direction

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void BothDirection_EmitsAllMethods()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper(Direction = MapperDirection.Both)]
public class BidirectionalDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}
";
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "BidirectionalDtoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("MapFromReader");
        generated.ShouldContain("MapFromDictionary");
        generated.ShouldContain("MapToParameters");
        generated.ShouldContain("GetPropertyNames");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void BothDirection_EmitsDbDataReaderUsing()
    {
        // Both direction requires the data reader — System.Data.Common must appear.
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper(Direction = MapperDirection.Both)]
public class FullDto
{
    public int Id { get; set; }
}
";
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "FullDtoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        generated.ShouldContain("System.Data.Common");
    }

    #endregion

    #region Direction annotation in emitted comment

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void GeneratedMapper_IncludesDirectionInComment()
    {
        var source = @"
using Fdw.Data;

namespace Test;

[GenerateMapper(Direction = MapperDirection.Output)]
public class AnnotatedDto
{
    public string Value { get; set; } = string.Empty;
}
";
        var (compilation, _) = CompilationHelper.RunGenerator(source);

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "AnnotatedDtoPocoMapper.g.cs");

        generated.ShouldNotBeNull();
        // The generator embeds the direction in the XML doc comment for observability.
        generated.ShouldContain("Direction:");
    }

    #endregion
}
