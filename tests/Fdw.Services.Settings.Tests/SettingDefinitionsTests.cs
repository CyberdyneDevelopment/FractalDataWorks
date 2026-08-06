using Fdw.Services.Settings;
using Shouldly;
using Xunit;

namespace Fdw.Services.Settings.Tests;

public sealed class SettingDefinitionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void MaxPaginationSizeHasExpectedValue()
    {
        SettingDefinitions.MaxPaginationSize.ShouldBe("MaxPaginationSize");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void MaxConcurrentQueriesHasExpectedValue()
    {
        SettingDefinitions.MaxConcurrentQueries.ShouldBe("MaxConcurrentQueries");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void MaxPreviewRowsHasExpectedValue()
    {
        SettingDefinitions.MaxPreviewRows.ShouldBe("MaxPreviewRows");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultTimeoutMsHasExpectedValue()
    {
        SettingDefinitions.DefaultTimeoutMs.ShouldBe("DefaultTimeoutMs");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void EnableLineageTrackingHasExpectedValue()
    {
        SettingDefinitions.EnableLineageTracking.ShouldBe("EnableLineageTracking");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void MaxRetryAttemptsHasExpectedValue()
    {
        SettingDefinitions.MaxRetryAttempts.ShouldBe("MaxRetryAttempts");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllDefinitionsAreDistinct()
    {
        // Arrange
        var allNames = new[]
        {
            SettingDefinitions.MaxPaginationSize,
            SettingDefinitions.MaxConcurrentQueries,
            SettingDefinitions.MaxPreviewRows,
            SettingDefinitions.DefaultTimeoutMs,
            SettingDefinitions.EnableLineageTracking,
            SettingDefinitions.MaxRetryAttempts
        };

        // Assert
        allNames.Distinct().Count().ShouldBe(allNames.Length);
    }
}
