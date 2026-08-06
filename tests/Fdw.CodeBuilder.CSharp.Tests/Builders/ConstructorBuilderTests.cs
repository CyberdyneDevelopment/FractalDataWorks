using Fdw.CodeBuilder.CSharp.Builders;

namespace Fdw.CodeBuilder.CSharp.Tests.Builders;

public class ConstructorBuilderTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_DefaultConstructor_GeneratesBasicConstructor()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder.Build();

        // Assert
        result.ShouldContain("public MyClass()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithClassName_SetsConstructorName()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder.WithClassName("Person").Build();

        // Assert
        result.ShouldContain("Person()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithAccessModifier_SetsModifier()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder.WithAccessModifier("private").Build();

        // Assert
        result.ShouldContain("private MyClass()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsStatic_GeneratesStaticConstructor()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder.AsStatic().Build();

        // Assert
        result.ShouldContain("static MyClass()");
        result.ShouldNotContain("public");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithParameter_AddsParameter()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder.WithParameter("string", "name").Build();

        // Assert
        result.ShouldContain("(string name)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithParameter_WithDefault_AddsDefaultValue()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder.WithParameter("int", "age", "0").Build();

        // Assert
        result.ShouldContain("(int age = 0)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithParameter_Multiple_AddsAllParameters()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder
            .WithParameter("string", "name")
            .WithParameter("int", "age")
            .Build();

        // Assert
        result.ShouldContain("(string name, int age)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithBaseCall_AddsBaseConstructorCall()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder.WithBaseCall("name", "age").Build();

        // Assert
        result.ShouldContain(": base(name, age)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithThisCall_AddsThisConstructorCall()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder.WithThisCall("\"default\"").Build();

        // Assert
        result.ShouldContain(": this(\"default\")");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithBaseCall_ClearsThisCall()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder
            .WithThisCall("value")
            .WithBaseCall("value")
            .Build();

        // Assert
        result.ShouldContain(": base(value)");
        result.ShouldNotContain(": this");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithThisCall_ClearsBaseCall()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder
            .WithBaseCall("value")
            .WithThisCall("value")
            .Build();

        // Assert
        result.ShouldContain(": this(value)");
        result.ShouldNotContain(": base");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithAttribute_AddsAttribute()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder.WithAttribute("Obsolete").Build();

        // Assert
        result.ShouldContain("[Obsolete]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithBody_SetsConstructorBody()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder.WithBody("_name = name;").Build();

        // Assert
        result.ShouldContain("_name = name;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AddBodyLine_AddsLineToBody()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder
            .AddBodyLine("_name = name;")
            .AddBodyLine("_age = age;")
            .Build();

        // Assert
        result.ShouldContain("_name = name;");
        result.ShouldContain("_age = age;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithXmlDoc_GeneratesXmlDocumentation()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder.WithXmlDoc("Initializes a new instance.").Build();

        // Assert
        result.ShouldContain("/// <summary>");
        result.ShouldContain("/// Initializes a new instance.");
        result.ShouldContain("/// </summary>");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithParamDoc_AddsParameterDocumentation()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder
            .WithParameter("string", "name")
            .WithParamDoc("name", "The name value")
            .Build();

        // Assert
        result.ShouldContain("/// <param name=\"name\">The name value</param>");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_FullConstructor_GeneratesCompleteCode()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder
            .WithClassName("Person")
            .WithAccessModifier("public")
            .WithParameter("string", "name")
            .WithParameter("int", "age")
            .WithXmlDoc("Creates a new person.")
            .WithParamDoc("name", "The person's name")
            .WithParamDoc("age", "The person's age")
            .AddBodyLine("_name = name;")
            .AddBodyLine("_age = age;")
            .Build();

        // Assert
        result.ShouldContain("/// <summary>");
        result.ShouldContain("public Person(string name, int age)");
        result.ShouldContain("_name = name;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_WithBaseAndBody_GeneratesCorrectFormat()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder
            .WithClassName("Employee")
            .WithParameter("string", "name")
            .WithParameter("int", "age")
            .WithParameter("string", "department")
            .WithBaseCall("name", "age")
            .AddBodyLine("_department = department;")
            .Build();

        // Assert
        result.ShouldContain("Employee(string name, int age, string department) : base(name, age)");
        result.ShouldContain("_department = department;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_EmptyBody_GeneratesEmptyBlock()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder.Build();

        // Assert
        result.ShouldContain("{");
        result.ShouldContain("}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithBody_ClearsPreviousBody()
    {
        // Arrange
        var builder = new ConstructorBuilder();

        // Act
        var result = builder
            .AddBodyLine("old line;")
            .WithBody("new line;")
            .Build();

        // Assert
        result.ShouldContain("new line;");
        result.ShouldNotContain("old line;");
    }
}
