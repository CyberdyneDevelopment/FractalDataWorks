using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Tests;

/// <summary>
/// Tests for <see cref="TransformContext.Reject"/> (FDW-1142): a rejected record is reported as an error like any other and
/// kept with the stage that rejected it and why, for the pipeline's reject steps to write.
/// </summary>
public sealed class TransformContextRejectTests
{
    private static TransformContext Context() =>
        new(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), fieldTransformChains: new Dictionary<Guid, IReadOnlyList<IFieldMappingTransform>>());

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ARejectedRecordIsReportedAsAnErrorAndKeptWithItsStageAndReason()
    {
        // Arrange
        var context = Context();
        var record = new Dictionary<string, object?> { ["Id"] = 7 };

        // Act
        context.Reject("Clean", "Record rejected by check 'Positive' of stage 'Clean'", record);

        // Assert
        context.Errors.ShouldHaveSingleItem().Message.ShouldContain("Positive");
        var rejected = context.Rejections.ShouldHaveSingleItem();
        rejected.Stage.ShouldBe("Clean");
        rejected.Reason.ShouldContain("Positive");
        rejected.Record.ShouldBeSameAs(record);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AnErrorThatIsNotARejectionIsNotKeptAsOne()
    {
        // Arrange
        var context = Context();

        // Act
        context.ReportError("lookup failed", new Dictionary<string, object?> { ["Id"] = 1 });

        // Assert
        context.Errors.Count.ShouldBe(1);
        context.Rejections.ShouldBeEmpty();
    }
}
