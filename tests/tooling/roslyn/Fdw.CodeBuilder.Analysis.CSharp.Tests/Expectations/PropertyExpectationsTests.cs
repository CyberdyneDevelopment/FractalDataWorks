using Fdw.CodeBuilder.Analysis.CSharp.Expectations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.CodeBuilder.Analysis.CSharp.Tests.Expectations;

public class PropertyExpectationsTests
{
    private static PropertyDeclarationSyntax ParseProperty(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();
        return root.DescendantNodes().OfType<PropertyDeclarationSyntax>().First();
    }

    private static PropertyDeclarationSyntax ParseProperty(string code, int index)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();
        return root.DescendantNodes().OfType<PropertyDeclarationSyntax>().ElementAt(index);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Constructor_WithNull_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() => new PropertyExpectations(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasType_WithCorrectType_DoesNotThrow()
    {
        // Arrange
        var property = ParseProperty("class Test { public string Name { get; set; } }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.NotThrow(() => expectations.HasType("string"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasType_WithIncorrectType_Throws()
    {
        // Arrange
        var property = ParseProperty("class Test { public string Name { get; set; } }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.Throw<Shouldly.ShouldAssertException>(() => expectations.HasType("int"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasGetter_WithGetter_DoesNotThrow()
    {
        // Arrange
        var property = ParseProperty("class Test { public string Name { get; set; } }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.NotThrow(() => expectations.HasGetter());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasGetter_WithExpressionBodied_DoesNotThrow()
    {
        // Arrange
        var property = ParseProperty("class Test { public string Name => \"test\"; }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.NotThrow(() => expectations.HasGetter());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasSetter_WithSetter_DoesNotThrow()
    {
        // Arrange
        var property = ParseProperty("class Test { public string Name { get; set; } }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.NotThrow(() => expectations.HasSetter());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasSetter_WithoutSetter_Throws()
    {
        // Arrange
        var property = ParseProperty("class Test { public string Name { get; } }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.Throw<Shouldly.ShouldAssertException>(() => expectations.HasSetter());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasNoSetter_WithoutSetter_DoesNotThrow()
    {
        // Arrange
        var property = ParseProperty("class Test { public string Name { get; } }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.NotThrow(() => expectations.HasNoSetter());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsReadOnly_WithoutSetter_DoesNotThrow()
    {
        // Arrange
        var property = ParseProperty("class Test { public string Name { get; } }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.NotThrow(() => expectations.IsReadOnly());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasPrivateSetter_WithPrivateSetter_DoesNotThrow()
    {
        // Arrange
        var property = ParseProperty("class Test { public string Name { get; private set; } }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.NotThrow(() => expectations.HasPrivateSetter());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasInitSetter_WithInitSetter_DoesNotThrow()
    {
        // Arrange
        var property = ParseProperty("class Test { public string Name { get; init; } }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.NotThrow(() => expectations.HasInitSetter());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsPublic_WithPublicProperty_DoesNotThrow()
    {
        // Arrange
        var property = ParseProperty("class Test { public string Name { get; set; } }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.NotThrow(() => expectations.IsPublic());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsStatic_WithStaticProperty_DoesNotThrow()
    {
        // Arrange
        var property = ParseProperty("class Test { public static string Name { get; set; } }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.NotThrow(() => expectations.IsStatic());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsVirtual_WithVirtualProperty_DoesNotThrow()
    {
        // Arrange
        var property = ParseProperty("class Test { public virtual string Name { get; set; } }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.NotThrow(() => expectations.IsVirtual());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsOverride_WithOverrideProperty_DoesNotThrow()
    {
        // Arrange
        var fullCode = @"
            class Base { public virtual string Name { get; set; } }
            class Test : Base { public override string Name { get; set; } }";
        var property = ParseProperty(fullCode, 1); // Get the second property (from Test class, not Base)

        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.NotThrow(() => expectations.IsOverride());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsAbstract_WithAbstractProperty_DoesNotThrow()
    {
        // Arrange
        var property = ParseProperty("abstract class Test { public abstract string Name { get; set; } }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.NotThrow(() => expectations.IsAbstract());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsAutoProperty_WithAutoProperty_DoesNotThrow()
    {
        // Arrange
        var property = ParseProperty("class Test { public string Name { get; set; } }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.NotThrow(() => expectations.IsAutoProperty());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsAutoProperty_WithCustomGetter_Throws()
    {
        // Arrange
        var property = ParseProperty("class Test { public string Name { get { return _name; } set { _name = value; } } private string _name; }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.Throw<Shouldly.ShouldAssertException>(() => expectations.IsAutoProperty());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasName_WithCorrectName_DoesNotThrow()
    {
        // Arrange
        var property = ParseProperty("class Test { public string MyProperty { get; set; } }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.NotThrow(() => expectations.HasName("MyProperty"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasExpressionBody_WithExpressionBodied_DoesNotThrow()
    {
        // Arrange
        var property = ParseProperty("class Test { public string Name => \"test\"; }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.NotThrow(() => expectations.HasExpressionBody("\"test\""));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void FluentAPI_ChainsCorrectly()
    {
        // Arrange
        var property = ParseProperty("class Test { public string Name { get; private set; } }");
        var expectations = new PropertyExpectations(property);

        // Act & Assert
        Should.NotThrow(() => expectations
            .IsPublic()
            .HasType("string")
            .HasName("Name")
            .HasGetter()
            .HasPrivateSetter()
            .IsAutoProperty());
    }
}
