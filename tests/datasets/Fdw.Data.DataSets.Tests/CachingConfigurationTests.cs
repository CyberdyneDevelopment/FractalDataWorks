using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

public class CachingConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void NothingIsAssumedInCode()
    {
        // Arrange & Act: caching's values leave code (FDW-3931); its declared defaults come with FDW-3823 plan revision 4.
        var config = new CachingConfiguration();

        // Assert
        config.Enabled.ShouldBeFalse();
        config.DurationMinutes.ShouldBeNull();
        config.KeyPattern.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void Enabled_CanBeSet()
    {
        // Arrange
        var config = new CachingConfiguration { Enabled = false };

        // Assert
        config.Enabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DurationMinutes_CanBeSet()
    {
        // Arrange
        var config = new CachingConfiguration { DurationMinutes = 120 };

        // Assert
        config.DurationMinutes.ShouldBe(120);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void KeyPattern_CanBeSet()
    {
        // Arrange
        var config = new CachingConfiguration { KeyPattern = "custom:{id}" };

        // Assert
        config.KeyPattern.ShouldBe("custom:{id}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange
        var config = new CachingConfiguration
        {
            Enabled = false,
            DurationMinutes = 30,
            KeyPattern = "test:{key}"
        };

        // Assert
        config.Enabled.ShouldBeFalse();
        config.DurationMinutes.ShouldBe(30);
        config.KeyPattern.ShouldBe("test:{key}");
    }
}
