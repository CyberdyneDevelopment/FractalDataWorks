using Fdw.Collections.Attributes;

namespace Fdw.Collections.Tests.Attributes;

public class TypeOptionAttributeTests
{
    private class TestCollection { }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_SetsCollectionTypeAndName()
    {
        var attribute = new TypeOptionAttribute(typeof(TestCollection), "TestName");

        attribute.CollectionType.ShouldBe(typeof(TestCollection));
        attribute.Name.ShouldBe("TestName");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_ThrowsArgumentNullException_WhenCollectionTypeIsNull()
    {
        Should.Throw<ArgumentNullException>(() => new TypeOptionAttribute(null!, "Name"))
            .ParamName.ShouldBe("collectionType");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_ThrowsArgumentNullException_WhenNameIsNull()
    {
        Should.Throw<ArgumentNullException>(() => new TypeOptionAttribute(typeof(TestCollection), null!))
            .ParamName.ShouldBe("name");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AttributeUsage_AllowsSingleInstance()
    {
        var usage = typeof(TypeOptionAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>().FirstOrDefault();

        usage.ShouldNotBeNull();
        usage.AllowMultiple.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AttributeUsage_TargetsClass()
    {
        var usage = typeof(TypeOptionAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>().FirstOrDefault();

        usage.ShouldNotBeNull();
        usage.ValidOn.ShouldBe(AttributeTargets.Class);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CollectionType_IsReadOnly()
    {
        var property = typeof(TypeOptionAttribute).GetProperty(nameof(TypeOptionAttribute.CollectionType));

        property.ShouldNotBeNull();
        property!.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Name_IsReadOnly()
    {
        var property = typeof(TypeOptionAttribute).GetProperty(nameof(TypeOptionAttribute.Name));

        property.ShouldNotBeNull();
        property!.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithDifferentTypes_StoresCorrectly()
    {
        var collectionType = typeof(List<int>);
        var attribute = new TypeOptionAttribute(collectionType, "ListName");

        attribute.CollectionType.ShouldBe(collectionType);
        attribute.Name.ShouldBe("ListName");
    }
}
