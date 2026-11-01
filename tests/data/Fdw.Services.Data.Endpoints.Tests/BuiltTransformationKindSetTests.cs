using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-1240: the Built kind takes either results or table outputs, builds and stores a set the engine accepts, refuses a set
/// whose output reads a name it cannot see, keeps the stored set when a change leaves it out, and describes it on a read.
/// </summary>
public sealed class BuiltTransformationKindSetTests
{
    private static TransformationKindContext Context()
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationResults, null, null)).Returns(500);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationNodes, null, null)).Returns(20000);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationDepth, null, null)).Returns(32);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxSetOutputRows, null, null)).Returns(1000);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxSetLookupRows, null, null)).Returns(1000);
        return new TransformationKindContext(settings.Object, NullLogger.Instance);
    }

    private static BuiltFormulaNodePayload Field(string name) => new() { Op = "Field", Name = name };

    private static List<TransformationInputPayload> Inputs() => [new() { Name = "pct", ValueType = "Number" }];

    private static List<TableInputPayload> Tables() =>
    [
        new() { Name = "Trades", Columns = [new() { Name = "TradeId", ValueType = "Text" }, new() { Name = "Notional", ValueType = "Number" }] },
    ];

    private static List<TableOutputPayload> Outputs(string reads = "Notional") =>
    [
        new()
        {
            Name = "Fees",
            Rows = new RowSourcePayload { Kind = "Each", Table = "Trades" },
            Columns =
            [
                new() { Name = "TradeId", Kind = "Copy", Source = "TradeId" },
                new() { Name = "Amount", Kind = "Formula", Formula = new BuiltFormulaNodePayload { Op = "Multiply", Operands = [Field(reads), Field("pct")] } },
            ],
        },
    ];

    private static CreateTransformationRequest Create(List<TableOutputPayload>? outputs = null) =>
        new() { Name = "FeeSet", Implementation = "Built", Inputs = Inputs(), TableInputs = Tables(), TableOutputs = outputs ?? Outputs() };

    [Fact]
    public void ACreateWithTableOutputsBuildsAConfigurationHoldingTheSet()
    {
        var prepared = new BuiltTransformationKind().Prepare(Create(), Context());

        prepared.IsSuccess.ShouldBeTrue(prepared.CurrentMessage);
        var built = prepared.Value.ShouldBeOfType<BuiltTransformationConfiguration>();
        built.TableInputs.Single().Name.ShouldBe("Trades");
        built.TableOutputs.Single().Name.ShouldBe("Fees");
        built.Outputs.ShouldBeEmpty();
    }

    [Fact]
    public void ResultsAndTableOutputsTogetherAreRefused()
    {
        var request = Create();
        request.Results = [new() { Name = "total", IsKept = true, Formula = Field("pct") }];

        var refused = new BuiltTransformationKind().Prepare(request, Context());

        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain("FeeSet");
    }

    [Fact]
    public void TableInputsOrDerivedValuesWithoutTableOutputsAreRefusedNamingTheMember()
    {
        var kind = new BuiltTransformationKind();

        var tables = kind.Prepare(new CreateTransformationRequest { Name = "T", Implementation = "Built", Inputs = Inputs(), Results = [new() { Name = "x", Formula = Field("pct") }], TableInputs = Tables() }, Context());
        var derived = kind.Prepare(new CreateTransformationRequest { Name = "T", Implementation = "Built", Inputs = Inputs(), Results = [new() { Name = "x", Formula = Field("pct") }], Derived = [new() { Name = "d", Kind = "Sum", Table = "Trades", Column = "Notional" }] }, Context());

        tables.CurrentMessage.ShouldNotBeNull().ShouldContain("tableInputs");
        derived.CurrentMessage.ShouldNotBeNull().ShouldContain("derived");
    }

    [Fact]
    public void AnOutputThatReadsANameItCannotSeeIsRefusedBeforeAnythingIsStored()
    {
        var refused = new BuiltTransformationKind().Prepare(Create(Outputs(reads: "Missing")), Context());

        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain("Missing");
    }

    [Fact]
    public void AChangeThatLeavesOutTheSetKeepsTheStoredOne()
    {
        var kind = new BuiltTransformationKind();
        var stored = kind.Prepare(Create(), Context()).Value!;

        var changed = kind.PrepareUpdate(new UpdateTransformationRequest { Description = "Fees at one percent" }, stored, Context());

        changed.IsSuccess.ShouldBeTrue(changed.CurrentMessage);
        var built = changed.Value.ShouldBeOfType<BuiltTransformationConfiguration>();
        built.Description.ShouldBe("Fees at one percent");
        built.TableOutputs.Single().Name.ShouldBe("Fees");
        built.TableInputs.Single().Name.ShouldBe("Trades");
    }

    [Fact]
    public void AReadDescribesTheStoredSetAsTheWirePayloads()
    {
        var kind = new BuiltTransformationKind();
        var stored = kind.Prepare(Create(), Context()).Value!;
        var payload = new TransformationPayload();

        var detailed = kind.Detail(stored, payload, Context());

        detailed.IsSuccess.ShouldBeTrue(detailed.CurrentMessage);
        payload.TableInputs.Single().Columns.Select(column => column.Name).ShouldBe(["TradeId", "Notional"]);
        payload.TableOutputs.Single().Rows!.Kind.ShouldBe("Each");
        payload.TableOutputs.Single().Columns.Select(column => column.Kind).ShouldBe(["Copy", "Formula"]);
        payload.Results.ShouldBeEmpty();
    }
}
