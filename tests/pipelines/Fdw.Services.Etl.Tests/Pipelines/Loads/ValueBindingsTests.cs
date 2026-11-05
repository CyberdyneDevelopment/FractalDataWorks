using Fdw.Commands.Data.ValueSources;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Pipelines.Loads.ValueSources;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Tests for <see cref="ValueBindings"/> and the value source factories (FDW-1109): a configuration row becomes the
/// command layer's value source, and only the kinds the engine can put in a row are put there.
/// </summary>
public sealed class ValueBindingsTests
{
    private static PipelineLoadValueSourceConfiguration Constant(string column, string value, string type) =>
        new() { Name = column, ColumnName = column, SourceKind = "Constant", ConstantValue = value, ConstantDataType = type };

    private static PipelineLoadValueSourceConfiguration Stamp(string column) =>
        new() { Name = column, ColumnName = column, SourceKind = "CurrentTimestamp" };

    private static PipelineLoadValueSourceConfiguration Sequence(string column) =>
        new()
        {
            Name = column,
            ColumnName = column,
            SourceKind = "NextSequenceValue",
            SequenceName = "OrderNumbers",
            SequenceSchema = "seq",
            SequenceDatabase = "Other",
            SequenceOrderColumns =
            [
                new PipelineLoadColumnConfiguration { Name = "b", Role = "SequenceOrder", ColumnName = "Created", Ordinal = 1 },
                new PipelineLoadColumnConfiguration { Name = "a", Role = "SequenceOrder", ColumnName = "Region", Ordinal = 0 },
            ],
        };

    private static PipelineLoadValueSourceConfiguration Uuid(string column) =>
        new() { Name = column, ColumnName = column, SourceKind = "UuidV7" };

    private static PipelineLoadValueSourceConfiguration FromField(string column, string field) =>
        new() { Name = column, ColumnName = column, SourceKind = "Column", SourceField = field };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void BuildMakesTheCommandLayersValueSourceOfEachKindInOrder()
    {
        // Act
        var result = ValueBindings.Build(
            [Constant("Batch", "7", "Int64"), Stamp("LoadedAt"), Sequence("Number"), FromField("Region", "SourceRegion")], "test", NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var bindings = result.Value!;
        bindings.Select(b => b.Column).ShouldBe(new[] { "Batch", "LoadedAt", "Number", "Region" });
        bindings[0].Source.ShouldBeOfType<ConstantValueSource>().Value.ShouldBe(7L);
        bindings[1].Source.ShouldBeOfType<CurrentTimestampValueSource>();
        var sequence = bindings[2].Source.ShouldBeOfType<NextSequenceValueSource>();
        sequence.SequenceName.ShouldBe("OrderNumbers");
        sequence.Schema.ShouldBe("seq");
        sequence.Database.ShouldBe("Other");
        sequence.OrderBy.ShouldBe(new[] { "Region", "Created" });
        bindings[3].Source.ShouldBeOfType<ColumnReferenceValueSource>().SourceField.ShouldBe("SourceRegion");
    }

    [Theory]
    [InlineData("Int64", "ten")]
    [InlineData("Bool", "maybe")]
    [InlineData("DateTimeOffset", "soon")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void BuildRefusesAConstantThatIsNotAValueOfItsType(string type, string value)
    {
        // Act
        var result = ValueBindings.Build([Constant("Batch", value, type)], "load step 'x'", NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20211");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Batch");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void BuildRefusesAKindThatNoFactoryBuilds()
    {
        // Act
        var result = ValueBindings.Build(
            [new PipelineLoadValueSourceConfiguration { Name = "x", ColumnName = "X", SourceKind = "Telepathy" }], "test", NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20212");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void RowValuesGivesAConstantItsValueAndATimestampTheMomentTheLoadBegan()
    {
        // Arrange
        var bindings = ValueBindings.Build([Constant("Batch", "7", "Int64"), Stamp("LoadedAt")], "test", NullLogger.Instance).Value!;
        var now = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);

        // Act
        var result = ValueBindings.RowValues(bindings, "test", "Insert", replacesRows: false, now, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Select(p => (p.Key, p.Value())).ShouldBe(new[] { ("Batch", (object?)7L), ("LoadedAt", now) });
    }

    [Theory]
    [InlineData("NextSequenceValue")]
    [InlineData("Column")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RowValuesRefusesAKindTheDatabaseMustSupplySetBased(string kind)
    {
        // Arrange
        var source = kind == "Column" ? FromField("Region", "SourceRegion") : Sequence("Number");
        var bindings = ValueBindings.Build([source], "test", NullLogger.Instance).Value!;

        // Act
        var result = ValueBindings.RowValues(bindings, "load step 'x'", "Insert", replacesRows: false, DateTimeOffset.UtcNow, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20213");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Insert");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void BuildMakesAUuidV7ValueSourceThatStatesNothingElse()
    {
        // Act
        var result = ValueBindings.Build([Uuid("PublicId")], "test", NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Single().Column.ShouldBe("PublicId");
        result.Value!.Single().Source.ShouldBeOfType<UuidV7ValueSource>().Kind.ShouldBe("UuidV7");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void RowValuesGivesAUuidV7ProducerThatMintsANewVersionSevenGuidForEachCall()
    {
        // Arrange
        var bindings = ValueBindings.Build([Uuid("PublicId")], "test", NullLogger.Instance).Value!;

        // Act
        var result = ValueBindings.RowValues(bindings, "test", "Insert", replacesRows: false, DateTimeOffset.UtcNow, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var produce = result.Value!.Single().Value;
        var ids = Enumerable.Range(0, 200).Select(_ => produce().ShouldBeOfType<Guid>()).ToList();
        ids.Distinct().Count().ShouldBe(200);
        ids.ShouldAllBe(id => id.Version == 7);
        ids.ShouldAllBe(id => id != Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RowValuesRefusesAUuidV7ForALoadThatReplacesRowsThatExist()
    {
        // Arrange
        var bindings = ValueBindings.Build([Uuid("PublicId")], "test", NullLogger.Instance).Value!;

        // Act
        var result = ValueBindings.RowValues(bindings, "load step 'x'", "Update", replacesRows: true, DateTimeOffset.UtcNow, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20213");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("PublicId");
        result.CurrentMessage.ShouldContain("UuidV7");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void OnlyAUuidV7FactoryIsInsertOnly()
    {
        foreach (var factory in ValueSourceFactories.All())
        {
            factory.InsertOnly.ShouldBe(factory.Name == "UuidV7", factory.Name);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void EveryValueSourceKindHasAFactoryOfTheSameName()
    {
        foreach (var kind in ValueSourceKinds.All())
        {
            ReferenceEquals(ValueSourceFactories.ByName(kind.Name), ValueSourceFactories.NotFound).ShouldBeFalse($"no factory for value source kind {kind.Name}");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void OnlyConstantAndCurrentTimestampCanBeGivenToRows()
    {
        ValueSourceFactories.ByName("Constant").SuppliesRowValue.ShouldBeTrue();
        ValueSourceFactories.ByName("CurrentTimestamp").SuppliesRowValue.ShouldBeTrue();
        ValueSourceFactories.ByName("NextSequenceValue").SuppliesRowValue.ShouldBeFalse();
        ValueSourceFactories.ByName("Column").SuppliesRowValue.ShouldBeFalse();
    }
}
