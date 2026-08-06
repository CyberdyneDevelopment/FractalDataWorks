using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Messages.Tests;

/// <summary>
/// Tests for the GlobalMessageCollectionAttribute class.
/// </summary>
public class GlobalMessageCollectionAttributeTests
{
    #region Constructor Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithValidName_SetsProperties()
    {
        // Arrange
        const string name = "GlobalMessages";

        // Act
        var attribute = new GlobalMessageCollectionAttribute(name);

        // Assert
        attribute.Name.ShouldBe(name);
        attribute.ReturnType.ShouldBe(typeof(IGenericMessage));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new GlobalMessageCollectionAttribute(null!))
            .ParamName.ShouldBe("name");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Constructor_WithEmptyOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => new GlobalMessageCollectionAttribute(invalidName));
        exception.ParamName.ShouldBe("name");
        exception.Message.ShouldContain("Name cannot be empty or whitespace");
    }

    #endregion

    #region Property Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ReturnType_CanBeSet()
    {
        // Arrange
        var attribute = new GlobalMessageCollectionAttribute("GlobalMessages");
        var newReturnType = typeof(GenericMessage);

        // Act
        attribute.ReturnType = newReturnType;

        // Assert
        attribute.ReturnType.ShouldBe(newReturnType);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Name_IsReadOnly()
    {
        // Arrange
        const string name = "TestGlobalMessages";
        var attribute = new GlobalMessageCollectionAttribute(name);

        // Assert
        attribute.Name.ShouldBe(name);
        // Name property should only have a getter, not a setter
        var property = typeof(GlobalMessageCollectionAttribute).GetProperty("Name");
        property.ShouldNotBeNull();
        property!.CanWrite.ShouldBeFalse();
        property.CanRead.ShouldBeTrue();
    }

    #endregion

    #region Attribute Metadata Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Attribute_HasCorrectAttributeUsage()
    {
        // Arrange
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            typeof(GlobalMessageCollectionAttribute),
            typeof(AttributeUsageAttribute));

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.ValidOn.ShouldBe(AttributeTargets.Class);
        attributeUsage.AllowMultiple.ShouldBeFalse();
        attributeUsage.Inherited.ShouldBeTrue(); // Default value
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Attribute_IsExcludedFromCodeCoverage()
    {
        // Arrange
        var excludeAttribute = Attribute.GetCustomAttribute(
            typeof(GlobalMessageCollectionAttribute),
            typeof(System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute));

        // Assert
        excludeAttribute.ShouldNotBeNull();
    }

    #endregion
}
