using System.Reflection;
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
    public void EverySettingNameIsDefinedWithADescription()
    {
        // Arrange: every public name constant on SettingDefinitions is a setting the platform recognizes.
        var names = typeof(SettingDefinitions)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        // Act
        var undefined = names.Where(n => !SettingDefinitions.TryGet(n, out _)).ToList();
        var undescribed = names
            .Where(n => SettingDefinitions.TryGet(n, out var definition) && string.IsNullOrWhiteSpace(definition.Description))
            .ToList();

        // Assert
        names.ShouldNotBeEmpty();
        undefined.ShouldBeEmpty("every name constant needs an entry in the registry");
        undescribed.ShouldBeEmpty("every setting needs a plain sentence saying what it does");
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
            SettingDefinitions.MaxCalculatedFilterRows,
            SettingDefinitions.MaxConcurrentQueries,
            SettingDefinitions.MaxPreviewRows,
            SettingDefinitions.DefaultTimeoutMs,
            SettingDefinitions.EnableLineageTracking,
            SettingDefinitions.MaxRetryAttempts
        };

        // Assert
        allNames.Distinct().Count().ShouldBe(allNames.Length);
    }

    [Theory]
    [InlineData("ApiKeyDefaultPaginationSize")]
    [InlineData("ApiKeyMaxPaginationSize")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TheApiKeyPagingSettingsAreRegisteredAsInt32WithADescription(string name)
    {
        // Act
        var known = SettingDefinitions.TryGet(name, out var definition);

        // Assert
        known.ShouldBeTrue();
        definition.DataType.ShouldBe("Int32");
        definition.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TheApiKeyPagingSettingsHaveExpectedNames()
    {
        SettingDefinitions.ApiKeyDefaultPaginationSize.ShouldBe("ApiKeyDefaultPaginationSize");
        SettingDefinitions.ApiKeyMaxPaginationSize.ShouldBe("ApiKeyMaxPaginationSize");
    }
}
