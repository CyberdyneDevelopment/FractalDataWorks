using Fdw.Services.Quality.Abstractions.TypeCollections.QualitySeverityTypeOptions;

namespace Fdw.Services.Quality.Abstractions.Tests;

public class QualitySeverityTypesTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void AllReturnsAllSeverityTypes()
    {
        // Act
        var all = QualitySeverityTypes.All();

        // Assert
        all.ShouldNotBeEmpty();
        all.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ErrorSeverityTypeHasCorrectProperties()
    {
        // Act
        var severity = QualitySeverityTypes.ByName("Error");

        // Assert
        severity.ShouldNotBeNull();
        severity.Id.ShouldBe(1);
        severity.Name.ShouldBe("Error");
        severity.Priority.ShouldBe(1);
        severity.BlocksProcessing.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void WarningSeverityTypeHasCorrectProperties()
    {
        // Act
        var severity = QualitySeverityTypes.ByName("Warning");

        // Assert
        severity.ShouldNotBeNull();
        severity.Id.ShouldBe(2);
        severity.Name.ShouldBe("Warning");
        severity.Priority.ShouldBe(2);
        severity.BlocksProcessing.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void InfoSeverityTypeHasCorrectProperties()
    {
        // Act
        var severity = QualitySeverityTypes.ByName("Info");

        // Assert
        severity.ShouldNotBeNull();
        severity.Id.ShouldBe(3);
        severity.Name.ShouldBe("Info");
        severity.Priority.ShouldBe(3);
        severity.BlocksProcessing.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void AllSeverityTypesImplementInterface()
    {
        // Act
        var all = QualitySeverityTypes.All();

        // Assert
        foreach (var severity in all)
        {
            severity.ShouldBeAssignableTo<IQualitySeverityType>();
        }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void AllSeverityTypesHaveUniquePriorities()
    {
        // Act
        var all = QualitySeverityTypes.All();
        var priorities = all.Select(s => s.Priority).ToList();

        // Assert
        priorities.Count.ShouldBe(priorities.Distinct().Count());
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ErrorHasHighestPriority()
    {
        // Act
        var all = QualitySeverityTypes.All();
        var error = QualitySeverityTypes.ByName("Error");

        // Assert
        error.ShouldNotBeNull();
        error.Priority.ShouldBe(all.Min(s => s.Priority));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void InfoHasLowestPriority()
    {
        // Act
        var all = QualitySeverityTypes.All();
        var info = QualitySeverityTypes.ByName("Info");

        // Assert
        info.ShouldNotBeNull();
        info.Priority.ShouldBe(all.Max(s => s.Priority));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void OnlyErrorBlocksProcessing()
    {
        // Act
        var all = QualitySeverityTypes.All();

        // Assert
        var blockingTypes = all.Where(s => s.BlocksProcessing).ToList();
        blockingTypes.Count.ShouldBe(1);
        blockingTypes.Single().Name.ShouldBe("Error");
    }

    [Theory]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    [InlineData("Error", 1, true)]
    [InlineData("Warning", 2, false)]
    [InlineData("Info", 3, false)]
    public void SeverityTypeHasExpectedPriorityAndBlockingBehavior(
        string name,
        int expectedPriority,
        bool expectedBlocksProcessing)
    {
        // Act
        var severity = QualitySeverityTypes.ByName(name);

        // Assert
        severity.ShouldNotBeNull();
        severity.Priority.ShouldBe(expectedPriority);
        severity.BlocksProcessing.ShouldBe(expectedBlocksProcessing);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void PrioritiesAreSequential()
    {
        // Act
        var all = QualitySeverityTypes.All();
        var priorities = all.Select(s => s.Priority).OrderBy(p => p).ToList();

        // Assert
        for (int i = 0; i < priorities.Count; i++)
        {
            priorities[i].ShouldBe(i + 1);
        }
    }
}
