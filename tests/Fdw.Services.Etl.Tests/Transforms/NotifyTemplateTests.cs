using System;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="NotifyTemplate"/> (FDW-1120): the reserved variables of a run fill a Notify message, and a variable no
/// run has is refused rather than sent as written.
/// </summary>
public sealed class NotifyTemplateTests
{
    private static NotifyRunFacts Facts() =>
        new(Guid.NewGuid(), "NightlyCopy", Guid.Parse("11111111-2222-3333-4444-555555555555"), rowsExtracted: 120, rowsRejected: 7, rowsLoaded: 113,
            startedAt: new DateTimeOffset(2026, 10, 14, 19, 53, 57, TimeSpan.Zero),
            endedAt: new DateTimeOffset(2026, 10, 14, 19, 54, 2, TimeSpan.Zero),
            outcome: "Succeeded");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ExpandFillsEveryReservedVariable()
    {
        // Act
        var result = NotifyTemplate.Expand(
            "Alert",
            "{{PipelineName}} run {{ExecutionId}}: {{RowsExtracted}} read, {{RowsRejected}} rejected, {{RowsLoaded}} loaded",
            Facts(),
            NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBe("NightlyCopy run 11111111-2222-3333-4444-555555555555: 120 read, 7 rejected, 113 loaded");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ExpandReadsVariablesWithoutRegardToCaseOrInnerSpaces()
    {
        // Act
        var result = NotifyTemplate.Expand("Alert", "{{ pipelinename }} / {{ROWSLOADED}}", Facts(), NullLogger.Instance);

        // Assert
        result.Value.ShouldBe("NightlyCopy / 113");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ExpandRefusesAVariableNoRunHasAndNamesIt()
    {
        // Act
        var result = NotifyTemplate.Expand("Alert", "{{PipelineName}} lost {{RowsLost}} and {{Mood}}", Facts(), NullLogger.Instance);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("RowsLost");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Mood");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Alert");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void UnknownListsEachMissingNameOnceInOrderOfAppearance()
    {
        // Act
        var unknown = NotifyTemplate.Unknown("{{B}} {{PipelineName}} {{A}} {{b}}");

        // Assert
        unknown.ShouldBe(new[] { "B", "A" });
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ATextWithNoVariablesIsSentAsWritten()
    {
        // Act
        var result = NotifyTemplate.Expand("Alert", "the run is over", Facts(), NullLogger.Instance);

        // Assert
        result.Value.ShouldBe("the run is over");
    }
}
