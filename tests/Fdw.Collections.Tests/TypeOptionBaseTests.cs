using Fdw.Collections;

namespace Fdw.Collections.Tests;

/// <summary>
/// Tests for TypeOptionBase equality, hashing, ToString, and category behavior.
/// </summary>
public class TypeOptionBaseTests
{
    private class TestOption : TypeOptionBase<int, TestOption>
    {
        public TestOption(int id, string name) : base(id, name) { }
        public TestOption(int id, string name, string? category) : base(id, name, category) { }
        public TestOption(int id, string name, string configKey, string displayName, string description, string? category)
            : base(id, name, configKey, displayName, description, category) { }
    }

    private class OtherOption : TypeOptionBase<int, OtherOption>
    {
        public OtherOption(int id, string name) : base(id, name) { }
    }

    private class StringKeyOption : TypeOptionBase<string, StringKeyOption>
    {
        public StringKeyOption(string id, string name) : base(id, name) { }
    }

    #region Constructor Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithIdAndName_SetsProperties()
    {
        var option = new TestOption(1, "Test");

        option.Id.ShouldBe(1);
        option.Name.ShouldBe("Test");
        option.Category.ShouldBe("NotCategorized");
        option.ConfigurationKey.ShouldBe("TypeOptions:Test");
        option.DisplayName.ShouldBe("Test");
        option.Description.ShouldBe("Type option: Test");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithCategory_SetsCategory()
    {
        var option = new TestOption(1, "Test", "MyCategory");

        option.Category.ShouldBe("MyCategory");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithNullCategory_ReturnsNotCategorized()
    {
        var option = new TestOption(1, "Test", (string?)null);

        option.Category.ShouldBe("NotCategorized");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithEmptyCategory_ReturnsNotCategorized()
    {
        var option = new TestOption(1, "Test", string.Empty);

        option.Category.ShouldBe("NotCategorized");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new TestOption(1, null!));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithFullMetadata_SetsAllProperties()
    {
        var option = new TestOption(5, "Custom", "cfg:Custom", "Custom Display", "A custom option", "Advanced");

        option.Id.ShouldBe(5);
        option.Name.ShouldBe("Custom");
        option.ConfigurationKey.ShouldBe("cfg:Custom");
        option.DisplayName.ShouldBe("Custom Display");
        option.Description.ShouldBe("A custom option");
        option.Category.ShouldBe("Advanced");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithFullMetadataNullConfigKey_UsesDefaultKey()
    {
        var option = new TestOption(5, "Custom", null!, null!, null!, null);

        option.ConfigurationKey.ShouldBe("TypeOptions:Custom");
        option.DisplayName.ShouldBe("Custom");
        option.Description.ShouldBe("Type option: Custom");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithFullMetadataNullName_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TestOption(5, null!, "key", "display", "desc", "cat"));
    }

    #endregion

    #region Equality Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_SameId_ReturnsTrue()
    {
        var a = new TestOption(1, "A");
        var b = new TestOption(1, "B");

        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_DifferentId_ReturnsFalse()
    {
        var a = new TestOption(1, "A");
        var b = new TestOption(2, "B");

        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_Null_ReturnsFalse()
    {
        var a = new TestOption(1, "A");

        a.Equals(null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_NonTypeOption_ReturnsFalse()
    {
        var a = new TestOption(1, "A");

        a.Equals("not a type option").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_DifferentKeyType_ReturnsFalse()
    {
        var intOption = new TestOption(1, "A");
        var stringOption = new StringKeyOption("1", "B");

        intOption.Equals(stringOption).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Equals_CrossTypeWithSameId_ReturnsTrue()
    {
        // Different TypeOptionBase<int,T> types but same int Id
        var a = new TestOption(1, "A");
        var b = new OtherOption(1, "B");

        a.Equals(b).ShouldBeTrue();
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GetHashCode_SameId_SameHash()
    {
        var a = new TestOption(42, "A");
        var b = new TestOption(42, "B");

        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GetHashCode_DifferentId_DifferentHash()
    {
        var a = new TestOption(1, "A");
        var b = new TestOption(2, "B");

        a.GetHashCode().ShouldNotBe(b.GetHashCode());
    }

    #endregion

    #region ToString Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToString_ReturnsName()
    {
        var option = new TestOption(1, "MyOption");

        option.ToString().ShouldBe("MyOption");
    }

    #endregion

    #region ITypeOption Explicit Interface Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ITypeOption_Id_ReturnsBoxedValue()
    {
        ITypeOption option = new TestOption(42, "Test");

        option.Id.ShouldBeOfType<int>();
        ((int)option.Id).ShouldBe(42);
    }

    #endregion
}
