using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-1219: a Built transformation definition is refused, before anything is stored, when an input is wrong, a formula
/// cannot be built or checked, or a formula reads a field no input declares; a good definition becomes the rows to store.
/// </summary>
public sealed class BuiltTransformationSupportTests
{
    private static IEffectiveSettingsProvider Settings(int results = 500, int nodes = 20000, int depth = 32)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationResults, null, null)).Returns(results);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationNodes, null, null)).Returns(nodes);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationDepth, null, null)).Returns(depth);
        return settings.Object;
    }

    private static List<TransformationInputPayload> Inputs() =>
    [
        new() { Name = "price", ValueType = "Number" },
        new() { Name = "quantity", ValueType = "Number" },
    ];

    private static BuiltFormulaNodePayload Field(string name) => new() { Op = "Field", Name = name };

    private static BuiltFormulaNodePayload Total(string first, string second) =>
        new() { Op = "Multiply", Operands = [Field(first), Field(second)] };

    private static List<BuiltResultPayload> Results(BuiltFormulaNodePayload? formula) =>
        [new() { Name = "total", IsKept = true, Formula = formula }];

    [Fact]
    public void AGoodDefinitionBecomesTheInputsOutputsAndFormulaRowsToStore()
    {
        var prepared = BuiltTransformationSupport.Prepare(
            Settings(), "Settlement", "price times quantity", Inputs(), Results(Total("price", "quantity")), NullLogger.Instance);

        prepared.IsSuccess.ShouldBeTrue();
        prepared.Value!.Name.ShouldBe("Settlement");
        prepared.Value.Description.ShouldBe("price times quantity");
        prepared.Value.Inputs.Select(input => input.Name).ShouldBe(["price", "quantity"]);
        prepared.Value.Outputs.Single().Name.ShouldBe("total");
        prepared.Value.Outputs.Single().IsKept.ShouldBeTrue();
        prepared.Value.Nodes.Count.ShouldBe(3);
    }

    [Fact]
    public void AFormulaThatReadsAFieldNoInputDeclaresIsRefused()
    {
        var prepared = BuiltTransformationSupport.Prepare(
            Settings(), "Settlement", null, Inputs(), Results(Total("price", "tax")), NullLogger.Instance);

        prepared.IsFailure.ShouldBeTrue();
        prepared.CurrentMessage.ShouldNotBeNull().ShouldContain("tax");
    }

    [Fact]
    public void AnInputDeclaredTwiceIsRefused()
    {
        var inputs = Inputs();
        inputs.Add(new TransformationInputPayload { Name = "price", ValueType = "Number" });

        var prepared = BuiltTransformationSupport.Prepare(
            Settings(), "Settlement", null, inputs, Results(Total("price", "quantity")), NullLogger.Instance);

        prepared.IsFailure.ShouldBeTrue();
        prepared.CurrentMessage.ShouldNotBeNull().ShouldContain("price");
    }

    [Fact]
    public void AnInputWithAnUnknownValueTypeIsRefused()
    {
        var inputs = Inputs();
        inputs[1].ValueType = "Text";

        var prepared = BuiltTransformationSupport.Prepare(
            Settings(), "Settlement", null, inputs, Results(Total("price", "quantity")), NullLogger.Instance);

        prepared.IsFailure.ShouldBeTrue();
        prepared.CurrentMessage.ShouldNotBeNull().ShouldContain("Text");
    }

    [Fact]
    public void AResultWithoutAFormulaIsRefused()
    {
        var prepared = BuiltTransformationSupport.Prepare(
            Settings(), "Settlement", null, Inputs(), Results(null), NullLogger.Instance);

        prepared.IsFailure.ShouldBeTrue();
        prepared.CurrentMessage.ShouldNotBeNull().ShouldContain("total");
    }

    [Fact]
    public void AFormulaNestedDeeperThanTheLimitIsRefused()
    {
        var nested = new BuiltFormulaNodePayload { Op = "Add", Operands = [Total("price", "quantity"), Field("price")] };

        var prepared = BuiltTransformationSupport.Prepare(
            Settings(depth: 2), "Settlement", null, Inputs(), Results(nested), NullLogger.Instance);

        prepared.IsFailure.ShouldBeTrue();
        prepared.CurrentMessage.ShouldNotBeNull().ShouldContain("deeper");
    }

    [Fact]
    public void ALimitSettingThatIsNotPositiveIsRefusedNamingTheSetting()
    {
        var prepared = BuiltTransformationSupport.Prepare(
            Settings(nodes: 0), "Settlement", null, Inputs(), Results(Total("price", "quantity")), NullLogger.Instance);

        prepared.IsFailure.ShouldBeTrue();
        prepared.CurrentMessage.ShouldNotBeNull().ShouldContain(SettingDefinitions.MaxCalculationNodes);
    }
}
