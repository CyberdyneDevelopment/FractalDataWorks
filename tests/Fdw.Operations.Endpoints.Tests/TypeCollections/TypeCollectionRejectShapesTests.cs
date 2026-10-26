using System.Text.Json;
using Fdw.Collections;
using Fdw.Operations.Endpoints.TypeCollections;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.TypeCollections;

/// <summary>
/// Tests that the type-collection value summary lists, as data the console reads, whether a load step kind can write rejected
/// records and which columns the Reject load phase reserves (FDW-1148), and that an option that says neither lists neither.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Operations")]
public class TypeCollectionRejectShapesTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void AKindThatWritesRejectedRecordsSaysSo()
    {
        TypeCollectionResolver.ToSummary(new InsertLoadStepKind()).AcceptsRejectedRecords.ShouldBe(true);
    }

    [Fact]
    public void AKindThatDoesNotWriteRejectedRecordsSaysSo()
    {
        TypeCollectionResolver.ToSummary(new UpsertLoadStepKind()).AcceptsRejectedRecords.ShouldBe(false);
    }

    [Fact]
    public void TheRejectPhaseListsTheColumnsItReserves()
    {
        TypeCollectionResolver.ToSummary(new RejectLoadPhase()).ReservedColumns.ShouldNotBeNull().ShouldBe(new[] { "RejectReason", "RejectStage" });
    }

    [Fact]
    public void TheLoadPhaseReservesNoColumns()
    {
        TypeCollectionResolver.ToSummary(new LoadLoadPhase()).ReservedColumns.ShouldNotBeNull().ShouldBeEmpty();
    }

    [Fact]
    public void AnOptionThatSaysNeitherListsNeither()
    {
        var summary = TypeCollectionResolver.ToSummary(new PlainOption());

        summary.AcceptsRejectedRecords.ShouldBeNull();
        summary.ReservedColumns.ShouldBeNull();
    }

    [Fact]
    public void TheFactsAreListedByTheirNamesInTheJson()
    {
        var kind = JsonSerializer.Serialize(TypeCollectionResolver.ToSummary(new InsertLoadStepKind()), Web);
        var phase = JsonSerializer.Serialize(TypeCollectionResolver.ToSummary(new RejectLoadPhase()), Web);

        kind.ShouldContain("\"acceptsRejectedRecords\":true");
        phase.ShouldContain("\"reservedColumns\":[\"RejectReason\",\"RejectStage\"]");
    }

    private sealed class PlainOption : ITypeOption
    {
        public string Name => "Plain";

        public string Category => "Routing";
    }
}
