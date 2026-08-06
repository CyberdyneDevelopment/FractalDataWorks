using Fdw.CodeBuilder.CSharp.Generators;
using Fdw.CodeBuilder.CSharp.Builders;
using Fdw.CodeBuilder.CSharp.Parsing;
using Microsoft.CodeAnalysis.CSharp;

namespace Fdw.CodeBuilder.CSharp.Tests.Generators;

public class CSharpCodeGeneratorTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void TargetLanguage_ReturnsCSharp()
    {
        // Arrange
        var generator = new CSharpCodeGenerator();

        // Act
        var language = generator.TargetLanguage;

        // Assert
        language.ShouldBe("csharp");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Generate_WithSyntaxTree_ReturnsSourceText()
    {
        // Arrange
        var generator = new CSharpCodeGenerator();
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp");

        // Act
        var result = generator.Generate(syntaxTree);

        // Assert
        result.ShouldBe(code);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Generate_WithClassBuilder_ReturnsGeneratedCode()
    {
        // Arrange
        var generator = new CSharpCodeGenerator();
        var builder = new ClassBuilder()
            .WithName("Test")
            .WithAccessModifier("public");

        // Act
        var result = generator.Generate(builder);

        // Assert
        result.ShouldContain("public class Test");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Generate_WithInterfaceBuilder_ReturnsGeneratedCode()
    {
        // Arrange
        var generator = new CSharpCodeGenerator();
        var mockInterfaceBuilder = new Mock<Fdw.CodeBuilder.Abstractions.IInterfaceBuilder>();
        mockInterfaceBuilder.Setup(b => b.Build()).Returns("public interface ITest { }");

        // Act
        var result = generator.Generate(mockInterfaceBuilder.Object);

        // Assert
        result.ShouldBe("public interface ITest { }");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Generate_WithEnumBuilder_ReturnsGeneratedCode()
    {
        // Arrange
        var generator = new CSharpCodeGenerator();
        var mockEnumBuilder = new Mock<Fdw.CodeBuilder.Abstractions.IEnumBuilder>();
        mockEnumBuilder.Setup(b => b.Build()).Returns("public enum Status { Active, Inactive }");

        // Act
        var result = generator.Generate(mockEnumBuilder.Object);

        // Assert
        result.ShouldBe("public enum Status { Active, Inactive }");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateCompilationUnit_WithSingleBuilder_ReturnsCode()
    {
        // Arrange
        var generator = new CSharpCodeGenerator();
        var builder = new ClassBuilder()
            .WithName("Test")
            .WithAccessModifier("public");
        var builders = new[] { builder };

        // Act
        var result = generator.GenerateCompilationUnit(builders);

        // Assert
        result.ShouldContain("public class Test");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateCompilationUnit_WithMultipleBuilders_SeparatesWithBlankLines()
    {
        // Arrange
        var generator = new CSharpCodeGenerator();
        var builder1 = new ClassBuilder().WithName("Test1");
        var builder2 = new ClassBuilder().WithName("Test2");
        var builders = new[] { builder1, builder2 };

        // Act
        var result = generator.GenerateCompilationUnit(builders);

        // Assert
        result.ShouldContain("Test1");
        result.ShouldContain("Test2");
        var lines = result.Split(Environment.NewLine);
        lines.ShouldContain("");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateCompilationUnit_WithEmptyList_ReturnsEmptyString()
    {
        // Arrange
        var generator = new CSharpCodeGenerator();
        var builders = Array.Empty<Fdw.CodeBuilder.Abstractions.ICodeBuilder>();

        // Act
        var result = generator.GenerateCompilationUnit(builders);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Generate_WithComplexClassBuilder_GeneratesFullCode()
    {
        // Arrange
        var generator = new CSharpCodeGenerator();
        var builder = new ClassBuilder()
            .WithNamespace("MyApp")
            .WithName("Person")
            .WithAccessModifier("public")
            .WithProperty(new PropertyBuilder()
                .WithName("Name")
                .WithType("string")
                .WithAccessModifier("public"));

        // Act
        var result = generator.Generate(builder);

        // Assert
        result.ShouldContain("namespace MyApp;");
        result.ShouldContain("public class Person");
        result.ShouldContain("public string Name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateCompilationUnit_PreservesOrder()
    {
        // Arrange
        var generator = new CSharpCodeGenerator();
        var builder1 = new ClassBuilder().WithName("First");
        var builder2 = new ClassBuilder().WithName("Second");
        var builder3 = new ClassBuilder().WithName("Third");
        var builders = new[] { builder1, builder2, builder3 };

        // Act
        var result = generator.GenerateCompilationUnit(builders);

        // Assert
        var firstIndex = result.IndexOf("First");
        var secondIndex = result.IndexOf("Second");
        var thirdIndex = result.IndexOf("Third");

        firstIndex.ShouldBeLessThan(secondIndex);
        secondIndex.ShouldBeLessThan(thirdIndex);
    }
}
