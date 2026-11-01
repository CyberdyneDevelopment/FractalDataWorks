using Fdw.CodeBuilder.CSharp.Builders;

namespace Fdw.CodeBuilder.CSharp.Tests.Builders;

public class MethodBuilderTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_DefaultMethod_GeneratesBasicMethod()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder.Build();

        // Assert
        result.ShouldContain("public void Method()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithName_SetsMethodName()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder.WithName("Execute").Build();

        // Assert
        result.ShouldContain("Execute()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithReturnType_SetsReturnType()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder.WithReturnType("string").Build();

        // Assert
        result.ShouldContain("string Method()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithAccessModifier_SetsModifier()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder.WithAccessModifier("private").Build();

        // Assert
        result.ShouldContain("private void Method()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsStatic_AddsStaticKeyword()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder.AsStatic().Build();

        // Assert
        result.ShouldContain("public static void Method()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsVirtual_AddsVirtualKeyword()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder.AsVirtual().Build();

        // Assert
        result.ShouldContain("public virtual void Method()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsOverride_AddsOverrideKeyword()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder.AsOverride().Build();

        // Assert
        result.ShouldContain("public override void Method()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsAbstract_AddsAbstractKeyword()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder.AsAbstract().Build();

        // Assert
        result.ShouldContain("public abstract void Method();");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsAsync_AddsAsyncKeyword()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder.As().Build();

        // Assert
        result.ShouldContain("public async void Method()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithParameter_AddsParameter()
    {
        // Arrange
        var builder = new MethodBuilder();

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
        var builder = new MethodBuilder();

        // Act
        var result = builder.WithParameter("int", "count", "0").Build();

        // Assert
        result.ShouldContain("(int count = 0)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithParameter_Multiple_AddsAllParameters()
    {
        // Arrange
        var builder = new MethodBuilder();

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
    public void WithGenericParameters_AddsTypeParameters()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder.WithGenericParameters("T").Build();

        // Assert
        result.ShouldContain("Method<T>()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithGenericParameters_Multiple_AddsAllParameters()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder.WithGenericParameters("T", "U").Build();

        // Assert
        result.ShouldContain("Method<T, U>()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithGenericConstraint_AddsConstraint()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder
            .WithGenericParameters("T")
            .WithGenericConstraint("T", "class")
            .Build();

        // Assert
        result.ShouldContain("where T : class");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithGenericConstraint_MultipleConstraints_AddsAll()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder
            .WithGenericParameters("T")
            .WithGenericConstraint("T", "class", "new()")
            .Build();

        // Assert
        result.ShouldContain("where T : class, new()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithAttribute_AddsAttribute()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder.WithAttribute("Obsolete").Build();

        // Assert
        result.ShouldContain("[Obsolete]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithBody_SetsMethodBody()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder.WithBody("return 42;").Build();

        // Assert
        result.ShouldContain("return 42;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AddBodyLine_AddsLineToBody()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder
            .AddBodyLine("var x = 5;")
            .AddBodyLine("return x;")
            .Build();

        // Assert
        result.ShouldContain("var x = 5;");
        result.ShouldContain("return x;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithExpressionBody_GeneratesExpressionBodiedMethod()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder.WithExpressionBody("42").Build();

        // Assert
        result.ShouldContain("=> 42;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithXmlDoc_GeneratesXmlDocumentation()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder.WithXmlDoc("Executes the method.").Build();

        // Assert
        result.ShouldContain("/// <summary>");
        result.ShouldContain("/// Executes the method.");
        result.ShouldContain("/// </summary>");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithParamDoc_AddsParameterDocumentation()
    {
        // Arrange
        var builder = new MethodBuilder();

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
    public void WithReturnDoc_AddsReturnDocumentation()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder
            .WithReturnType("int")
            .WithReturnDoc("The result value")
            .Build();

        // Assert
        result.ShouldContain("/// <returns>The result value</returns>");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_FullMethod_GeneratesCompleteCode()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder
            .WithName("Calculate")
            .WithReturnType("int")
            .WithAccessModifier("public")
            .WithParameter("int", "x")
            .WithParameter("int", "y")
            .WithXmlDoc("Calculates the sum.")
            .WithParamDoc("x", "First value")
            .WithParamDoc("y", "Second value")
            .WithReturnDoc("The sum")
            .WithAttribute("Pure")
            .AddBodyLine("return x + y;")
            .Build();

        // Assert
        result.ShouldContain("/// <summary>");
        result.ShouldContain("[Pure]");
        result.ShouldContain("public int Calculate(int x, int y)");
        result.ShouldContain("return x + y;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_AbstractMethod_EndsWithSemicolon()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder.AsAbstract().Build();

        // Assert
        result.ShouldContain(";");
        result.ShouldNotContain("{");
        result.ShouldNotContain("}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithBody_ClearsExpressionBody()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder
            .WithExpressionBody("42")
            .WithBody("return 42;")
            .Build();

        // Assert
        result.ShouldNotContain("=>");
        result.ShouldContain("{");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithExpressionBody_ClearsBody()
    {
        // Arrange
        var builder = new MethodBuilder();

        // Act
        var result = builder
            .WithBody("return 42;")
            .WithExpressionBody("42")
            .Build();

        // Assert
        result.ShouldContain("=>");
    }
}
