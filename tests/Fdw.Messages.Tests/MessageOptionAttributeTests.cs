using Fdw.Messages.Attributes;

namespace Fdw.Messages.Tests;

/// <summary>
/// Tests for MessageOptionAttribute.
/// </summary>
public class MessageOptionAttributeTests
{
    private class TestCollection { }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_SetsCollectionType()
    {
        // Act
        var attr = new MessageOptionAttribute(typeof(TestCollection));

        // Assert
        attr.CollectionType.ShouldBe(typeof(TestCollection));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_ThrowsOnNullCollectionType()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new MessageOptionAttribute(null!))
            .ParamName.ShouldBe("collectionType");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AttributeUsage_TargetsClass()
    {
        var usage = typeof(MessageOptionAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>().First();

        usage.ValidOn.ShouldBe(AttributeTargets.Class);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AttributeUsage_DoesNotAllowMultiple()
    {
        var usage = typeof(MessageOptionAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>().First();

        usage.AllowMultiple.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AttributeUsage_IsNotInherited()
    {
        var usage = typeof(MessageOptionAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>().First();

        usage.Inherited.ShouldBeFalse();
    }
}
