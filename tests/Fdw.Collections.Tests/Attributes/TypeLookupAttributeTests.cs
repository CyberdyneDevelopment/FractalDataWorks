using Fdw.Collections.Attributes;

namespace Fdw.Collections.Tests.Attributes;

public class TypeLookupAttributeTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_MinimalParameters_SetsDefaults()
    {
        var attribute = new TypeLookupAttribute("TestMethod");

        attribute.MethodName.ShouldBe("TestMethod");
        attribute.IsUnique.ShouldBeTrue();
        attribute.ReturnType.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithIsUniqueFalse_SetsProperty()
    {
        var attribute = new TypeLookupAttribute("TestMethod", isUnique: false);

        attribute.MethodName.ShouldBe("TestMethod");
        attribute.IsUnique.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithReturnType_SetsProperty()
    {
        var attribute = new TypeLookupAttribute("TestMethod", returnType: typeof(string));

        attribute.MethodName.ShouldBe("TestMethod");
        attribute.ReturnType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithAllParameters_SetsAllProperties()
    {
        var attribute = new TypeLookupAttribute("TestMethod", returnType: typeof(int), isUnique: false);

        attribute.MethodName.ShouldBe("TestMethod");
        attribute.IsUnique.ShouldBeFalse();
        attribute.ReturnType.ShouldBe(typeof(int));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_ThrowsArgumentNullException_WhenMethodNameIsNull()
    {
        Should.Throw<ArgumentNullException>(() => new TypeLookupAttribute(null!))
            .ParamName.ShouldBe("methodName");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AttributeUsage_AllowsSingleInstance()
    {
        var usage = typeof(TypeLookupAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>().FirstOrDefault();

        usage.ShouldNotBeNull();
        usage.AllowMultiple.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AttributeUsage_TargetsProperty()
    {
        var usage = typeof(TypeLookupAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>().FirstOrDefault();

        usage.ShouldNotBeNull();
        usage.ValidOn.ShouldBe(AttributeTargets.Property);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithIsUniqueTrue_ExplicitlySetsTrue()
    {
        var attribute = new TypeLookupAttribute("TestMethod", isUnique: true);

        attribute.MethodName.ShouldBe("TestMethod");
        attribute.IsUnique.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithNullReturnType_ExplicitlyIsNull()
    {
        var attribute = new TypeLookupAttribute("TestMethod", returnType: null);

        attribute.MethodName.ShouldBe("TestMethod");
        attribute.ReturnType.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithAllParametersExplicitlySet_SetsCorrectly()
    {
        var attribute = new TypeLookupAttribute(
            methodName: "TestMethod",
            returnType: null,
            isUnique: true);

        attribute.MethodName.ShouldBe("TestMethod");
        attribute.IsUnique.ShouldBeTrue();
        attribute.ReturnType.ShouldBeNull();
    }
}
