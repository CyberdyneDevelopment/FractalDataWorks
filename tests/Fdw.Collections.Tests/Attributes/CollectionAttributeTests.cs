using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Collections.Tests.Attributes;

/// <summary>
/// Tests for MutableTypeCollectionAttribute, TypeInstanceCollectionAttribute,
/// ServiceTypeCollectionAttribute,
/// MutableServiceTypeCollectionAttribute, ServiceTypeInstanceCollectionAttribute,
/// and ServiceTypeOptionAttribute.
/// </summary>
public class CollectionAttributeTests
{
    private class TestBase { }
    private class TestReturn { }
    private class TestCollection { }
    private class TestParent { }

    #region MutableTypeCollectionAttribute Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MutableTypeCollectionAttribute_Constructor_SetsRequiredProperties()
    {
        var attr = new MutableTypeCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection));

        attr.BaseType.ShouldBe(typeof(TestBase));
        attr.DefaultReturnType.ShouldBe(typeof(TestReturn));
        attr.CollectionType.ShouldBe(typeof(TestCollection));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MutableTypeCollectionAttribute_ThrowsOnNullBaseType()
    {
        Should.Throw<ArgumentNullException>(() =>
            new MutableTypeCollectionAttribute(null!, typeof(TestReturn), typeof(TestCollection)))
            .ParamName.ShouldBe("baseType");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MutableTypeCollectionAttribute_ThrowsOnNullDefaultReturnType()
    {
        Should.Throw<ArgumentNullException>(() =>
            new MutableTypeCollectionAttribute(typeof(TestBase), null!, typeof(TestCollection)))
            .ParamName.ShouldBe("defaultReturnType");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MutableTypeCollectionAttribute_ThrowsOnNullCollectionType()
    {
        Should.Throw<ArgumentNullException>(() =>
            new MutableTypeCollectionAttribute(typeof(TestBase), typeof(TestReturn), null!))
            .ParamName.ShouldBe("collectionType");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MutableTypeCollectionAttribute_BaseTypeName_ReturnsFullName()
    {
        var attr = new MutableTypeCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection));
        attr.BaseTypeName.ShouldBe(typeof(TestBase).FullName);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MutableTypeCollectionAttribute_CollectionName_ReturnsName()
    {
        var attr = new MutableTypeCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection));
        attr.CollectionName.ShouldBe(typeof(TestCollection).Name);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MutableTypeCollectionAttribute_UseMethods_DefaultsFalse()
    {
        var attr = new MutableTypeCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection));
        attr.UseMethods.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MutableTypeCollectionAttribute_RestrictToCurrentCompilation_DefaultsFalse()
    {
        var attr = new MutableTypeCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection));
        attr.RestrictToCurrentCompilation.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MutableTypeCollectionAttribute_GenerateUIComponent_DefaultsFalse()
    {
        var attr = new MutableTypeCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection));
        attr.GenerateUIComponent.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MutableTypeCollectionAttribute_OptionalProperties_CanBeSet()
    {
        var attr = new MutableTypeCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection))
        {
            UseMethods = true,
            RestrictToCurrentCompilation = true,
            GenerateUIComponent = true,
            UIComponent = typeof(TestParent),
            TypeOption = typeof(TestBase),
            TypeOptionName = "TestOption"
        };

        attr.UseMethods.ShouldBeTrue();
        attr.RestrictToCurrentCompilation.ShouldBeTrue();
        attr.GenerateUIComponent.ShouldBeTrue();
        attr.UIComponent.ShouldBe(typeof(TestParent));
        attr.TypeOption.ShouldBe(typeof(TestBase));
        attr.TypeOptionName.ShouldBe("TestOption");
    }

    #endregion

    #region TypeInstanceCollectionAttribute Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TypeInstanceCollectionAttribute_Constructor_SetsRequiredProperties()
    {
        var attr = new TypeInstanceCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection));

        attr.BaseType.ShouldBe(typeof(TestBase));
        attr.DefaultReturnType.ShouldBe(typeof(TestReturn));
        attr.CollectionType.ShouldBe(typeof(TestCollection));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TypeInstanceCollectionAttribute_ThrowsOnNullBaseType()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TypeInstanceCollectionAttribute(null!, typeof(TestReturn), typeof(TestCollection)))
            .ParamName.ShouldBe("baseType");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TypeInstanceCollectionAttribute_ThrowsOnNullDefaultReturnType()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TypeInstanceCollectionAttribute(typeof(TestBase), null!, typeof(TestCollection)))
            .ParamName.ShouldBe("defaultReturnType");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TypeInstanceCollectionAttribute_ThrowsOnNullCollectionType()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TypeInstanceCollectionAttribute(typeof(TestBase), typeof(TestReturn), null!))
            .ParamName.ShouldBe("collectionType");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TypeInstanceCollectionAttribute_BaseTypeName_ReturnsFullName()
    {
        var attr = new TypeInstanceCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection));
        attr.BaseTypeName.ShouldBe(typeof(TestBase).FullName);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TypeInstanceCollectionAttribute_CollectionName_ReturnsName()
    {
        var attr = new TypeInstanceCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection));
        attr.CollectionName.ShouldBe(typeof(TestCollection).Name);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TypeInstanceCollectionAttribute_OptionalProperties_CanBeSet()
    {
        var attr = new TypeInstanceCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection))
        {
            UseMethods = true,
            RestrictToCurrentCompilation = true,
            GenerateUIComponent = true,
            UIComponent = typeof(TestParent),
            TypeOption = typeof(TestBase),
            TypeOptionName = "FactoryOption"
        };

        attr.UseMethods.ShouldBeTrue();
        attr.RestrictToCurrentCompilation.ShouldBeTrue();
        attr.GenerateUIComponent.ShouldBeTrue();
        attr.UIComponent.ShouldBe(typeof(TestParent));
        attr.TypeOption.ShouldBe(typeof(TestBase));
        attr.TypeOptionName.ShouldBe("FactoryOption");
    }

    #endregion

    #region ServiceTypeCollectionAttribute Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ServiceTypeCollectionAttribute_Constructor_SetsRequiredProperties()
    {
        var attr = new ServiceTypeCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection));

        attr.BaseType.ShouldBe(typeof(TestBase));
        attr.InterfaceType.ShouldBe(typeof(TestReturn));
        attr.CollectionType.ShouldBe(typeof(TestCollection));
        attr.ParentCollection.ShouldBeNull();
        attr.Name.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ServiceTypeCollectionAttribute_Constructor_SetsOptionalParams()
    {
        var attr = new ServiceTypeCollectionAttribute(
            typeof(TestBase), typeof(TestReturn), typeof(TestCollection),
            parentCollection: typeof(TestParent), name: "Child");

        attr.ParentCollection.ShouldBe(typeof(TestParent));
        attr.Name.ShouldBe("Child");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ServiceTypeCollectionAttribute_DefaultOptionalProperties()
    {
        var attr = new ServiceTypeCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection));

        attr.RestrictToCurrentCompilation.ShouldBeFalse();
        attr.ServiceInterface.ShouldBeNull();
        attr.ConfigurationInterface.ShouldBeNull();
        attr.ProviderType.ShouldBeNull();
        attr.ProviderInterface.ShouldBeNull();
        attr.ServiceCategory.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ServiceTypeCollectionAttribute_AllProperties_CanBeSet()
    {
        var attr = new ServiceTypeCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection))
        {
            RestrictToCurrentCompilation = true,
            ServiceInterface = typeof(TestBase),
            ConfigurationInterface = typeof(TestReturn),
            ProviderType = typeof(TestParent),
            ProviderInterface = typeof(TestReturn),
            ServiceCategory = "Connection"
        };

        attr.RestrictToCurrentCompilation.ShouldBeTrue();
        attr.ServiceInterface.ShouldBe(typeof(TestBase));
        attr.ConfigurationInterface.ShouldBe(typeof(TestReturn));
        attr.ProviderType.ShouldBe(typeof(TestParent));
        attr.ProviderInterface.ShouldBe(typeof(TestReturn));
        attr.ServiceCategory.ShouldBe("Connection");
    }

    #endregion

    #region MutableServiceTypeCollectionAttribute Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MutableServiceTypeCollectionAttribute_Constructor_SetsRequiredProperties()
    {
        var attr = new MutableServiceTypeCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection));

        attr.BaseType.ShouldBe(typeof(TestBase));
        attr.InterfaceType.ShouldBe(typeof(TestReturn));
        attr.CollectionType.ShouldBe(typeof(TestCollection));
        attr.ParentCollection.ShouldBeNull();
        attr.Name.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MutableServiceTypeCollectionAttribute_WithParent_SetsParentAndName()
    {
        var attr = new MutableServiceTypeCollectionAttribute(
            typeof(TestBase), typeof(TestReturn), typeof(TestCollection),
            parentCollection: typeof(TestParent), name: "MutableChild");

        attr.ParentCollection.ShouldBe(typeof(TestParent));
        attr.Name.ShouldBe("MutableChild");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MutableServiceTypeCollectionAttribute_DefaultOptionalProperties()
    {
        var attr = new MutableServiceTypeCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection));

        attr.RestrictToCurrentCompilation.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MutableServiceTypeCollectionAttribute_OptionalProperties_CanBeSet()
    {
        var attr = new MutableServiceTypeCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection))
        {
            RestrictToCurrentCompilation = true
        };

        attr.RestrictToCurrentCompilation.ShouldBeTrue();
    }

    #endregion

    #region ServiceTypeInstanceCollectionAttribute Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ServiceTypeInstanceCollectionAttribute_Constructor_SetsRequiredProperties()
    {
        var attr = new ServiceTypeInstanceCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection));

        attr.BaseType.ShouldBe(typeof(TestBase));
        attr.InterfaceType.ShouldBe(typeof(TestReturn));
        attr.CollectionType.ShouldBe(typeof(TestCollection));
        attr.ParentCollection.ShouldBeNull();
        attr.Name.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ServiceTypeInstanceCollectionAttribute_WithParent_SetsParentAndName()
    {
        var attr = new ServiceTypeInstanceCollectionAttribute(
            typeof(TestBase), typeof(TestReturn), typeof(TestCollection),
            parentCollection: typeof(TestParent), name: "InstanceChild");

        attr.ParentCollection.ShouldBe(typeof(TestParent));
        attr.Name.ShouldBe("InstanceChild");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ServiceTypeInstanceCollectionAttribute_DefaultOptionalProperties()
    {
        var attr = new ServiceTypeInstanceCollectionAttribute(typeof(TestBase), typeof(TestReturn), typeof(TestCollection));

        attr.RestrictToCurrentCompilation.ShouldBeFalse();
    }

    #endregion

    #region ServiceTypeOptionAttribute Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ServiceTypeOptionAttribute_Constructor_SetsProperties()
    {
        var attr = new ServiceTypeOptionAttribute(typeof(TestCollection), "MsSql");

        attr.CollectionType.ShouldBe(typeof(TestCollection));
        attr.Name.ShouldBe("MsSql");
    }

    #endregion
}
