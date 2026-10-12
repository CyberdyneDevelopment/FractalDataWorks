using Fdw.Configuration;
using Microsoft.Extensions.Configuration;

namespace Fdw.Configuration.Tests;

/// <summary>
/// Tests for FdwConfigurationProvider class.
/// </summary>
public class FdwConfigurationProviderTests
{
    private class TestConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Constructor_WithValidParameters_CreatesProvider()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var loadHierarchy = () => new Dictionary<int, IDictionary<string, object>>();

        // Act
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Assert
        provider.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Constructor_WithNullSource_ThrowsArgumentNullException()
    {
        // Arrange
        var loadHierarchy = () => new Dictionary<int, IDictionary<string, object>>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FdwConfigurationProvider(null!, loadHierarchy))
            .ParamName.ShouldBe("source");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Constructor_WithNullLoadHierarchy_ThrowsArgumentNullException()
    {
        // Arrange
        var source = new TestConfigurationSource();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FdwConfigurationProvider(source, null!))
            .ParamName.ShouldBe("loadHierarchy");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Constructor_WithNullSectionName_UsesSemptyString()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["Key1"] = "Value1" }
        };
        var loadHierarchy = () => hierarchy;

        // Act
        var provider = new FdwConfigurationProvider(source, loadHierarchy, null!);
        provider.Load();

        // Assert
        provider.TryGet("Key1", out var value).ShouldBeTrue();
        value.ShouldBe("Value1");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Constructor_WithSectionName_StoresSectionName()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["Key1"] = "Value1" }
        };
        var loadHierarchy = () => hierarchy;

        // Act
        var provider = new FdwConfigurationProvider(source, loadHierarchy, "MySection");
        provider.Load();

        // Assert
        provider.TryGet("MySection:Key1", out var value).ShouldBeTrue();
        value.ShouldBe("Value1");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithEmptyHierarchy_LoadsNoData()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>();
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("anykey", out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithLevel0Data_LoadsDefaultConfiguration()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object>
            {
                ["Setting1"] = "DefaultValue1",
                ["Setting2"] = "DefaultValue2"
            }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("Setting1", out var value1).ShouldBeTrue();
        value1.ShouldBe("DefaultValue1");
        provider.TryGet("Setting2", out var value2).ShouldBeTrue();
        value2.ShouldBe("DefaultValue2");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithMultipleLevels_MergesInCorrectOrder()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["Key"] = "Default" },
            [1] = new Dictionary<string, object> { ["Key"] = "Application" },
            [2] = new Dictionary<string, object> { ["Key"] = "Tenant" },
            [3] = new Dictionary<string, object> { ["Key"] = "User" }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert - User level (3) should override all others
        provider.TryGet("Key", out var value).ShouldBeTrue();
        value.ShouldBe("User");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithTenantLevel_OverridesDefaultAndApplication()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["Key"] = "Default" },
            [1] = new Dictionary<string, object> { ["Key"] = "Application" },
            [2] = new Dictionary<string, object> { ["Key"] = "Tenant" }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("Key", out var value).ShouldBeTrue();
        value.ShouldBe("Tenant");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithApplicationLevel_OverridesDefault()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["Key"] = "Default" },
            [1] = new Dictionary<string, object> { ["Key"] = "Application" }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("Key", out var value).ShouldBeTrue();
        value.ShouldBe("Application");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_ExcludesMetadataColumns()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object>
            {
                ["Id"] = 1,
                ["Level"] = 0,
                ["TenantId"] = 100,
                ["UserId"] = 200,
                ["CreatedAt"] = DateTime.UtcNow,
                ["ModifiedAt"] = DateTime.UtcNow,
                ["ValidKey"] = "ValidValue"
            }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("Id", out _).ShouldBeFalse();
        provider.TryGet("Level", out _).ShouldBeFalse();
        provider.TryGet("TenantId", out _).ShouldBeFalse();
        provider.TryGet("UserId", out _).ShouldBeFalse();
        provider.TryGet("CreatedAt", out _).ShouldBeFalse();
        provider.TryGet("ModifiedAt", out _).ShouldBeFalse();
        provider.TryGet("ValidKey", out var value).ShouldBeTrue();
        value.ShouldBe("ValidValue");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithSectionName_PrefixesKeys()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["Key1"] = "Value1" }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy, "Section");

        // Act
        provider.Load();

        // Assert
        provider.TryGet("Section:Key1", out var value).ShouldBeTrue();
        value.ShouldBe("Value1");
        provider.TryGet("Key1", out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithNullValues_StoresEmptyString()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["NullKey"] = null! }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("NullKey", out var value).ShouldBeTrue();
        value.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithComplexObjects_ConvertsToString()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var testObject = new { Property = "Value" };
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["ObjectKey"] = testObject }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("ObjectKey", out var value).ShouldBeTrue();
        value.ShouldNotBeNull();
        value.ShouldBe(testObject.ToString());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_IsCaseInsensitive()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["MixedCaseKey"] = "Value" }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("MIXEDCASEKEY", out var value1).ShouldBeTrue();
        value1.ShouldBe("Value");
        provider.TryGet("mixedcasekey", out var value2).ShouldBeTrue();
        value2.ShouldBe("Value");
        provider.TryGet("MixedCaseKey", out var value3).ShouldBeTrue();
        value3.ShouldBe("Value");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithSkippedLevels_LoadsAvailableLevels()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["Key0"] = "Value0" },
            [2] = new Dictionary<string, object> { ["Key2"] = "Value2" }
            // Level 1 and 3 missing
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("Key0", out var value0).ShouldBeTrue();
        value0.ShouldBe("Value0");
        provider.TryGet("Key2", out var value2).ShouldBeTrue();
        value2.ShouldBe("Value2");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithDifferentKeysAtDifferentLevels_LoadsAll()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["DefaultKey"] = "DefaultValue" },
            [1] = new Dictionary<string, object> { ["AppKey"] = "AppValue" },
            [2] = new Dictionary<string, object> { ["TenantKey"] = "TenantValue" },
            [3] = new Dictionary<string, object> { ["UserKey"] = "UserValue" }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("DefaultKey", out var value0).ShouldBeTrue();
        value0.ShouldBe("DefaultValue");
        provider.TryGet("AppKey", out var value1).ShouldBeTrue();
        value1.ShouldBe("AppValue");
        provider.TryGet("TenantKey", out var value2).ShouldBeTrue();
        value2.ShouldBe("TenantValue");
        provider.TryGet("UserKey", out var value3).ShouldBeTrue();
        value3.ShouldBe("UserValue");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_CanBeCalledMultipleTimes()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var callCount = 0;
        Func<IDictionary<int, IDictionary<string, object>>> loadHierarchy = () =>
        {
            callCount++;
            return new Dictionary<int, IDictionary<string, object>>
            {
                [0] = new Dictionary<string, object> { ["Count"] = callCount.ToString() }
            };
        };
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();
        provider.TryGet("Count", out var firstValue).ShouldBeTrue();
        firstValue.ShouldBe("1");

        provider.Load();
        provider.TryGet("Count", out var secondValue).ShouldBeTrue();

        // Assert
        secondValue.ShouldBe("2");
        callCount.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Constructor_WithEmptySectionName_UsesEmptyString()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["Key"] = "Value" }
        };
        var loadHierarchy = () => hierarchy;

        // Act
        var provider = new FdwConfigurationProvider(source, loadHierarchy, "");
        provider.Load();

        // Assert
        provider.TryGet("Key", out var value).ShouldBeTrue();
        value.ShouldBe("Value");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithLevelsAbove3_IgnoresThem()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["Key0"] = "Value0" },
            [1] = new Dictionary<string, object> { ["Key1"] = "Value1" },
            [2] = new Dictionary<string, object> { ["Key2"] = "Value2" },
            [3] = new Dictionary<string, object> { ["Key3"] = "Value3" },
            [4] = new Dictionary<string, object> { ["Key4"] = "Value4" },
            [5] = new Dictionary<string, object> { ["Key5"] = "Value5" }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert - Levels 0-3 should be loaded
        provider.TryGet("Key0", out _).ShouldBeTrue();
        provider.TryGet("Key1", out _).ShouldBeTrue();
        provider.TryGet("Key2", out _).ShouldBeTrue();
        provider.TryGet("Key3", out _).ShouldBeTrue();
        // Levels 4-5 should be ignored
        provider.TryGet("Key4", out _).ShouldBeFalse();
        provider.TryGet("Key5", out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithNegativeLevels_IgnoresThem()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [-1] = new Dictionary<string, object> { ["NegativeKey"] = "NegativeValue" },
            [0] = new Dictionary<string, object> { ["Key0"] = "Value0" }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("NegativeKey", out _).ShouldBeFalse();
        provider.TryGet("Key0", out var value).ShouldBeTrue();
        value.ShouldBe("Value0");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithEmptyLevelData_DoesNotAddKeys()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object>(), // Empty
            [1] = new Dictionary<string, object> { ["Key1"] = "Value1" }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("Key1", out var value).ShouldBeTrue();
        value.ShouldBe("Value1");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithOnlyMetadataColumns_LoadsNothing()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object>
            {
                ["Id"] = 1,
                ["Level"] = 0,
                ["TenantId"] = 100,
                ["UserId"] = 200,
                ["CreatedAt"] = DateTime.UtcNow,
                ["ModifiedAt"] = DateTime.UtcNow
            }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert - No keys should be loaded
        provider.TryGet("Id", out _).ShouldBeFalse();
        provider.TryGet("Level", out _).ShouldBeFalse();
        provider.TryGet("TenantId", out _).ShouldBeFalse();
        provider.TryGet("UserId", out _).ShouldBeFalse();
        provider.TryGet("CreatedAt", out _).ShouldBeFalse();
        provider.TryGet("ModifiedAt", out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithDuplicateKeysInSameLevel_LastValueWins()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var levelData = new Dictionary<string, object>
        {
            ["Key"] = "FirstValue"
        };
        levelData["Key"] = "SecondValue"; // Overwrite
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = levelData
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("Key", out var value).ShouldBeTrue();
        value.ShouldBe("SecondValue");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithNumericValues_ConvertsToString()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object>
            {
                ["IntValue"] = 42,
                ["DoubleValue"] = 3.14,
                ["BoolValue"] = true,
                ["LongValue"] = 9999999999L
            }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("IntValue", out var intValue).ShouldBeTrue();
        intValue.ShouldBe("42");
        provider.TryGet("DoubleValue", out var doubleValue).ShouldBeTrue();
        doubleValue.ShouldBe("3.14");
        provider.TryGet("BoolValue", out var boolValue).ShouldBeTrue();
        boolValue.ShouldBe("True");
        provider.TryGet("LongValue", out var longValue).ShouldBeTrue();
        longValue.ShouldBe("9999999999");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithEmptyStringValue_PreservesEmptyString()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["EmptyKey"] = "" }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("EmptyKey", out var value).ShouldBeTrue();
        value.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithWhitespaceValue_PreservesWhitespace()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["WhitespaceKey"] = "   " }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("WhitespaceKey", out var value).ShouldBeTrue();
        value.ShouldBe("   ");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithSectionNameContainingColon_HandlesCorrectly()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["Key"] = "Value" }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy, "Section:Subsection");

        // Act
        provider.Load();

        // Assert
        provider.TryGet("Section:Subsection:Key", out var value).ShouldBeTrue();
        value.ShouldBe("Value");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithVariedCaseKeys_HandlesCorrectly()
    {
        // Arrange - Test that metadata exclusion is case-sensitive
        // but configuration lookup is case-insensitive
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object>
            {
                ["id"] = 1,                      // Lowercase "id", NOT metadata
                ["LEVEL"] = 0,                   // Uppercase "LEVEL", NOT metadata
                ["ValidKey"] = "ValidValue"
            }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert - Non-exact case metadata names are NOT excluded
        provider.TryGet("id", out var idValue).ShouldBeTrue();
        idValue.ShouldBe("1");
        provider.TryGet("LEVEL", out var levelValue).ShouldBeTrue();
        levelValue.ShouldBe("0");
        provider.TryGet("ValidKey", out var value).ShouldBeTrue();
        value.ShouldBe("ValidValue");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithPartialOverride_PreservesNonOverriddenKeys()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object>
            {
                ["Key1"] = "Default1",
                ["Key2"] = "Default2",
                ["Key3"] = "Default3"
            },
            [1] = new Dictionary<string, object>
            {
                ["Key2"] = "Application2" // Only override Key2
            }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("Key1", out var value1).ShouldBeTrue();
        value1.ShouldBe("Default1");
        provider.TryGet("Key2", out var value2).ShouldBeTrue();
        value2.ShouldBe("Application2");
        provider.TryGet("Key3", out var value3).ShouldBeTrue();
        value3.ShouldBe("Default3");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithAllLevelsHavingSameKey_UserLevelWins()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["Priority"] = "Default" },
            [1] = new Dictionary<string, object> { ["Priority"] = "Application" },
            [2] = new Dictionary<string, object> { ["Priority"] = "Tenant" },
            [3] = new Dictionary<string, object> { ["Priority"] = "User" }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("Priority", out var value).ShouldBeTrue();
        value.ShouldBe("User");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_ReplacesAllDataOnSubsequentCalls()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var firstLoad = true;
        Func<IDictionary<int, IDictionary<string, object>>> loadHierarchy = () =>
        {
            if (firstLoad)
            {
                firstLoad = false;
                return new Dictionary<int, IDictionary<string, object>>
                {
                    [0] = new Dictionary<string, object>
                    {
                        ["Key1"] = "FirstValue1",
                        ["Key2"] = "FirstValue2"
                    }
                };
            }
            else
            {
                return new Dictionary<int, IDictionary<string, object>>
                {
                    [0] = new Dictionary<string, object>
                    {
                        ["Key2"] = "SecondValue2",
                        ["Key3"] = "SecondValue3"
                    }
                };
            }
        };
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();
        provider.TryGet("Key1", out var firstKey1).ShouldBeTrue();
        firstKey1.ShouldBe("FirstValue1");

        provider.Load();

        // Assert - After reload, Key1 should not exist, Key2 should be updated, Key3 should be new
        provider.TryGet("Key1", out _).ShouldBeFalse();
        provider.TryGet("Key2", out var secondKey2).ShouldBeTrue();
        secondKey2.ShouldBe("SecondValue2");
        provider.TryGet("Key3", out var secondKey3).ShouldBeTrue();
        secondKey3.ShouldBe("SecondValue3");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithLargeNumberOfKeys_LoadsAllKeys()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var largeData = new Dictionary<string, object>();
        for (int i = 0; i < 1000; i++)
        {
            largeData[$"Key{i}"] = $"Value{i}";
        }
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = largeData
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert - Spot check several keys
        provider.TryGet("Key0", out var value0).ShouldBeTrue();
        value0.ShouldBe("Value0");
        provider.TryGet("Key500", out var value500).ShouldBeTrue();
        value500.ShouldBe("Value500");
        provider.TryGet("Key999", out var value999).ShouldBeTrue();
        value999.ShouldBe("Value999");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Constructor_StoresParameters()
    {
        // This test verifies the constructor properly stores all parameters
        // Even though we can't directly access private fields, we can verify behavior

        // Arrange
        var source = new TestConfigurationSource();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["Test"] = "Value" }
        };
        var loadHierarchy = () => hierarchy;

        // Act
        var provider = new FdwConfigurationProvider(source, loadHierarchy, "Section");
        provider.Load();

        // Assert - If parameters were stored correctly, this should work
        provider.TryGet("Section:Test", out var value).ShouldBeTrue();
        value.ShouldBe("Value");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithGuidValues_ConvertsToString()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var guidValue = Guid.NewGuid();
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["GuidKey"] = guidValue }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("GuidKey", out var value).ShouldBeTrue();
        value.ShouldBe(guidValue.ToString());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void Load_WithDateTimeValues_ConvertsToString()
    {
        // Arrange
        var source = new TestConfigurationSource();
        var dateValue = new DateTime(2025, 1, 15, 10, 30, 45);
        var hierarchy = new Dictionary<int, IDictionary<string, object>>
        {
            [0] = new Dictionary<string, object> { ["DateKey"] = dateValue }
        };
        var loadHierarchy = () => hierarchy;
        var provider = new FdwConfigurationProvider(source, loadHierarchy);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("DateKey", out var value).ShouldBeTrue();
        value.ShouldBe(dateValue.ToString());
    }
}
