using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Which transforms need the whole input decides whether <c>BatchCopyPipeline</c> may hand records on
/// page by page (FDW-946). Getting it wrong for an aggregate would give per-page groups instead of
/// whole-set groups, so the answer is pinned per transform.
/// </summary>
public sealed class RequiresCompleteInputTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AggregateRequiresCompleteInput()
    {
        // Act
        var requires = new AggregateTransformType().RequiresCompleteInput;

        // Assert
        requires.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RowAtATimeTransformsDoNotRequireCompleteInput()
    {
        // Act / Assert
        new MapTransformType().RequiresCompleteInput.ShouldBeFalse();
        new FilterTransformType().RequiresCompleteInput.ShouldBeFalse();
        new CalculateTransformType().RequiresCompleteInput.ShouldBeFalse();
        new LookupTransformType().RequiresCompleteInput.ShouldBeFalse();
    }
}
