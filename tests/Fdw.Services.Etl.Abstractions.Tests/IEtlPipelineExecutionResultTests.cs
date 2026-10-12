using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Abstractions.Tests;

/// <summary>
/// Tests for IEtlPipelineExecutionResult interface contract.
/// </summary>
public class IEtlPipelineExecutionResultTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ExecutionIdPropertyCanBeRead()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var result = new TestEtlPipelineExecutionResult { ExecutionId = expectedId };

        // Act
        var actual = result.ExecutionId;

        // Assert
        actual.ShouldBe(expectedId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IsSuccessPropertyCanBeRead()
    {
        // Arrange
        var result = new TestEtlPipelineExecutionResult { IsSuccess = true };

        // Act
        var actual = result.IsSuccess;

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RecordsExtractedPropertyCanBeRead()
    {
        // Arrange
        const int expected = 100;
        var result = new TestEtlPipelineExecutionResult { RecordsExtracted = expected };

        // Act
        var actual = result.RecordsExtracted;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RecordsTransformedPropertyCanBeRead()
    {
        // Arrange
        const int expected = 95;
        var result = new TestEtlPipelineExecutionResult { RecordsTransformed = expected };

        // Act
        var actual = result.RecordsTransformed;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RecordsLoadedPropertyCanBeRead()
    {
        // Arrange
        const int expected = 90;
        var result = new TestEtlPipelineExecutionResult { RecordsLoaded = expected };

        // Act
        var actual = result.RecordsLoaded;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RecordsFailedPropertyCanBeRead()
    {
        // Arrange
        const int expected = 10;
        var result = new TestEtlPipelineExecutionResult { RecordsFailed = expected };

        // Act
        var actual = result.RecordsFailed;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ExtractDurationPropertyCanBeRead()
    {
        // Arrange
        var expected = TimeSpan.FromSeconds(5);
        var result = new TestEtlPipelineExecutionResult { ExtractDuration = expected };

        // Act
        var actual = result.ExtractDuration;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TransformDurationPropertyCanBeRead()
    {
        // Arrange
        var expected = TimeSpan.FromSeconds(10);
        var result = new TestEtlPipelineExecutionResult { TransformDuration = expected };

        // Act
        var actual = result.TransformDuration;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void LoadDurationPropertyCanBeRead()
    {
        // Arrange
        var expected = TimeSpan.FromSeconds(8);
        var result = new TestEtlPipelineExecutionResult { LoadDuration = expected };

        // Act
        var actual = result.LoadDuration;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TotalDurationPropertyCanBeRead()
    {
        // Arrange
        var expected = TimeSpan.FromSeconds(23);
        var result = new TestEtlPipelineExecutionResult { TotalDuration = expected };

        // Act
        var actual = result.TotalDuration;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void StartedAtPropertyCanBeRead()
    {
        // Arrange
        var expected = DateTime.UtcNow;
        var result = new TestEtlPipelineExecutionResult { StartedAt = expected };

        // Act
        var actual = result.StartedAt;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CompletedAtPropertyCanBeReadWhenSet()
    {
        // Arrange
        var expected = DateTime.UtcNow;
        var result = new TestEtlPipelineExecutionResult { CompletedAt = expected };

        // Act
        var actual = result.CompletedAt;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CompletedAtPropertyCanBeNull()
    {
        // Arrange
        var result = new TestEtlPipelineExecutionResult { CompletedAt = null };

        // Act
        var actual = result.CompletedAt;

        // Assert
        actual.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ErrorsPropertyCanBeRead()
    {
        // Arrange
        var expected = new List<string> { "Error1", "Error2" };
        var result = new TestEtlPipelineExecutionResult { Errors = expected };

        // Act
        var actual = result.Errors;

        // Assert
        actual.ShouldBe(expected);
        actual.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ErrorsPropertyCanBeEmpty()
    {
        // Arrange
        var result = new TestEtlPipelineExecutionResult { Errors = Array.Empty<string>() };

        // Act
        var actual = result.Errors;

        // Assert
        actual.ShouldNotBeNull();
        actual.Count.ShouldBe(0);
    }

    /// <summary>
    /// Test implementation of IEtlPipelineExecutionResult.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private sealed class TestEtlPipelineExecutionResult : IEtlPipelineExecutionResult
    {
        public Guid ExecutionId { get; set; }
        public bool IsSuccess { get; set; }
        public int RecordsExtracted { get; set; }
        public int RecordsTransformed { get; set; }
        public int RecordsLoaded { get; set; }
        public int RecordsFailed { get; set; }
        public TimeSpan ExtractDuration { get; set; }
        public TimeSpan TransformDuration { get; set; }
        public TimeSpan LoadDuration { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
    }
}
