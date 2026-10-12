using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Messages.Tests;

/// <summary>
/// Tests for the MessageCollectionAttribute class.
/// </summary>
public class MessageCollectionAttributeTests
{
    #region Constructor Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithValidName_SetsProperties()
    {
        // Arrange
        const string name = "MyMessageCollection";

        // Act
        var attribute = new MessageCollectionAttribute(name);

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
        Should.Throw<ArgumentNullException>(() => new MessageCollectionAttribute(null!))
            .ParamName.ShouldBe("name");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Constructor_WithEmptyOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => new MessageCollectionAttribute(invalidName));
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
        var attribute = new MessageCollectionAttribute("TestCollection");
        var newReturnType = typeof(GenericMessage);

        // Act
        attribute.ReturnType = newReturnType;

        // Assert
        attribute.ReturnType.ShouldBe(newReturnType);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ReturnType_DefaultsToIGenericMessage()
    {
        // Arrange & Act
        var attribute = new MessageCollectionAttribute("TestCollection");

        // Assert
        attribute.ReturnType.ShouldBe(typeof(IGenericMessage));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Name_IsReadOnly()
    {
        // Arrange
        var attribute = new MessageCollectionAttribute("InitialName");

        // Assert
        attribute.Name.ShouldBe("InitialName");
        typeof(MessageCollectionAttribute).GetProperty(nameof(MessageCollectionAttribute.Name))!
            .CanWrite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ReturnType_CanBeSetToCustomType()
    {
        // Arrange
        var attribute = new MessageCollectionAttribute("TestCollection");
        var customType = typeof(string);

        // Act
        attribute.ReturnType = customType;

        // Assert
        attribute.ReturnType.ShouldBe(customType);
    }

    #endregion

    #region AttributeUsage Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Attribute_HasCorrectAttributeUsage()
    {
        // Arrange
        var attributeType = typeof(MessageCollectionAttribute);

        // Act
        var usageAttribute = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            attributeType, typeof(AttributeUsageAttribute))!;

        // Assert
        usageAttribute.ShouldNotBeNull();
        usageAttribute.ValidOn.ShouldBe(AttributeTargets.Class);
        usageAttribute.AllowMultiple.ShouldBeFalse();
        usageAttribute.Inherited.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Attribute_CanBeAppliedToClass()
    {
        // Arrange & Act
        var attribute = (MessageCollectionAttribute)Attribute.GetCustomAttribute(
            typeof(TestClassWithMessageCollection), typeof(MessageCollectionAttribute))!;

        // Assert
        attribute.ShouldNotBeNull();
        attribute.Name.ShouldBe("TestMessages");
    }

    #endregion

    #region Edge Case Tests

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    [InlineData("A")]
    [InlineData("VeryLongCollectionNameThatIsStillValid")]
    [InlineData("Collection123")]
    [InlineData("Collection_With_Underscores")]
    public void Constructor_WithVariousValidNames_Succeeds(string name)
    {
        // Act
        var attribute = new MessageCollectionAttribute(name);

        // Assert
        attribute.Name.ShouldBe(name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_PreservesExactNameCasing()
    {
        // Arrange
        const string name = "MyExactCasing";

        // Act
        var attribute = new MessageCollectionAttribute(name);

        // Assert
        attribute.Name.ShouldBe("MyExactCasing");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithTabWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => new MessageCollectionAttribute("\t"));
        exception.ParamName.ShouldBe("name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithNewlineWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => new MessageCollectionAttribute("\n"));
        exception.ParamName.ShouldBe("name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithMixedWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => new MessageCollectionAttribute(" \t\n "));
        exception.ParamName.ShouldBe("name");
    }

    #endregion
}

[MessageCollection("TestMessages")]
internal class TestClassWithMessageCollection
{
}
