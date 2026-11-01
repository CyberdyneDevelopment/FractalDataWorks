using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Tests for <see cref="RejectedRows"/> (FDW-1142): a rejected record gains the reason and the stage, and a record that
/// already has either field is refused with a named result code, never quietly overwritten.
/// </summary>
public sealed class RejectedRowsTests
{
    private static RejectedRecord Rejected(string stage, string reason, params (string Field, object? Value)[] fields) =>
        new(stage, reason, fields.ToDictionary(f => f.Field, f => f.Value, StringComparer.Ordinal));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ARejectedRecordGainsTheReasonAndTheStageAndIsLeftUnchanged()
    {
        // Arrange
        var record = Rejected("Clean", "failed check 'Positive'", ("Id", 7));

        // Act
        var rows = RejectedRows.Of("copy", [record], NullLogger.Instance);

        // Assert
        rows.IsSuccess.ShouldBeTrue(rows.CurrentMessage);
        var row = rows.Value!.ShouldHaveSingleItem();
        row["Id"].ShouldBe(7);
        row[RejectedRecordColumns.Reason].ShouldBe("failed check 'Positive'");
        row[RejectedRecordColumns.Stage].ShouldBe("Clean");
        record.Record.Keys.ShouldBe(new[] { "Id" });
    }

    [Theory]
    [InlineData("RejectReason")]
    [InlineData("rejectstage")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARecordThatAlreadyHasAReservedFieldIsRefusedNamingTheFieldTheStageAndThePipeline(string field)
    {
        // Arrange
        var records = new[] { Rejected("Clean", "r", ("Id", 1)), Rejected("Branchy", "r", ("Id", 2), (field, "mine")) };

        // Act
        var rows = RejectedRows.Of("copy", records, NullLogger.Instance);

        // Assert
        rows.IsSuccess.ShouldBeFalse();
        rows.Code!.Id.ShouldBe(21002);
        rows.Details!.GetValue<string>("FieldName").ShouldBe(RejectedRecordColumns.All.Single(c => string.Equals(c, field, StringComparison.OrdinalIgnoreCase)));
        rows.Details!.GetValue<string>("StageName").ShouldBe("Branchy");
        rows.Details!.GetValue<string>("PipelineName").ShouldBe("copy");
        records[1].Record[field].ShouldBe("mine");
    }
}
