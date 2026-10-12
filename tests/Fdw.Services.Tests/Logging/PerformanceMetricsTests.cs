using Fdw.Services.Logging;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Logging;

public class PerformanceMetricsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsAllProperties()
    {
        var metrics = new PerformanceMetrics(150.5, 42, "BulkInsert");

        metrics.Duration.ShouldBe(150.5);
        metrics.ItemsProcessed.ShouldBe(42);
        metrics.OperationType.ShouldBe("BulkInsert");
        metrics.SensitiveData.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithSensitiveDataSetsAllProperties()
    {
        var metrics = new PerformanceMetrics(200.0, 10, "Query", "secret-token");

        metrics.Duration.ShouldBe(200.0);
        metrics.ItemsProcessed.ShouldBe(10);
        metrics.OperationType.ShouldBe("Query");
        metrics.SensitiveData.ShouldBe("secret-token");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToStringExcludesSensitiveData()
    {
        var metrics = new PerformanceMetrics(100.0, 5, "Insert", "should-not-appear");

        var result = metrics.ToString();

        result.ShouldContain("100");
        result.ShouldContain("5");
        result.ShouldContain("Insert");
        result.ShouldNotContain("should-not-appear");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToStringFormatsCorrectly()
    {
        var metrics = new PerformanceMetrics(250.5, 100, "BulkUpdate");

        var result = metrics.ToString();

        result.ShouldBe("Duration: 250.5ms, Items: 100, Type: BulkUpdate");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToStringWithZeroValuesFormatsCorrectly()
    {
        var metrics = new PerformanceMetrics(0, 0, "NoOp");

        var result = metrics.ToString();

        result.ShouldBe("Duration: 0ms, Items: 0, Type: NoOp");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RecordEqualityWithSameValuesReturnsTrue()
    {
        var metrics1 = new PerformanceMetrics(100.0, 5, "Query");
        var metrics2 = new PerformanceMetrics(100.0, 5, "Query");

        metrics1.ShouldBe(metrics2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RecordEqualityWithDifferentValuesReturnsFalse()
    {
        var metrics1 = new PerformanceMetrics(100.0, 5, "Query");
        var metrics2 = new PerformanceMetrics(200.0, 5, "Query");

        metrics1.ShouldNotBe(metrics2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RecordWithExpressionCreatesModifiedCopy()
    {
        var original = new PerformanceMetrics(100.0, 5, "Query");
        var modified = original with { Duration = 200.0 };

        modified.Duration.ShouldBe(200.0);
        modified.ItemsProcessed.ShouldBe(5);
        modified.OperationType.ShouldBe("Query");
        original.Duration.ShouldBe(100.0);
    }
}
