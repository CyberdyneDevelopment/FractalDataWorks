using Fdw.Messages;

namespace Fdw.Messages.Tests;

/// <summary>
/// Tests for the MessageAttribute class.
/// </summary>
public class MessageAttributeTests
{
    #region Constructor Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var attribute = new MessageAttribute();

        // Assert
        attribute.ReturnType.ShouldBe(typeof(IGenericMessage));
        attribute.CollectionName.ShouldBeNull();
        attribute.Name.ShouldBeNull();
        attribute.ReturnTypeNamespace.ShouldBeNull();
        attribute.IncludeInGlobalCollection.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithCollectionName_SetsProperties()
    {
        // Arrange
        const string collectionName = "MyMessages";

        // Act
        var attribute = new MessageAttribute(collectionName);

        // Assert
        attribute.CollectionName.ShouldBe(collectionName);
        attribute.ReturnType.ShouldBe(typeof(IGenericMessage));
        attribute.IncludeInGlobalCollection.ShouldBeTrue();
    }

    #endregion

    #region Property Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Properties_CanBeSetAndRead()
    {
        // Arrange
        var attribute = new MessageAttribute();
        const string collectionName = "CustomCollection";
        const string name = "CustomName";
        var returnType = typeof(GenericMessage);
        const string returnTypeNamespace = "Fdw.Messages";

        // Act
        attribute.CollectionName = collectionName;
        attribute.Name = name;
        attribute.ReturnType = returnType;
        attribute.ReturnTypeNamespace = returnTypeNamespace;
        attribute.IncludeInGlobalCollection = false;

        // Assert
        attribute.CollectionName.ShouldBe(collectionName);
        attribute.Name.ShouldBe(name);
        attribute.ReturnType.ShouldBe(returnType);
        attribute.ReturnTypeNamespace.ShouldBe(returnTypeNamespace);
        attribute.IncludeInGlobalCollection.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CollectionName_CanBeSetToNull()
    {
        // Arrange
        var attribute = new MessageAttribute("InitialName");

        // Act
        attribute.CollectionName = null;

        // Assert
        attribute.CollectionName.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Name_CanBeSetToNull()
    {
        // Arrange
        var attribute = new MessageAttribute();

        // Act
        attribute.Name = null;

        // Assert
        attribute.Name.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ReturnType_CanBeSetToNull()
    {
        // Arrange
        var attribute = new MessageAttribute();

        // Act
        attribute.ReturnType = null;

        // Assert
        attribute.ReturnType.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ReturnTypeNamespace_CanBeSetToNull()
    {
        // Arrange
        var attribute = new MessageAttribute();
        attribute.ReturnTypeNamespace = "Test";

        // Act
        attribute.ReturnTypeNamespace = null;

        // Assert
        attribute.ReturnTypeNamespace.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IncludeInGlobalCollection_CanBeToggled()
    {
        // Arrange
        var attribute = new MessageAttribute();

        // Assert initial state
        attribute.IncludeInGlobalCollection.ShouldBeTrue();

        // Act
        attribute.IncludeInGlobalCollection = false;

        // Assert
        attribute.IncludeInGlobalCollection.ShouldBeFalse();

        // Act
        attribute.IncludeInGlobalCollection = true;

        // Assert
        attribute.IncludeInGlobalCollection.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    [InlineData("Collection1")]
    [InlineData("My_Collection")]
    [InlineData("Collection123")]
    public void CollectionName_AcceptsDifferentFormats(string collectionName)
    {
        // Arrange & Act
        var attribute = new MessageAttribute(collectionName);

        // Assert
        attribute.CollectionName.ShouldBe(collectionName);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    [InlineData("Name1")]
    [InlineData("My_Name")]
    [InlineData("Name123")]
    public void Name_AcceptsDifferentFormats(string name)
    {
        // Arrange
        var attribute = new MessageAttribute();

        // Act
        attribute.Name = name;

        // Assert
        attribute.Name.ShouldBe(name);
    }

    #endregion

    #region AttributeUsage Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Attribute_HasCorrectAttributeUsage()
    {
        // Arrange
        var attributeType = typeof(MessageAttribute);

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
        var attribute = (MessageAttribute)Attribute.GetCustomAttribute(
            typeof(TestMessageWithAttribute), typeof(MessageAttribute))!;

        // Assert
        attribute.ShouldNotBeNull();
        attribute.CollectionName.ShouldBe("TestMessages");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Attribute_CanBeAppliedWithoutParameters()
    {
        // Arrange & Act
        var attribute = (MessageAttribute)Attribute.GetCustomAttribute(
            typeof(TestMessageWithoutParameters), typeof(MessageAttribute))!;

        // Assert
        attribute.ShouldNotBeNull();
        attribute.CollectionName.ShouldBeNull();
        attribute.ReturnType.ShouldBe(typeof(IGenericMessage));
    }

    #endregion

    #region Constructor Parameter Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithNullCollectionName_SetsCollectionNameToNull()
    {
        // Arrange & Act
        var attribute = new MessageAttribute(null!);

        // Assert
        attribute.CollectionName.ShouldBeNull();
        attribute.ReturnType.ShouldBe(typeof(IGenericMessage));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithEmptyCollectionName_SetsCollectionNameToEmpty()
    {
        // Arrange & Act
        var attribute = new MessageAttribute(string.Empty);

        // Assert
        attribute.CollectionName.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_PreservesCollectionNameCasing()
    {
        // Arrange & Act
        var attribute = new MessageAttribute("MyExactCasing");

        // Assert
        attribute.CollectionName.ShouldBe("MyExactCasing");
    }

    #endregion

    #region Integration Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllProperties_CanBeSetIndependently()
    {
        // Arrange
        var attribute = new MessageAttribute();

        // Act & Assert - Set each property independently
        attribute.CollectionName = "Collection1";
        attribute.CollectionName.ShouldBe("Collection1");

        attribute.Name = "Name1";
        attribute.Name.ShouldBe("Name1");

        attribute.ReturnType = typeof(string);
        attribute.ReturnType.ShouldBe(typeof(string));

        attribute.ReturnTypeNamespace = "System";
        attribute.ReturnTypeNamespace.ShouldBe("System");

        attribute.IncludeInGlobalCollection = false;
        attribute.IncludeInGlobalCollection.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithCollectionName_DoesNotAffectOtherProperties()
    {
        // Arrange & Act
        var attribute = new MessageAttribute("CustomCollection");

        // Assert
        attribute.CollectionName.ShouldBe("CustomCollection");
        attribute.Name.ShouldBeNull();
        attribute.ReturnType.ShouldBe(typeof(IGenericMessage));
        attribute.ReturnTypeNamespace.ShouldBeNull();
        attribute.IncludeInGlobalCollection.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ReturnType_CanBeSetToAnyType()
    {
        // Arrange
        var attribute = new MessageAttribute();
        var types = new[] { typeof(string), typeof(int), typeof(object), typeof(GenericMessage) };

        // Act & Assert
        foreach (var type in types)
        {
            attribute.ReturnType = type;
            attribute.ReturnType.ShouldBe(type);
        }
    }

    #endregion
}

[Message("TestMessages")]
internal class TestMessageWithAttribute
{
}

[Message]
internal class TestMessageWithoutParameters
{
}
