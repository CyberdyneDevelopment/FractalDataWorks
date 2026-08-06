using Fdw.CodeBuilder.CSharp.Parsing;
using Microsoft.CodeAnalysis.CSharp;

namespace Fdw.CodeBuilder.CSharp.Tests.Parsing;

public class RoslynSyntaxTreeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Constructor_SetsSourceText()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp");

        // Assert
        syntaxTree.SourceText.ShouldBe(code);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Constructor_SetsLanguage()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp");

        // Assert
        syntaxTree.Language.ShouldBe("csharp");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Constructor_WithFilePath_SetsFilePath()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var filePath = "Test.cs";

        // Act
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp", filePath);

        // Assert
        syntaxTree.FilePath.ShouldBe(filePath);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Root_ReturnsNonNull()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp");

        // Act
        var root = syntaxTree.Root;

        // Assert
        root.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasErrors_WithValidCode_ReturnsFalse()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp");

        // Act
        var hasErrors = syntaxTree.HasErrors;

        // Assert
        hasErrors.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasErrors_WithInvalidCode_ReturnsTrue()
    {
        // Arrange
        var code = "class Test { invalid }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp");

        // Act
        var hasErrors = syntaxTree.HasErrors;

        // Assert
        hasErrors.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetErrors_WithValidCode_ReturnsEmpty()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp");

        // Act
        var errors = syntaxTree.GetErrors().ToList();

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetErrors_WithInvalidCode_ReturnsErrors()
    {
        // Arrange
        var code = "class Test { invalid }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp");

        // Act
        var errors = syntaxTree.GetErrors().ToList();

        // Assert
        errors.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void FindNodes_WithClassDeclaration_FindsClass()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp");

        // Act
        var nodes = syntaxTree.FindNodes("ClassDeclaration").ToList();

        // Assert
        nodes.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void FindNodes_WithNonExistentType_ReturnsEmpty()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp");

        // Act
        var nodes = syntaxTree.FindNodes("InterfaceDeclaration").ToList();

        // Assert
        nodes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetNodeAtPosition_WithValidPosition_ReturnsNode()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp");

        // Act
        var node = syntaxTree.GetNodeAtPosition(0);

        // Assert
        node.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetNodeAtPosition_WithNegativePosition_ReturnsNull()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp");

        // Act
        var node = syntaxTree.GetNodeAtPosition(-1);

        // Assert
        node.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetNodeAtPosition_WithPositionBeyondEnd_ReturnsNull()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp");

        // Act
        var node = syntaxTree.GetNodeAtPosition(1000);

        // Assert
        node.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetNodeAtLocation_WithValidLocation_ReturnsNode()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp");

        // Act
        var node = syntaxTree.GetNodeAtLocation(0, 0);

        // Assert
        node.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Root_Text_ContainsOriginalCode()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp");

        // Act
        var text = syntaxTree.Root.Text;

        // Assert
        text.ShouldContain("Test");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Root_DescendantNodes_ReturnsAllNodes()
    {
        // Arrange
        var code = @"
            class Test
            {
                public int Property { get; set; }
            }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp");

        // Act
        var nodes = syntaxTree.Root.DescendantNodes().ToList();

        // Assert
        nodes.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetNodeAtPosition_FindsDeepestNode()
    {
        // Arrange
        var code = @"
            class Test
            {
                public int Property { get; set; }
            }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var syntaxTree = new RoslynSyntaxTree(tree, code, "csharp");

        // Find position of "Property"
        var position = code.IndexOf("Property");

        // Act
        var node = syntaxTree.GetNodeAtPosition(position);

        // Assert
        node.ShouldNotBeNull();
        node!.Text.ShouldContain("Property");
    }
}
