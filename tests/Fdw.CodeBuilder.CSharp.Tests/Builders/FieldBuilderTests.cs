using Fdw.CodeBuilder.CSharp.Builders;

namespace Fdw.CodeBuilder.CSharp.Tests.Builders;

public class FieldBuilderTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_DefaultField_GeneratesBasicField()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder.Build();

        // Assert
        result.ShouldContain("private object field;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithName_SetsFieldName()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder.WithName("_myField").Build();

        // Assert
        result.ShouldContain("_myField");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithType_SetsFieldType()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder.WithType("string").Build();

        // Assert
        result.ShouldContain("string field;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithAccessModifier_SetsModifier()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder.WithAccessModifier("public").Build();

        // Assert
        result.ShouldContain("public object field;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsStatic_AddsStaticKeyword()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder.AsStatic().Build();

        // Assert
        result.ShouldContain("private static object field;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsReadOnly_AddsReadOnlyKeyword()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder.AsReadOnly().Build();

        // Assert
        result.ShouldContain("private readonly object field;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsConst_AddsConstKeyword()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder.AsConst().WithInitializer("null").Build();

        // Assert
        result.ShouldContain("private const object field = null;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsConst_WithoutInitializer_ThrowsException()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.AsConst().Build());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsConst_ClearsStatic()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder.AsStatic().AsConst().WithInitializer("null").Build();

        // Assert
        result.ShouldContain("const");
        result.ShouldNotContain("static");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsVolatile_AddsVolatileKeyword()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder.AsVolatile().Build();

        // Assert
        result.ShouldContain("private volatile object field;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsVolatile_ClearsConst()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder.AsConst().WithInitializer("null").AsVolatile().Build();

        // Assert
        result.ShouldContain("volatile");
        result.ShouldNotContain("const");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithInitializer_AddsInitializerValue()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder.WithInitializer("\"test\"").Build();

        // Assert
        result.ShouldContain("= \"test\";");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithAttribute_AddsAttribute()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder.WithAttribute("NonSerialized").Build();

        // Assert
        result.ShouldContain("[NonSerialized]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithXmlDoc_GeneratesXmlDocumentation()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder.WithXmlDoc("The field value.").Build();

        // Assert
        result.ShouldContain("/// <summary>");
        result.ShouldContain("/// The field value.");
        result.ShouldContain("/// </summary>");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithPreprocessorDirective_AddsDirective()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder.WithPreprocessorDirective("if !NET8_0_OR_GREATER").Build();

        // Assert
        result.ShouldContain("#if !NET8_0_OR_GREATER");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_FullField_GeneratesCompleteCode()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder
            .WithName("_count")
            .WithType("int")
            .WithAccessModifier("private")
            .AsReadOnly()
            .WithInitializer("0")
            .WithXmlDoc("The count value.")
            .WithAttribute("DebuggerBrowsable(DebuggerBrowsableState.Never)")
            .Build();

        // Assert
        result.ShouldContain("/// <summary>");
        result.ShouldContain("[DebuggerBrowsable(DebuggerBrowsableState.Never)]");
        result.ShouldContain("private readonly int _count = 0;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_StaticReadOnlyField_CombinesModifiers()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder
            .AsStatic()
            .AsReadOnly()
            .WithInitializer("new()")
            .Build();

        // Assert
        result.ShouldContain("private static readonly object field = new();");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsReadOnly_ClearsConst()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder
            .AsConst()
            .WithInitializer("null")
            .AsReadOnly()
            .Build();

        // Assert
        result.ShouldContain("readonly");
        result.ShouldNotContain("const");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_WithMultipleAttributes_AddsAll()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder
            .WithAttribute("NonSerialized")
            .WithAttribute("DebuggerBrowsable(DebuggerBrowsableState.Never)")
            .Build();

        // Assert
        result.ShouldContain("[NonSerialized]");
        result.ShouldContain("[DebuggerBrowsable(DebuggerBrowsableState.Never)]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithInitializer_WithEmptyString_ClearsInitializer()
    {
        // Arrange
        var builder = new FieldBuilder();

        // Act
        var result = builder
            .WithInitializer("value")
            .WithInitializer("")
            .Build();

        // Assert
        result.ShouldNotContain("=");
    }
}
