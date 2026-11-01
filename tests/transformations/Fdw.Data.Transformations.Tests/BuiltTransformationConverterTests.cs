using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Fluency;
using Fdw.Services.Calculations.Abstractions.Trees;
using Shouldly;
using Xunit;
using static Fdw.Services.Calculations.Abstractions.Fluency.Calc;

namespace Fdw.Data.Transformations.Tests;

/// <summary>A built calculation round-trips between the engine's trees and the stored rows, and bad rows are refused.</summary>
public sealed class BuiltTransformationConverterTests
{
    private static readonly CalculationLimits Limits = new(200, 5000, 32);

    private static readonly IReadOnlyList<BuiltTransformationInputConfiguration> Inputs = new List<BuiltTransformationInputConfiguration>
    {
        new() { Name = "intervalMinutes", ValueType = "Number", ExecutionOrder = 0 },
        new() { Name = "quantity", ValueType = "Number", ExecutionOrder = 1 },
        new() { Name = "price", ValueType = "Number", ExecutionOrder = 2 },
        new() { Name = "share", ValueType = "Number", ExecutionOrder = 3 },
        new() { Name = "fee", ValueType = "Number", ExecutionOrder = 4 },
    };

    private static CalculationArrayDefinition Settlement() =>
        CalculationArray.Named("Settlement")
            .Result("hours", Divide(60, Field("intervalMinutes")), emit: false)
            .Result("energy", Divide(Field("quantity"), Ref("hours")), emit: false)
            .Result("amount", When(NotEqual(Field("share"), 0), Multiply(Ref("energy"), Field("price"), Field("share")), 0))
            .Result("total", Add(Ref("amount"), Field("fee")))
            .Build(Limits)
            .Value!;

    private static BuiltTransformationConfiguration Stored() => BuiltTransformationConverter.ToConfiguration("Settlement", Settlement(), Inputs);

    private static string RefusalCode(IGenericResult<CalculationArrayDefinition> result) =>
        result.IsFailure ? result.Code?.Name ?? string.Empty : "Success";

    [Fact]
    [Trait("Priority", "P1")]
    public void RoundTripKeepsEveryResultAndItsFormula()
    {
        var original = Settlement();

        var read = BuiltTransformationConverter.ToArray(Stored(), Limits);

        read.IsSuccess.ShouldBeTrue();
        read.Value!.Name.ShouldBe("Settlement");
        read.Value.Results.Count.ShouldBe(original.Results.Count);
        for (var index = 0; index < original.Results.Count; index++)
        {
            read.Value.Results[index].Name.ShouldBe(original.Results[index].Name);
            read.Value.Results[index].Emit.ShouldBe(original.Results[index].Emit);
            CalculationTreeJson.Write(read.Value.Results[index].Expression).ShouldBe(CalculationTreeJson.Write(original.Results[index].Expression));
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void OutputsKeepTheirOrderAndWhetherTheyAreKept()
    {
        var stored = Stored();

        string.Join(",", stored.Outputs.Select(output => output.Name)).ShouldBe("hours,energy,amount,total");
        string.Join(",", stored.Outputs.Select(output => output.IsKept)).ShouldBe("False,False,True,True");
        string.Join(",", stored.Outputs.Select(output => output.ExecutionOrder)).ShouldBe("0,1,2,3");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void PiecesAreNumberedRootFirstWithTheirParentAndPosition()
    {
        var rows = Stored().Nodes.Where(row => row.OutputName == "hours").OrderBy(row => row.NodeNumber).ToList();

        rows.Count.ShouldBe(3);
        rows[0].ShouldSatisfyAllConditions(
            row => row.NodeNumber.ShouldBe(0),
            row => row.ParentNodeNumber.ShouldBeNull(),
            row => row.Operator.ShouldBe("Divide"));
        rows[1].ShouldSatisfyAllConditions(
            row => row.ParentNodeNumber.ShouldBe(0),
            row => row.Position.ShouldBe(0),
            row => row.Operator.ShouldBe("Constant"),
            row => row.NumberValue.ShouldBe(60m));
        rows[2].ShouldSatisfyAllConditions(
            row => row.ParentNodeNumber.ShouldBe(0),
            row => row.Position.ShouldBe(1),
            row => row.Operator.ShouldBe("Field"),
            row => row.NodeName.ShouldBe("intervalMinutes"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void InputsAreKeptAsGiven()
    {
        string.Join(",", Stored().Inputs.Select(input => input.Name)).ShouldBe(string.Join(",", Inputs.Select(input => input.Name)));
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void AnOutputWithNoFormulaRowsIsRefused()
    {
        var stored = Stored();
        foreach (var row in stored.Nodes.Where(candidate => candidate.OutputName == "energy").ToList())
        {
            stored.Nodes.Remove(row);
        }

        RefusalCode(BuiltTransformationConverter.ToArray(stored, Limits)).ShouldBe("StoredFormulaInvalid");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ARepeatedPieceNumberIsRefused()
    {
        var stored = Stored();
        var rows = stored.Nodes.Where(candidate => candidate.OutputName == "hours").OrderBy(candidate => candidate.NodeNumber).ToList();
        rows[2].NodeNumber = rows[1].NodeNumber;

        RefusalCode(BuiltTransformationConverter.ToArray(stored, Limits)).ShouldBe("StoredFormulaInvalid");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void TwoRootPiecesAreRefused()
    {
        var stored = Stored();
        var rows = stored.Nodes.Where(candidate => candidate.OutputName == "hours").OrderBy(candidate => candidate.NodeNumber).ToList();
        rows[2].ParentNodeNumber = null;

        RefusalCode(BuiltTransformationConverter.ToArray(stored, Limits)).ShouldBe("StoredFormulaInvalid");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void APieceThatNoRootReachesIsRefused()
    {
        var stored = Stored();
        var rows = stored.Nodes.Where(candidate => candidate.OutputName == "hours").OrderBy(candidate => candidate.NodeNumber).ToList();
        rows[2].ParentNodeNumber = 99;

        RefusalCode(BuiltTransformationConverter.ToArray(stored, Limits)).ShouldBe("StoredFormulaInvalid");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void AFormulaDeeperThanTheLimitIsRefused()
    {
        var shallow = new CalculationLimits(200, 5000, 1);

        RefusalCode(BuiltTransformationConverter.ToArray(Stored(), shallow)).ShouldBe("StoredFormulaInvalid");
    }
}
