using Fdw.Commands.Abstractions;

namespace Fdw.Commands.Abstractions.Tests;

public sealed class TranslationCapabilitiesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FullCapabilitiesHasAllFeaturesEnabled()
    {
        // Act
        var capabilities = TranslationCapabilities.Full;

        // Assert
        capabilities.SupportsProjection.ShouldBeTrue();
        capabilities.SupportsFiltering.ShouldBeTrue();
        capabilities.SupportsOrdering.ShouldBeTrue();
        capabilities.SupportsPaging.ShouldBeTrue();
        capabilities.SupportsJoins.ShouldBeTrue();
        capabilities.SupportsGrouping.ShouldBeTrue();
        capabilities.SupportsAggregation.ShouldBeTrue();
        capabilities.SupportsSubqueries.ShouldBeTrue();
        capabilities.SupportsTransactions.ShouldBeTrue();
        capabilities.SupportsBulkOperations.ShouldBeTrue();
        capabilities.SupportsParameterization.ShouldBeTrue();
        capabilities.MaxComplexityLevel.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void BasicCapabilitiesHasLimitedFeatures()
    {
        // Act
        var capabilities = TranslationCapabilities.Basic;

        // Assert
        capabilities.SupportsProjection.ShouldBeTrue();
        capabilities.SupportsFiltering.ShouldBeTrue();
        capabilities.SupportsOrdering.ShouldBeTrue();
        capabilities.SupportsPaging.ShouldBeFalse();
        capabilities.SupportsJoins.ShouldBeFalse();
        capabilities.SupportsGrouping.ShouldBeFalse();
        capabilities.SupportsAggregation.ShouldBeFalse();
        capabilities.SupportsSubqueries.ShouldBeFalse();
        capabilities.SupportsTransactions.ShouldBeFalse();
        capabilities.SupportsBulkOperations.ShouldBeFalse();
        capabilities.SupportsParameterization.ShouldBeTrue();
        capabilities.MaxComplexityLevel.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void DefaultMaxComplexityLevelIsFive()
    {
        // Act
        var capabilities = new TranslationCapabilities();

        // Assert
        capabilities.MaxComplexityLevel.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CanCreateCustomCapabilities()
    {
        // Act
        var capabilities = new TranslationCapabilities
        {
            SupportsProjection = true,
            SupportsFiltering = true,
            SupportsOrdering = false,
            SupportsPaging = true,
            SupportsJoins = false,
            SupportsGrouping = false,
            SupportsAggregation = false,
            SupportsSubqueries = false,
            SupportsTransactions = false,
            SupportsBulkOperations = false,
            SupportsParameterization = true,
            MaxComplexityLevel = 7
        };

        // Assert
        capabilities.SupportsProjection.ShouldBeTrue();
        capabilities.SupportsFiltering.ShouldBeTrue();
        capabilities.SupportsOrdering.ShouldBeFalse();
        capabilities.SupportsPaging.ShouldBeTrue();
        capabilities.SupportsJoins.ShouldBeFalse();
        capabilities.MaxComplexityLevel.ShouldBe(7);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AllPropertiesDefaultToFalse()
    {
        // Act
        var capabilities = new TranslationCapabilities();

        // Assert
        capabilities.SupportsProjection.ShouldBeFalse();
        capabilities.SupportsFiltering.ShouldBeFalse();
        capabilities.SupportsOrdering.ShouldBeFalse();
        capabilities.SupportsPaging.ShouldBeFalse();
        capabilities.SupportsJoins.ShouldBeFalse();
        capabilities.SupportsGrouping.ShouldBeFalse();
        capabilities.SupportsAggregation.ShouldBeFalse();
        capabilities.SupportsSubqueries.ShouldBeFalse();
        capabilities.SupportsTransactions.ShouldBeFalse();
        capabilities.SupportsBulkOperations.ShouldBeFalse();
        capabilities.SupportsParameterization.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CanSetAllPropertiesToTrue()
    {
        // Act
        var capabilities = new TranslationCapabilities
        {
            SupportsProjection = true,
            SupportsFiltering = true,
            SupportsOrdering = true,
            SupportsPaging = true,
            SupportsJoins = true,
            SupportsGrouping = true,
            SupportsAggregation = true,
            SupportsSubqueries = true,
            SupportsTransactions = true,
            SupportsBulkOperations = true,
            SupportsParameterization = true
        };

        // Assert
        capabilities.SupportsProjection.ShouldBeTrue();
        capabilities.SupportsFiltering.ShouldBeTrue();
        capabilities.SupportsOrdering.ShouldBeTrue();
        capabilities.SupportsPaging.ShouldBeTrue();
        capabilities.SupportsJoins.ShouldBeTrue();
        capabilities.SupportsGrouping.ShouldBeTrue();
        capabilities.SupportsAggregation.ShouldBeTrue();
        capabilities.SupportsSubqueries.ShouldBeTrue();
        capabilities.SupportsTransactions.ShouldBeTrue();
        capabilities.SupportsBulkOperations.ShouldBeTrue();
        capabilities.SupportsParameterization.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CanSetMaxComplexityLevelToZero()
    {
        // Act
        var capabilities = new TranslationCapabilities
        {
            MaxComplexityLevel = 0
        };

        // Assert
        capabilities.MaxComplexityLevel.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CanSetMaxComplexityLevelToMax()
    {
        // Act
        var capabilities = new TranslationCapabilities
        {
            MaxComplexityLevel = int.MaxValue
        };

        // Assert
        capabilities.MaxComplexityLevel.ShouldBe(int.MaxValue);
    }
}
