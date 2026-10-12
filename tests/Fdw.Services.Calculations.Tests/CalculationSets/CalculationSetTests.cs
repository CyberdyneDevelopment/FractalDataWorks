using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Calculations.Sets;
using Shouldly;
using Xunit;
using static Fdw.Services.Calculations.Abstractions.Fluency.Calc;

namespace Fdw.Services.Calculations.Tests.CalculationSets;

/// <summary>
/// FDW-1227: a calculation set reads tables as name-to-value rows and writes different tables, whatever the source. These are
/// the worked examples of the design: filter and reshape, a data-driven count, groups with totals, a lookup, and several
/// outputs. The numbers are worked by hand.
/// </summary>
public class CalculationSetTests
{
    private static readonly CalculationLimits Formulas = new(256, 5000, 64);
    private static readonly SetLimits Rows = new(1000, 1000);
    private static readonly IReadOnlyDictionary<string, object?> NoScalars = new Dictionary<string, object?>();

    private static Dictionary<string, object?> Row(params (string Name, object? Value)[] values) =>
        values.ToDictionary(pair => pair.Name, pair => pair.Value);

    private static Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> Tables(
        params (string Name, IReadOnlyDictionary<string, object?>[] Rows)[] tables) =>
        tables.ToDictionary(table => table.Name, table => (IReadOnlyList<IReadOnlyDictionary<string, object?>>)table.Rows);

    private static ICompiledCalculationSet Compile(CalculationSetDefinition definition)
    {
        var compiled = new CalculationSetCompiler().Compile(definition, Formulas, Rows);
        compiled.IsSuccess.ShouldBeTrue(compiled.CurrentMessage);
        return compiled.Value!;
    }

    private static CalculationSetDefinition Set(
        IReadOnlyList<TableInputDefinition> tables, IReadOnlyList<DerivedValueDefinition> derived, params TableOutputDefinition[] outputs) =>
        new("test", tables, derived, outputs);

    private static TableInputDefinition Table(string name, params string[] columns) =>
        new(name, [.. columns.Select(column => new TableColumnDefinition(column, "Number"))]);

    [Fact]
    public void EachRowKeepsOnlyTheRowsTheConditionAcceptsAndReshapesThem()
    {
        var set = Compile(Set([Table("Trades", "TradeId", "Qty", "Price")], [], new TableOutputDefinition(
            "Settled",
            new EachRowSource("Trades", GreaterOrEqual(Multiply(Field("Qty"), Field("Price")), Constant(1000m))),
            [new CopyColumn("TradeId", "TradeId"), new FormulaColumn("Notional", Round(Multiply(Field("Qty"), Field("Price")), 2, "AwayFromZero"))])));

        var result = set.Evaluate(NoScalars, Tables(("Trades", [
            Row(("TradeId", "T1"), ("Qty", 100m), ("Price", 9.99m)),
            Row(("TradeId", "T2"), ("Qty", 250m), ("Price", 4.10m))])));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var settled = result.Value!.Tables["Settled"];
        settled.Count.ShouldBe(1);
        settled[0]["TradeId"].ShouldBe("T2");
        settled[0]["Notional"].ShouldBe(1025.00m);
    }

