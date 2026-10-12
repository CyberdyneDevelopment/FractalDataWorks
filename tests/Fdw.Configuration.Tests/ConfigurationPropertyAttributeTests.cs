using Fdw.Configuration;

namespace Fdw.Configuration.Tests;

/// <summary>
/// Tests for ConfigurationPropertyAttribute.
/// </summary>
public class ConfigurationPropertyAttributeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var attr = new ConfigurationPropertyAttribute();

        // Assert
        attr.Label.ShouldBeNull();
        attr.HelpText.ShouldBeNull();
        attr.Placeholder.ShouldBeNull();
        attr.Order.ShouldBe(int.MaxValue);
        attr.IsHidden.ShouldBeFalse();
        attr.IsReadOnly.ShouldBeFalse();
        attr.MaxLength.ShouldBe(255);
        attr.ColumnName.ShouldBeNull();
        attr.Section.ShouldBe("General");
        attr.Width.ShouldBe(6);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllProperties_CanBeSet()
    {
        // Act
        var attr = new ConfigurationPropertyAttribute
        {
            Label = "Display Name",
            HelpText = "Enter the value",
            Placeholder = "Type here...",
            Order = 3,
            IsHidden = true,
            IsReadOnly = true,
            MaxLength = 500,
            ColumnName = "db_column",
            Section = "Advanced",
            Width = 12
        };

        // Assert
        attr.Label.ShouldBe("Display Name");
        attr.HelpText.ShouldBe("Enter the value");
        attr.Placeholder.ShouldBe("Type here...");
        attr.Order.ShouldBe(3);
        attr.IsHidden.ShouldBeTrue();
        attr.IsReadOnly.ShouldBeTrue();
        attr.MaxLength.ShouldBe(500);
        attr.ColumnName.ShouldBe("db_column");
        attr.Section.ShouldBe("Advanced");
        attr.Width.ShouldBe(12);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AttributeUsage_TargetsProperty()
    {
        var usage = typeof(ConfigurationPropertyAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>().First();

        usage.ValidOn.ShouldBe(AttributeTargets.Property);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AttributeUsage_DoesNotAllowMultiple()
    {
        var usage = typeof(ConfigurationPropertyAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>().First();

        usage.AllowMultiple.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AttributeUsage_IsInherited()
    {
        var usage = typeof(ConfigurationPropertyAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>().First();

        usage.Inherited.ShouldBeTrue();
    }
}
