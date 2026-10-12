using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Calculations.Abstractions.Trees;
using Shouldly;
using Xunit;
using static Fdw.Services.Calculations.Abstractions.Fluency.Calc;

namespace Fdw.Data.Transformations.Tests;

/// <summary>
/// FDW-1231: a calculation set is stored as rows and read back as the same set, for every kind of row source, column and
/// derived value, and rows that cannot make a set are refused with a code.
/// </summary>
public sealed class BuiltTransformationSetStorageTests
{
    private static readonly CalculationLimits Limits = new(200, 5000, 32);

    private static CalculationSetDefinition Everything() => new(
        "Everything",
        [
            new TableInputDefinition("Trades", [new TableColumnDefinition("Counterparty", "Text"), new TableColumnDefinition("Notional", "Number")]),
            new TableInputDefinition("Rates", [new TableColumnDefinition("Region", "Text"), new TableColumnDefinition("Rate", "Number")]),
        ],
        [
            new SumValue("totalNotional", "Trades", "Notional"),
            new LookupValue("rate", "Rates", "Region", "Region", "Rate"),
        ],
        [
            new TableOutputDefinition(
                "Settled",
                new EachRowSource("Trades", GreaterOrEqual(Field("Notional"), Constant(1000m))),
                [new CopyColumn("Counterparty", "Counterparty"), new LiteralColumn("FeeName", "Commission"), new FormulaColumn("Amount", Multiply(Field("Notional"), Field("rate")))]),
            new TableOutputDefinition(
                "Schedule",
                new CountSource(Field("months"), "N", "Trades"),
                [new FormulaColumn("Balance", Subtract(Field("principal"), Field("N")))]),
            new TableOutputDefinition(
                "Statement",
                new EachGroupSource("Trades", ["Counterparty"]),
                [new CopyColumn("Counterparty", "Counterparty"), new LiteralColumn("Rank", 1), new LiteralColumn("Open", true), new FormulaColumn("Total", Field("totalNotional"))]),
        ]);

    private static BuiltTransformationConfiguration Stored()
    {
        var built = new BuiltTransformationConfiguration { Name = "Everything" };
        BuiltTransformationSetWriter.Apply(built, Everything()).IsSuccess.ShouldBeTrue();
        return built;
    }

    [Fact]
    public void ASetWrittenAsRowsIsReadBackAsTheSameSet()
    {
        var read = BuiltTransformationSetReader.ToDefinition(Stored(), Limits);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        var set = read.Value!;
        set.TableInputs.Select(table => table.Name).ShouldBe(new[] { "Trades", "Rates" });
        set.TableInputs[1].Columns.Select(column => $"{column.Name}:{column.ValueType}").ShouldBe(new[] { "Region:Text", "Rate:Number" });
        set.Derived.Select(value => $"{value.Kind}:{value.Name}:{value.Table}:{value.Column}").ShouldBe(new[] { "Sum:totalNotional:Trades:Notional", "Lookup:rate:Rates:Rate" });
        set.Derived[1].KeyColumn.ShouldBe("Region");
        set.Derived[1].MatchField.ShouldBe("Region");
        set.Outputs.Select(output => output.Rows.Kind).ShouldBe(new[] { "Each", "Count", "EachGroup" });
    }

    [Fact]
    public void EveryRowSourceKeepsWhatItNeeds()
    {
        var set = BuiltTransformationSetReader.ToDefinition(Stored(), Limits).Value!;

        set.Outputs[0].Rows.TableName.ShouldBe("Trades");
        set.Outputs[0].Rows.Condition!.Op.ShouldBe("GreaterOrEqual");
        set.Outputs[1].Rows.IndexColumn.ShouldBe("N");
        set.Outputs[1].Rows.TableName.ShouldBe("Trades");
        set.Outputs[1].Rows.CountFormula!.Name.ShouldBe("months");
        set.Outputs[2].Rows.GroupBy.ShouldBe(new[] { "Counterparty" });
    }

    [Fact]
    public void EveryColumnKindKeepsItsValueAndItsType()
    {
        var set = BuiltTransformationSetReader.ToDefinition(Stored(), Limits).Value!;

        var settled = set.Outputs[0].Columns;
        settled.Select(column => column.Kind).ShouldBe(new[] { "Copy", "Literal", "Formula" });
        settled[0].Source.ShouldBe("Counterparty");
        settled[1].Literal.ShouldBe("Commission");
        settled[2].Formula!.Op.ShouldBe("Multiply");
        var statement = set.Outputs[2].Columns;
        statement[1].Literal.ShouldBe(1m);
        statement[2].Literal.ShouldBe(true);
    }

    [Fact]
    public void AnOutputWithoutAConditionReadsBackWithoutOne()
    {
        var built = new BuiltTransformationConfiguration { Name = "Plain" };
        BuiltTransformationSetWriter.Apply(built, new CalculationSetDefinition(
            "Plain",
            [new TableInputDefinition("T", [new TableColumnDefinition("A", "Number")])],
            [],
            [new TableOutputDefinition("O", new EachRowSource("T"), [new CopyColumn("A", "A")])]));

        var set = BuiltTransformationSetReader.ToDefinition(built, Limits).Value!;

        set.Outputs[0].Rows.Condition.ShouldBeNull();
        built.TableOutputNodes.ShouldBeEmpty();
    }

    [Fact]
    public void RowsThatCannotMakeASetAreRefusedWithACode()
    {
        var unknownKind = Stored();
        unknownKind.DerivedValues[0].Kind = "Median";
        var noKey = Stored();
        noKey.DerivedValues[1].KeyColumn = null;
        var noFormula = Stored();
        noFormula.TableOutputNodes.Clear();
        var badLiteral = Stored();
        badLiteral.TableOutputColumns.First(column => column.Name == "Rank").LiteralText = "one";

        BuiltTransformationSetReader.ToDefinition(unknownKind, Limits).Code!.Name.ShouldBe("SetDefinitionInvalid");
        BuiltTransformationSetReader.ToDefinition(noKey, Limits).Code!.Name.ShouldBe("SetDefinitionInvalid");
        BuiltTransformationSetReader.ToDefinition(noFormula, Limits).Code!.Name.ShouldBe("StoredFormulaInvalid");
        BuiltTransformationSetReader.ToDefinition(badLiteral, Limits).Code!.Name.ShouldBe("SetDefinitionInvalid");
    }
}
