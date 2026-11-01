using Fdw.CodeBuilder.CSharp.Builders;

namespace Fdw.CodeBuilder.CSharp.Tests.Builders;

public class PropertyBuilderTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_DefaultProperty_GeneratesBasicProperty()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.Build();

        // Assert
        result.ShouldContain("public object Property");
        result.ShouldContain("{ get; set; }");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithName_SetsPropertyName()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.WithName("MyProperty").Build();

        // Assert
        result.ShouldContain("MyProperty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithType_SetsPropertyType()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.WithType("string").Build();

        // Assert
        result.ShouldContain("string Property");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithAccessModifier_SetsModifier()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.WithAccessModifier("private").Build();

        // Assert
        result.ShouldContain("private object Property");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsStatic_AddsStaticKeyword()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.AsStatic().Build();

        // Assert
        result.ShouldContain("public static object Property");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsVirtual_AddsVirtualKeyword()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.AsVirtual().Build();

        // Assert
        result.ShouldContain("public virtual object Property");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsOverride_AddsOverrideKeyword()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.AsOverride().Build();

        // Assert
        result.ShouldContain("public override object Property");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsOverride_ClearsVirtual()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.AsVirtual().AsOverride().Build();

        // Assert
        result.ShouldContain("override");
        result.ShouldNotContain("virtual");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsAbstract_AddsAbstractKeyword()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.AsAbstract().Build();

        // Assert
        result.ShouldContain("public abstract object Property");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsReadOnly_GeneratesGetterOnly()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.AsReadOnly().Build();

        // Assert
        result.ShouldContain("{ get; }");
        result.ShouldNotContain("set");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsWriteOnly_GeneratesSetterOnly()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.AsWriteOnly().Build();

        // Assert
        result.ShouldContain("{ set; }");
        result.ShouldNotContain("get");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithGetter_GeneratesCustomGetter()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.WithGetter("return _field;").Build();

        // Assert
        result.ShouldContain("get");
        result.ShouldContain("return _field;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithSetter_GeneratesCustomSetter()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.WithSetter("_field = value;").Build();

        // Assert
        result.ShouldContain("set");
        result.ShouldContain("_field = value;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithGetterAccessModifier_SetsGetterModifier()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.WithGetterAccessModifier("private").Build();

        // Assert
        result.ShouldContain("private get;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithSetterAccessModifier_SetsSetterModifier()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.WithSetterAccessModifier("private").Build();

        // Assert
        result.ShouldContain("private set;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithInitializer_AddsInitializer()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.WithInitializer("\"test\"").Build();

        // Assert
        result.ShouldContain("= \"test\";");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithInitSetter_GeneratesInitAccessor()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.WithInitSetter().Build();

        // Assert
        result.ShouldContain("init;");
        result.ShouldNotContain("set;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithAttribute_AddsAttribute()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.WithAttribute("JsonProperty(\"name\")").Build();

        // Assert
        result.ShouldContain("[JsonProperty(\"name\")]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithExpressionBody_GeneratesExpressionBodiedProperty()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.WithExpressionBody("_field").Build();

        // Assert
        result.ShouldContain("=> _field;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithXmlDoc_GeneratesXmlDocumentation()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder.WithXmlDoc("Gets or sets the value.").Build();

        // Assert
        result.ShouldContain("/// <summary>");
        result.ShouldContain("/// Gets or sets the value.");
        result.ShouldContain("/// </summary>");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_FullProperty_GeneratesCompleteCode()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder
            .WithName("Name")
            .WithType("string")
            .WithAccessModifier("public")
            .WithXmlDoc("Gets or sets the name.")
            .WithAttribute("Required")
            .WithSetterAccessModifier("private")
            .Build();

        // Assert
        result.ShouldContain("/// <summary>");
        result.ShouldContain("[Required]");
        result.ShouldContain("public string Name");
        result.ShouldContain("get;");
        result.ShouldContain("private set;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_AbstractProperty_OmitsInitializer()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder
            .AsAbstract()
            .WithInitializer("\"test\"")
            .Build();

        // Assert
        result.ShouldNotContain("=");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_WithBlockAccessors_GeneratesBlockStyle()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder
            .WithGetter("return _field;")
            .WithSetter("_field = value;")
            .Build();

        // Assert
        result.ShouldContain("{");
        result.ShouldContain("get");
        result.ShouldContain("return _field;");
        result.ShouldContain("set");
        result.ShouldContain("_field = value;");
        result.ShouldContain("}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithSetter_ClearsInitSetter()
    {
        // Arrange
        var builder = new PropertyBuilder();

        // Act
        var result = builder
            .WithInitSetter()
            .WithSetter("_field = value;")
            .Build();

        // Assert
        result.ShouldNotContain("init");
        result.ShouldContain("set");
    }
}