    [Fact]
    public void ARowThatCannotBeBuiltFailsTheSetUnlessTheCallerDropsIt()
    {
        var set = Compile(Set([Table("Trades", "TradeId", "Notional", "Qty")], [], new TableOutputDefinition(
            "PerUnit",
            new EachRowSource("Trades"),
            [new CopyColumn("TradeId", "TradeId"), new FormulaColumn("Each", Divide(Field("Notional"), Field("Qty")))])));
        var tables = Tables(("Trades", [
            Row(("TradeId", "T1"), ("Notional", 10m), ("Qty", 2m)),
            Row(("TradeId", "T2"), ("Notional", 10m), ("Qty", 0m)),
            Row(("TradeId", "T3"), ("Notional", 10m), ("Qty", 5m))]));

        var failed = set.Evaluate(NoScalars, tables);
        var failures = new List<SetRowFailure>();
        var dropped = set.Evaluate(NoScalars, tables, failure =>
        {
            failures.Add(failure);
            return true;
        });
        var refused = set.Evaluate(NoScalars, tables, static _ => false);

        failed.IsFailure.ShouldBeTrue();
        refused.IsFailure.ShouldBeTrue();
        dropped.IsSuccess.ShouldBeTrue(dropped.CurrentMessage);
        dropped.Value!.Tables["PerUnit"].Select(row => row["TradeId"]).ShouldBe(["T1", "T3"]);
        failures.Count.ShouldBe(1);
        failures[0].Output.ShouldBe("PerUnit");
        failures[0].Row["TradeId"].ShouldBe("T2");
        failures[0].Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ACountFormulaMakesThatManyRowsEachKnowingItsPosition()
    {
        var set = Compile(Set([], [], new TableOutputDefinition(
            "Schedule",
            new CountSource(Field("months"), "N"),
            [
                new CopyColumn("LoanId", "LoanId"),
                new FormulaColumn("Payment", Round(Divide(Field("principal"), Field("months")), 2, "AwayFromZero")),
                new FormulaColumn("Balance", Round(Subtract(Field("principal"), Multiply(Divide(Field("principal"), Field("months")), Field("N"))), 2, "AwayFromZero")),
            ])));

        var result = set.Evaluate(Row(("LoanId", "L7"), ("principal", 1200m), ("months", 3m)), Tables());

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var schedule = result.Value!.Tables["Schedule"];
        schedule.Select(row => row["Balance"]).ShouldBe(new object[] { 800.00m, 400.00m, 0.00m });
        schedule.Select(row => row["Payment"]).ShouldBe(new object[] { 400.00m, 400.00m, 400.00m });
    }

    [Fact]
    public void ACountThatIsNotAWholeNumberIsRefusedWithItsOwnCode()
    {
        var set = Compile(Set([], [], new TableOutputDefinition("Schedule", new CountSource(Field("months"), "N"), [new FormulaColumn("N2", Field("N"))])));

        var result = set.Evaluate(Row(("months", 2.5m)), Tables());

        result.IsFailure.ShouldBeTrue();
        result.Code!.Name.ShouldBe("SetRowCountInvalid");
    }

    [Fact]
    public void EachGroupMakesOneRowPerGroupWithTotalsOverThatGroup()
    {
        var set = Compile(Set(
            [Table("Trades", "Counterparty", "TradeId", "Notional")],
            [new SumValue("totalNotional", "Trades", "Notional"), new CountValue("tradeCount", "Trades", "TradeId"), new MaxValue("largestNotional", "Trades", "Notional")],
            new TableOutputDefinition(
                "Statement",
                new EachGroupSource("Trades", ["Counterparty"]),
                [
                    new CopyColumn("Counterparty", "Counterparty"),
                    new FormulaColumn("Trades", Field("tradeCount")),
                    new FormulaColumn("Notional", Field("totalNotional")),
                    new FormulaColumn("Largest", Field("largestNotional")),
                    new FormulaColumn("Fee", Round(Multiply(Field("totalNotional"), Constant(0.001m)), 2, "AwayFromZero")),
                ])));

        var result = set.Evaluate(NoScalars, Tables(("Trades", [
            Row(("Counterparty", "Acme"), ("TradeId", "T1"), ("Notional", 999.00m)),
            Row(("Counterparty", "Acme"), ("TradeId", "T2"), ("Notional", 1025.00m)),
            Row(("Counterparty", "Birch"), ("TradeId", "T3"), ("Notional", 1000.00m))])));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var statement = result.Value!.Tables["Statement"];
        statement.Count.ShouldBe(2);
        statement[0]["Counterparty"].ShouldBe("Acme");
        statement[0]["Trades"].ShouldBe(2.00m);
        statement[0]["Notional"].ShouldBe(2024.00m);
        statement[0]["Largest"].ShouldBe(1025.00m);
        statement[0]["Fee"].ShouldBe(2.02m);
        statement[1]["Notional"].ShouldBe(1000.00m);
        statement[1]["Fee"].ShouldBe(1.00m);
    }

    [Fact]
    public void ALookupReadsAValueFromAnotherTableAndAMissingKeyIsRefused()
    {
        var definition = Set(
            [Table("Trades", "TradeId", "Region", "Notional"), Table("Rates", "Region", "Rate")],
            [new LookupValue("rate", "Rates", "Region", "Region", "Rate")],
            new TableOutputDefinition(
                "Fees",
                new EachRowSource("Trades"),
                [
                    new CopyColumn("TradeId", "TradeId"),
                    new LiteralColumn("FeeName", "Commission"),
                    new FormulaColumn("Amount", Round(Multiply(Field("Notional"), Field("rate")), 2, "AwayFromZero")),
                ]));
        var set = Compile(definition);
        var rates = ("Rates", new[] { Row(("Region", "EU"), ("Rate", 0.0035m)), Row(("Region", "US"), ("Rate", 0.0025m)) });

        var found = set.Evaluate(NoScalars, Tables(("Trades", [
            Row(("TradeId", "T2"), ("Region", "EU"), ("Notional", 1025.00m)),
            Row(("TradeId", "T3"), ("Region", "US"), ("Notional", 1000.00m))]), rates));
        var missing = set.Evaluate(NoScalars, Tables(("Trades", [Row(("TradeId", "T4"), ("Region", "JP"), ("Notional", 10m))]), rates));

        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        found.Value!.Tables["Fees"].Select(row => row["Amount"]).ShouldBe(new object[] { 3.59m, 2.50m });
        found.Value.Tables["Fees"][0]["FeeName"].ShouldBe("Commission");
        missing.Code!.Name.ShouldBe("SetLookupKeyNotFound");
    }

    [Fact]
    public void SeveralOutputsAreWrittenFromTheSameInputWithoutTouchingIt()
    {
        var set = Compile(Set(
            [Table("Trades", "TradeId", "Notional")],
            [],
            new TableOutputDefinition("Commission", new EachRowSource("Trades"), [new CopyColumn("TradeId", "TradeId"), new LiteralColumn("FeeName", "Commission"), new FormulaColumn("Amount", Round(Multiply(Field("Notional"), Constant(0.0035m)), 2, "AwayFromZero"))]),
            new TableOutputDefinition("Tax", new EachRowSource("Trades"), [new CopyColumn("TradeId", "TradeId"), new LiteralColumn("FeeName", "Tax"), new FormulaColumn("Amount", Round(Multiply(Field("Notional"), Constant(0.002m)), 2, "AwayFromZero"))])));

        var result = set.Evaluate(NoScalars, Tables(("Trades", [Row(("TradeId", "T2"), ("Notional", 1025.00m))])));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        set.OutputNames.ShouldBe(new[] { "Commission", "Tax" });
        result.Value!.Tables["Commission"][0]["Amount"].ShouldBe(3.59m);
        result.Value.Tables["Tax"][0]["Amount"].ShouldBe(2.05m);
    }

    [Fact]
    public void AMissingTableAnOversizedOutputAndABadDefinitionEachHaveTheirOwnRefusal()
    {
        var oneRow = Set([Table("Trades", "TradeId")], [], new TableOutputDefinition("Out", new EachRowSource("Trades"), [new CopyColumn("TradeId", "TradeId")]));
        var set = Compile(oneRow);
        var small = new CalculationSetCompiler().Compile(oneRow, Formulas, new SetLimits(1, 10)).Value!;
        var twoRows = Tables(("Trades", [Row(("TradeId", "a")), Row(("TradeId", "b"))]));
        var duplicate = Set([Table("Trades", "TradeId")], [], new TableOutputDefinition("Out", new EachRowSource("Trades"), [new CopyColumn("A", "TradeId"), new CopyColumn("A", "TradeId")]));
        var unknownTable = Set([], [], new TableOutputDefinition("Out", new EachRowSource("Nope"), [new CopyColumn("A", "A")]));

        set.Evaluate(NoScalars, Tables()).Code!.Name.ShouldBe("SetTableMissing");
        small.Evaluate(NoScalars, twoRows).Code!.Name.ShouldBe("SetRowLimitExceeded");
        new CalculationSetCompiler().Compile(duplicate, Formulas, Rows).Code!.Name.ShouldBe("SetDefinitionInvalid");
        new CalculationSetCompiler().Compile(unknownTable, Formulas, Rows).Code!.Name.ShouldBe("SetDefinitionInvalid");
    }
}
