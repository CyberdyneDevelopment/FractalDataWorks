using Fdw.CodeBuilder.Analysis.CSharp.Expectations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.CodeBuilder.Analysis.CSharp.Tests.Expectations;

public class FieldExpectationsTests
{
    private FieldDeclarationSyntax ParseField(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();
        return root.DescendantNodes().OfType<FieldDeclarationSyntax>().First();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Constructor_WithNull_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() => new FieldExpectations(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsPrivate_WithPrivateField_DoesNotThrow()
    {
        // Arrange
        var field = ParseField("class Test { private int _field; }");
        var expectations = new FieldExpectations(field);

        // Act & Assert
        Should.NotThrow(() => expectations.IsPrivate());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsPrivate_WithPublicField_Throws()
    {
        // Arrange
        var field = ParseField("class Test { public int _field; }");
        var expectations = new FieldExpectations(field);

        // Act & Assert
        Should.Throw<Shouldly.ShouldAssertException>(() => expectations.IsPrivate());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsPublic_WithPublicField_DoesNotThrow()
    {
        // Arrange
        var field = ParseField("class Test { public int field; }");
        var expectations = new FieldExpectations(field);

        // Act & Assert
        Should.NotThrow(() => expectations.IsPublic());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsReadOnly_WithReadOnlyField_DoesNotThrow()
    {
        // Arrange
        var field = ParseField("class Test { private readonly int _field; }");
        var expectations = new FieldExpectations(field);

        // Act & Assert
        Should.NotThrow(() => expectations.IsReadOnly());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsStatic_WithStaticField_DoesNotThrow()
    {
        // Arrange
        var field = ParseField("class Test { private static int _field; }");
        var expectations = new FieldExpectations(field);

        // Act & Assert
        Should.NotThrow(() => expectations.IsStatic());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasType_WithCorrectType_DoesNotThrow()
    {
        // Arrange
        var field = ParseField("class Test { private string _field; }");
        var expectations = new FieldExpectations(field);

        // Act & Assert
        Should.NotThrow(() => expectations.HasType("string"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasType_WithIncorrectType_Throws()
    {
        // Arrange
        var field = ParseField("class Test { private string _field; }");
        var expectations = new FieldExpectations(field);

        // Act & Assert
        Should.Throw<Shouldly.ShouldAssertException>(() => expectations.HasType("int"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasInitializer_WithInitializer_DoesNotThrow()
    {
        // Arrange
        var field = ParseField("class Test { private int _field = 42; }");
        var expectations = new FieldExpectations(field);

        // Act & Assert
        Should.NotThrow(() => expectations.HasInitializer());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasInitializer_WithoutInitializer_Throws()
    {
        // Arrange
        var field = ParseField("class Test { private int _field; }");
        var expectations = new FieldExpectations(field);

        // Act & Assert
        Should.Throw<Shouldly.ShouldAssertException>(() => expectations.HasInitializer());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasInitializer_WithSpecificValue_DoesNotThrow()
    {
        // Arrange
        var field = ParseField("class Test { private int _field = 42; }");
        var expectations = new FieldExpectations(field);

        // Act & Assert
        Should.NotThrow(() => expectations.HasInitializer("42"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasNoInitializer_WithoutInitializer_DoesNotThrow()
    {
        // Arrange
        var field = ParseField("class Test { private int _field; }");
        var expectations = new FieldExpectations(field);

        // Act & Assert
        Should.NotThrow(() => expectations.HasNoInitializer());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasName_WithCorrectName_DoesNotThrow()
    {
        // Arrange
        var field = ParseField("class Test { private int _myField; }");
        var expectations = new FieldExpectations(field);

        // Act & Assert
        Should.NotThrow(() => expectations.HasName("_myField"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasModifiers_WithCorrectModifiers_DoesNotThrow()
    {
        // Arrange
        var field = ParseField("class Test { private static readonly int _field = 0; }");
        var expectations = new FieldExpectations(field);

        // Act & Assert
        Should.NotThrow(() => expectations.HasModifiers(
            Microsoft.CodeAnalysis.CSharp.SyntaxKind.PrivateKeyword,
            Microsoft.CodeAnalysis.CSharp.SyntaxKind.StaticKeyword,
            Microsoft.CodeAnalysis.CSharp.SyntaxKind.ReadOnlyKeyword));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void FluentAPI_ChainsCorrectly()
    {
        // Arrange
        var field = ParseField("class Test { private readonly string _name = \"test\"; }");
        var expectations = new FieldExpectations(field);

        // Act & Assert
        Should.NotThrow(() => expectations
            .IsPrivate()
            .IsReadOnly()
            .HasType("string")
            .HasName("_name")
            .HasInitializer("\"test\""));
    }
}
