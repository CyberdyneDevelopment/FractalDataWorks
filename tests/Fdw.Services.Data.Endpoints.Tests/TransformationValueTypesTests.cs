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
/// FDW-2333: a Built transformation may yield a date-time, and a list and a read of transformations say the kind of value each result
/// is, so a client can offer a calculation for the field it fits.
/// </summary>
public sealed class TransformationValueTypesTests
{
    private static TransformationKindContext Context()
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationResults, null, null)).Returns(500);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationNodes, null, null)).Returns(20000);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationDepth, null, null)).Returns(32);
        return new TransformationKindContext(settings.Object, NullLogger.Instance);
    }

    private static BuiltFormulaNodePayload Node(string op, string? name = null, string? mode = null, decimal? value = null, params BuiltFormulaNodePayload[] operands)
        => new() { Op = op, Name = name, Mode = mode, Value = value, Operands = operands };

    private static BuiltTransformationConfiguration Built(string name, params BuiltResultPayload[] results)
    {
        var prepared = new BuiltTransformationKind().Prepare(
            new CreateTransformationRequest { Name = name, Implementation = "Built", Inputs = [new() { Name = "seconds", ValueType = "Number" }], Results = results },
            Context());
        prepared.IsSuccess.ShouldBeTrue(prepared.CurrentMessage);
        return prepared.Value.ShouldBeOfType<BuiltTransformationConfiguration>();
    }

    private static BuiltResultPayload Yesterday() => new()
    {
        Name = "when",
        IsKept = true,
        Formula = Node("AddTime", "UTC", "Days", null, Node("Now", "UTC", "StartOfDay"), Node("Constant", value: -1m)),
    };

    [Fact]
    public void ABuiltTransformationWithADateTimeResultSavesAndItsOutputSaysDateTime()
    {
        var summary = TransformationEndpointSupport.ToSummary(Built("Yesterday", Yesterday()), Context());

        var output = summary.Outputs.ShouldHaveSingleItem();
        output.ValueType.ShouldBe("DateTime");
        output.DataType.ShouldBe("DateTimeOffset");
    }

    [Fact]
    public void ANumberResultSaysNumberWhetherItIsKeptOrNot()
    {
        var number = Built(
            "Twice",
            new BuiltResultPayload { Name = "base", IsKept = false, Formula = Node("Field", "seconds") },
            new BuiltResultPayload { Name = "twice", IsKept = true, Formula = Node("Multiply", operands: [Node("Ref", "base"), Node("Constant", value: 2m)]) });

        var summary = TransformationEndpointSupport.ToSummary(number, Context());

        summary.Outputs.Select(output => (output.Name, output.ValueType, output.DataType))
            .ShouldBe(new[] { ("base", (string?)"Number", (string?)"Decimal"), ("twice", "Number", "Decimal") });
    }

    [Fact]
    public void ADefinitionTheEngineRefusesLeavesItsResultsWithoutAValueType()
    {
        var stored = Built("Broken", Yesterday());
        stored.Nodes.First().Operator = "NoSuchOperator";

        var summary = TransformationEndpointSupport.ToSummary(stored, Context());

        var output = summary.Outputs.ShouldHaveSingleItem();
        output.ValueType.ShouldBeNull();
        output.DataType.ShouldBeNull();
    }

    [Fact]
    public void AReadOfOneTransformationSaysTheValueTypesToo()
    {
        var detail = TransformationEndpointSupport.ToDetail(Built("Yesterday", Yesterday()), Context());

        detail.IsSuccess.ShouldBeTrue();
        detail.Value!.Outputs.ShouldHaveSingleItem().ValueType.ShouldBe("DateTime");
    }
}
