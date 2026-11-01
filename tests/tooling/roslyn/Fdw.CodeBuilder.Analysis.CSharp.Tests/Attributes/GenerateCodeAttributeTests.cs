using Fdw.CodeBuilder.Analysis.CSharp.Attributes;

namespace Fdw.CodeBuilder.Analysis.CSharp.Tests.Attributes;

public class GenerateCodeAttributeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Constructor_CreatesAttribute()
    {
        // Arrange & Act
        var attribute = new GenerateCodeAttribute();

        // Assert
        attribute.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Attribute_CanBeAppliedToClass()
    {
        // Arrange & Act
        var type = typeof(TestClassWithAttribute);
        var attributes = type.GetCustomAttributes(typeof(GenerateCodeAttribute), false);

        // Assert
        attributes.ShouldNotBeEmpty();
    }

    [GenerateCode]
    private class TestClassWithAttribute
    {
    }
}
