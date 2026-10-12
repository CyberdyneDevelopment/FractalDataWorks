using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-1240: the table inputs, derived values and table outputs a request states become a calculation set definition, and a
/// member that belongs to another kind of source, value or column is refused with its reason, never ignored.
/// </summary>
public sealed class BuiltSetMapperTests
{
    private static BuiltFormulaNodePayload Field(string name) => new() { Op = "Field", Name = name };

    private static BuiltFormulaNodePayload Number(decimal value) => new() { Op = "Constant", Value = value };

    private static List<TableInputPayload> Tables() =>
    [
        new() { Name = "Trades", Columns = [new() { Name = "TradeId", ValueType = "Text" }, new() { Name = "Notional", ValueType = "Number" }] },
    ];

    private static TableOutputPayload Output(RowSourcePayload? rows, params OutputColumnPayload[] columns) =>
        new() { Name = "Out", Rows = rows, Columns = [.. columns] };

    private static OutputColumnPayload Copy(string name, string source) => new() { Name = name, Kind = "Copy", Source = source };

    private static Mapped Map(IList<DerivedValuePayload>? derived = null, params TableOutputPayload[] outputs) =>
        new(BuiltSetMapper.ToDefinition("Set", Tables(), derived ?? [], outputs, 32, NullLogger.Instance));

    private sealed record Mapped(Fdw.Results.IGenericResult<CalculationSetDefinition> Result)
    {
        public bool Ok => Result.IsSuccess;

        public string Message => Result.CurrentMessage ?? string.Empty;

        public CalculationSetDefinition Definition => Result.Value!;
    }

    [Fact]
    public void AnEachSourceWithAConditionAndEveryColumnKindBecomesTheDefinition()
    {
        var mapped = Map(
            [new DerivedValuePayload { Name = "total", Kind = "Sum", Table = "Trades", Column = "Notional" }],
            Output(
                new RowSourcePayload { Kind = "Each", Table = "Trades", Where = new BuiltFormulaNodePayload { Op = "GreaterOrEqual", Operands = [Field("Notional"), Number(1000)] } },
                Copy("TradeId", "TradeId"),
                new OutputColumnPayload { Name = "Kind", Kind = "Literal", LiteralType = "Text", LiteralValue = "Big" },
                new OutputColumnPayload { Name = "Share", Kind = "Formula", Formula = new BuiltFormulaNodePayload { Op = "Divide", Operands = [Field("Notional"), Field("total")] } }));

        mapped.Ok.ShouldBeTrue(mapped.Message);
        mapped.Definition.TableInputs.Single().Columns.Count.ShouldBe(2);
        mapped.Definition.Derived.Single().ShouldBeOfType<SumValue>();
        var output = mapped.Definition.Outputs.Single();
        output.Rows.ShouldBeOfType<EachRowSource>();
        output.Columns.Select(column => column.GetType()).ShouldBe([typeof(CopyColumn), typeof(LiteralColumn), typeof(FormulaColumn)]);
    }

    [Fact]
    public void ACountSourceAndAnEachGroupSourceBecomeTheirDefinitions()
    {
        var count = Map(null, Output(new RowSourcePayload { Kind = "Count", Count = Number(3), IndexColumn = "N" }, Copy("N", "N")));
        var group = Map(null, Output(new RowSourcePayload { Kind = "EachGroup", Table = "Trades", By = ["TradeId"] }, Copy("TradeId", "TradeId")));

        count.Definition.Outputs.Single().Rows.ShouldBeOfType<CountSource>();
        group.Definition.Outputs.Single().Rows.ShouldBeOfType<EachGroupSource>();
    }

    [Fact]
    public void ADerivedValueOfEveryKindIsMappedAndAMisplacedMemberIsRefused()
    {
        foreach (var kind in new[] { "Sum", "Count", "Min", "Max", "Average" })
        {
            Map([new DerivedValuePayload { Name = "v", Kind = kind, Table = "Trades", Column = "Notional" }]).Ok.ShouldBeTrue(kind);
        }

        Map([new DerivedValuePayload { Name = "v", Kind = "Lookup", Table = "Trades", Column = "Notional", KeyColumn = "TradeId", MatchField = "id" }]).Ok.ShouldBeTrue();
        Map([new DerivedValuePayload { Name = "v", Kind = "Lookup", Table = "Trades", Column = "Notional" }]).Ok.ShouldBeFalse();
        Map([new DerivedValuePayload { Name = "v", Kind = "Sum", Table = "Trades", Column = "Notional", KeyColumn = "TradeId" }]).Ok.ShouldBeFalse();
        Map([new DerivedValuePayload { Name = "v", Kind = "Median", Table = "Trades", Column = "Notional" }]).Message.ShouldContain("Median");
    }

    [Fact]
    public void ARowSourceWithMembersFromAnotherSourceOrNoneAtAllIsRefused()
    {
        var refused = new[]
        {
            new RowSourcePayload { Kind = "Each", Table = "Trades", IndexColumn = "N" },
            new RowSourcePayload { Kind = "Each" },
            new RowSourcePayload { Kind = "Count", Count = Number(2) },
            new RowSourcePayload { Kind = "Count", Count = Number(2), IndexColumn = "N", By = ["TradeId"] },
            new RowSourcePayload { Kind = "EachGroup", Table = "Trades" },
            new RowSourcePayload { Kind = "EachGroup", Table = "Trades", By = ["TradeId"], IndexColumn = "N" },
            new RowSourcePayload { Kind = "Everything", Table = "Trades" },
        };

        foreach (var rows in refused)
        {
            Map(null, Output(rows, Copy("A", "TradeId"))).Ok.ShouldBeFalse(rows.Kind);
        }

        Map(null, Output(null, Copy("A", "TradeId"))).Message.ShouldContain("no rows source");
    }

    [Fact]
    public void AColumnWithMembersFromAnotherKindOrMissingItsOwnIsRefused()
    {
        var each = new RowSourcePayload { Kind = "Each", Table = "Trades" };
        var refused = new[]
        {
            new OutputColumnPayload { Name = "A", Kind = "Copy" },
            new OutputColumnPayload { Name = "A", Kind = "Copy", Source = "TradeId", Formula = Field("Notional") },
            new OutputColumnPayload { Name = "A", Kind = "Literal", LiteralType = "Number", LiteralValue = "twelve" },
            new OutputColumnPayload { Name = "A", Kind = "Literal", LiteralType = "Text" },
            new OutputColumnPayload { Name = "A", Kind = "Literal", LiteralType = "Text", LiteralValue = "x", Source = "TradeId" },
            new OutputColumnPayload { Name = "A", Kind = "Formula" },
            new OutputColumnPayload { Name = "A", Kind = "Formula", Formula = Field("Notional"), Source = "TradeId" },
            new OutputColumnPayload { Name = "A", Kind = "Magic" },
        };

        foreach (var column in refused)
        {
            Map(null, Output(each, column)).Ok.ShouldBeFalse(column.Kind);
        }
    }

    [Fact]
    public void ANumberAndAFlagLiteralAreParsedWithTheInvariantCulture()
    {
        var mapped = Map(null, Output(
            new RowSourcePayload { Kind = "Each", Table = "Trades" },
            new OutputColumnPayload { Name = "Rate", Kind = "Literal", LiteralType = "Number", LiteralValue = "0.25" },
            new OutputColumnPayload { Name = "On", Kind = "Literal", LiteralType = "Flag", LiteralValue = "true" }));

        mapped.Ok.ShouldBeTrue(mapped.Message);
        mapped.Definition.Outputs.Single().Columns.OfType<LiteralColumn>().Count().ShouldBe(2);
    }
}
