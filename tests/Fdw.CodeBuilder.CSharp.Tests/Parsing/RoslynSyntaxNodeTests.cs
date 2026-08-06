using Fdw.CodeBuilder.CSharp.Parsing;
using Microsoft.CodeAnalysis.CSharp;

namespace Fdw.CodeBuilder.CSharp.Tests.Parsing;

public class RoslynSyntaxNodeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void NodeType_ReturnsCorrectType()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);
        var classNode = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().First();

        // Act
        var node = new RoslynSyntaxNode(classNode, code);

        // Assert
        node.NodeType.ShouldBe("ClassDeclaration");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Text_ReturnsNodeText()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);
        var classNode = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().First();

        // Act
        var node = new RoslynSyntaxNode(classNode, code);

        // Assert
        node.Text.ShouldContain("Test");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void StartPosition_ReturnsCorrectPosition()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);

        // Act
        var node = new RoslynSyntaxNode(root, code);

        // Assert
        node.StartPosition.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void EndPosition_IsGreaterThanStartPosition()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);

        // Act
        var node = new RoslynSyntaxNode(root, code);

        // Assert
        node.EndPosition.ShouldBeGreaterThan(node.StartPosition);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void StartLine_ReturnsValidLineNumber()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);

        // Act
        var node = new RoslynSyntaxNode(root, code);

        // Assert
        node.StartLine.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void StartColumn_ReturnsValidColumnNumber()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);

        // Act
        var node = new RoslynSyntaxNode(root, code);

        // Assert
        node.StartColumn.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Children_ReturnsChildNodes()
    {
        // Arrange
        var code = "class Test { public int Property { get; set; } }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);
        var classNode = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().First();

        // Act
        var node = new RoslynSyntaxNode(classNode, code);

        // Assert
        node.Children.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Children_CalledMultipleTimes_ReturnsSameInstance()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);
        var node = new RoslynSyntaxNode(root, code);

        // Act
        var children1 = node.Children;
        var children2 = node.Children;

        // Assert
        children1.ShouldBe(children2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Parent_WithParentNode_ReturnsParent()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);
        var parentNode = new RoslynSyntaxNode(root, code);
        var childSyntax = root.ChildNodes().FirstOrDefault();

        // Act
        var childNode = childSyntax != null ? new RoslynSyntaxNode(childSyntax, code, parentNode) : null;

        // Assert
        childNode?.Parent.ShouldBe(parentNode);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Parent_WithoutParentNode_ReturnsNull()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);

        // Act
        var node = new RoslynSyntaxNode(root, code);

        // Assert
        node.Parent.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsTerminal_WithNoChildren_ReturnsTrue()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);
        var terminalNode = root.DescendantNodes().FirstOrDefault(n => !n.ChildNodes().Any());

        // Act
        var node = terminalNode != null ? new RoslynSyntaxNode(terminalNode, code) : null;

        // Assert
        node?.IsTerminal.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsTerminal_WithChildren_ReturnsFalse()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);

        // Act
        var node = new RoslynSyntaxNode(root, code);

        // Assert
        node.IsTerminal.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsError_WithValidCode_ReturnsFalse()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);

        // Act
        var node = new RoslynSyntaxNode(root, code);

        // Assert
        node.IsError.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void FindChild_WithExistingType_ReturnsChild()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);
        var node = new RoslynSyntaxNode(root, code);

        // Act
        var child = node.FindChild("ClassDeclaration");

        // Assert
        child.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void FindChild_WithNonExistentType_ReturnsNull()
    {
        // Arrange
        var code = "class Test { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);
        var node = new RoslynSyntaxNode(root, code);

        // Act
        var child = node.FindChild("InterfaceDeclaration");

        // Assert
        child.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void FindChildren_WithMatchingType_ReturnsMatches()
    {
        // Arrange
        var code = @"
            class Test1 { }
            class Test2 { }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);
        var node = new RoslynSyntaxNode(root, code);

        // Act
        var children = node.FindChildren("ClassDeclaration").ToList();

        // Assert
        children.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void DescendantNodes_ReturnsAllDescendants()
    {
        // Arrange
        var code = "class Test { public int Property { get; set; } }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);
        var node = new RoslynSyntaxNode(root, code);

        // Act
        var descendants = node.DescendantNodes().ToList();

        // Assert
        descendants.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void DescendantNodes_IncludesNestedNodes()
    {
        // Arrange
        var code = @"
            class Test
            {
                class Nested { }
            }";
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        var root = tree.GetRoot(TestContext.Current.CancellationToken);
        var node = new RoslynSyntaxNode(root, code);

        // Act
        var descendants = node.DescendantNodes().ToList();

        // Assert
        descendants.Count.ShouldBeGreaterThanOrEqualTo(2);
    }
}
