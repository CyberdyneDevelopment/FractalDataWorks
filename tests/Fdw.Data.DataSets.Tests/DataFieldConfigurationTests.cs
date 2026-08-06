using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

public class DataFieldConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new DataFieldConfiguration();

        // Assert
        config.Name.ShouldBe(string.Empty);
        config.Description.ShouldBeNull();
        config.TypeName.ShouldBe(string.Empty);
        config.IsKey.ShouldBeFalse();
        config.IsRequired.ShouldBeFalse();
        config.IsIndexed.ShouldBeFalse();
        config.MaxLength.ShouldBeNull();
        config.DefaultValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Properties_CanBeSet()
    {
        // Arrange
        var config = new DataFieldConfiguration
        {
            Name = "TestField",
            Description = "Test Description",
            TypeName = "System.String",
            IsKey = true,
            IsRequired = true,
            IsIndexed = true,
            MaxLength = 100,
            DefaultValue = "default"
        };

        // Assert
        config.Name.ShouldBe("TestField");
        config.Description.ShouldBe("Test Description");
        config.TypeName.ShouldBe("System.String");
        config.IsKey.ShouldBeTrue();
        config.IsRequired.ShouldBeTrue();
        config.IsIndexed.ShouldBeTrue();
        config.MaxLength.ShouldBe(100);
        config.DefaultValue.ShouldBe("default");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Clone_CreatesExactCopy()
    {
        // Arrange
        var original = new DataFieldConfiguration
        {
            Name = "TestField",
            Description = "Test Description",
            TypeName = "System.String",
            IsKey = true,
            IsRequired = true,
            IsIndexed = true,
            MaxLength = 100,
            DefaultValue = "default"
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.Name.ShouldBe(original.Name);
        clone.Description.ShouldBe(original.Description);
        clone.TypeName.ShouldBe(original.TypeName);
        clone.IsKey.ShouldBe(original.IsKey);
        clone.IsRequired.ShouldBe(original.IsRequired);
        clone.IsIndexed.ShouldBe(original.IsIndexed);
        clone.MaxLength.ShouldBe(original.MaxLength);
        clone.DefaultValue.ShouldBe(original.DefaultValue);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Clone_ModifyingClone_DoesNotAffectOriginal()
    {
        // Arrange
        var original = new DataFieldConfiguration
        {
            Name = "Original",
            Description = "Original Description"
        };

        // Act
        var clone = original.Clone();
        clone.Name = "Modified";
        clone.Description = "Modified Description";

        // Assert
        original.Name.ShouldBe("Original");
        original.Description.ShouldBe("Original Description");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Clone_WithNullValues_ClonesCorrectly()
    {
        // Arrange
        var original = new DataFieldConfiguration
        {
            Name = "TestField",
            Description = null,
            MaxLength = null,
            DefaultValue = null
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Description.ShouldBeNull();
        clone.MaxLength.ShouldBeNull();
        clone.DefaultValue.ShouldBeNull();
    }
}
