using Fdw.Configuration;

namespace Fdw.Configuration.Tests;

/// <summary>
/// Tests for ConfigurationSectionAttribute.
/// </summary>
public class ConfigurationSectionAttributeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Constructor_SetsName()
    {
        // Act
        var attr = new ConfigurationSectionAttribute("Basic");

        // Assert
        attr.Name.ShouldBe("Basic");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Constructor_ThrowsOnNullName()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ConfigurationSectionAttribute(null!))
            .ParamName.ShouldBe("name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Title_DefaultsToNull()
    {
        // Act
        var attr = new ConfigurationSectionAttribute("Section");

        // Assert
        attr.Title.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Title_CanBeSet()
    {
        // Act
        var attr = new ConfigurationSectionAttribute("Section") { Title = "My Section" };

        // Assert
        attr.Title.ShouldBe("My Section");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Description_DefaultsToNull()
    {
        // Act
        var attr = new ConfigurationSectionAttribute("Section");

        // Assert
        attr.Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Description_CanBeSet()
    {
        // Act
        var attr = new ConfigurationSectionAttribute("Section") { Description = "Help text" };

        // Assert
        attr.Description.ShouldBe("Help text");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Order_DefaultsToIntMaxValue()
    {
        // Act
        var attr = new ConfigurationSectionAttribute("Section");

        // Assert
        attr.Order.ShouldBe(int.MaxValue);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Order_CanBeSet()
    {
        // Act
        var attr = new ConfigurationSectionAttribute("Section") { Order = 5 };

        // Assert
        attr.Order.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IsCollapsible_DefaultsToFalse()
    {
        // Act
        var attr = new ConfigurationSectionAttribute("Section");

        // Assert
        attr.IsCollapsible.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IsCollapsible_CanBeSet()
    {
        // Act
        var attr = new ConfigurationSectionAttribute("Section") { IsCollapsible = true };

        // Assert
        attr.IsCollapsible.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IsExpanded_DefaultsToTrue()
    {
        // Act
        var attr = new ConfigurationSectionAttribute("Section");

        // Assert
        attr.IsExpanded.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IsExpanded_CanBeSet()
    {
        // Act
        var attr = new ConfigurationSectionAttribute("Section") { IsExpanded = false };

        // Assert
        attr.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AttributeUsage_AllowsMultiple()
    {
        // Act
        var usage = typeof(ConfigurationSectionAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>().First();

        // Assert
        usage.AllowMultiple.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AttributeUsage_IsInherited()
    {
        // Act
        var usage = typeof(ConfigurationSectionAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>().First();

        // Assert
        usage.Inherited.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AttributeUsage_TargetsClass()
    {
        // Act
        var usage = typeof(ConfigurationSectionAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>().First();

        // Assert
        usage.ValidOn.ShouldBe(AttributeTargets.Class);
    }
}
