using System.Collections.Generic;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for the parameters a graph declares at design time (FDW-1549): they are judged with the graph, declared in the root scope
/// from the values a run supplies, and read by run start through the same mechanism the trigger and the schedule use.
/// </summary>
public sealed class GraphParametersTests
{
    private static PipelineParameterConfiguration Parameter(string name, string type = "Int32", bool required = false, bool readOnly = false) =>
        new() { Name = name, ParameterType = type, IsRequired = required, IsReadOnly = readOnly };

    private static GraphPipelineConfiguration WithParameters(params PipelineParameterConfiguration[] parameters)
    {
        var graph = Steps(SourceFlow("flow"));
        foreach (var parameter in parameters)
        {
            graph.Parameters.Add(parameter);
        }

        return graph;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphWithWellFormedParametersIsValid()
    {
        var graph = WithParameters(Parameter("Limit", required: true), Parameter("Region", "String", readOnly: true));

        GraphShapeValidator.Problems(graph, NullLogger.Instance).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AParameterOfAnUnknownTypeIsReportedWhereItIs()
    {
        var problem = GraphShapeValidator.Problems(WithParameters(Parameter("Limit", "Nonsense")), NullLogger.Instance).ShouldHaveSingleItem();

        problem.Path.ShouldBe(["parameters", "Limit"]);
        problem.Reason.ShouldContain("not a data type");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AParameterAndAVariableWithOneNameAreReported()
    {
        var graph = WithParameters(Parameter("Limit"));
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "limit", DataType = "Int32" });

        var problem = GraphShapeValidator.Problems(graph, NullLogger.Instance).ShouldHaveSingleItem();

        problem.Path.ShouldBe(["parameters", "Limit"]);
        problem.Reason.ShouldContain("both a parameter and a variable");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TwoParametersWithOneNameAreReported()
    {
        var problem = GraphShapeValidator.Problems(WithParameters(Parameter("Limit"), Parameter("LIMIT")), NullLogger.Instance).ShouldHaveSingleItem();

        problem.Reason.ShouldContain("more than once");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AParameterCannotTakeANameARunReserves()
    {
        var problem = GraphShapeValidator.Problems(WithParameters(Parameter(PipelineRunParameterValues.LastSuccessfulRun, "DateTimeOffset")), NullLogger.Instance).ShouldHaveSingleItem();

        problem.Reason.ShouldContain("reserves");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASetVariableStepOnAReadOnlyParameterIsReportedAndOnAMutableOneIsNot()
    {
        var readOnly = WithParameters(Parameter("Limit", readOnly: true));
        readOnly.Components.Add(SetVariable("change", "Limit", "Int32", "7", ordinal: 1));
        var mutable = WithParameters(Parameter("Limit"));
        mutable.Components.Add(SetVariable("change", "Limit", "Int32", "7", ordinal: 1));

        var problem = GraphShapeValidator.Problems(readOnly, NullLogger.Instance).ShouldHaveSingleItem();
        GraphShapeValidator.Problems(mutable, NullLogger.Instance).ShouldBeEmpty();

        problem.Reason.ShouldContain("read-only parameter");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheRunScopeHoldsTheSuppliedValueAndALeftOutOptionalParameterBlank()
    {
        var graph = WithParameters(Parameter("Limit", required: true), Parameter("Region", "String"));
        var supplied = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["Limit"] = 5 };

        var scope = GraphVariables.Declare(graph, supplied, NullLogger.Instance);

        scope.IsSuccess.ShouldBeTrue(scope.CurrentMessage);
        scope.Value!.TryGet("Limit", out var limit).ShouldBeTrue();
        limit.ShouldBe(5);
        scope.Value.IsBlank("Region").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AValueNoParameterTakesRefusesTheRun()
    {
        var supplied = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["Other"] = 1 };

        var scope = GraphVariables.Declare(WithParameters(Parameter("Limit")), supplied, NullLogger.Instance);

        scope.IsFailure.ShouldBeTrue();
        scope.CurrentMessage.ShouldContain("Other");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RunStartReadsAGraphsDeclaredParametersThroughTheSharedMechanism()
    {
        var graph = WithParameters(Parameter("Limit", required: true), Parameter("Region", "String", readOnly: true));
        var declared = ((IDeclaresRunParameters)graph).DeclaredRunParameters;

        var resolved = PipelineRunParameterValues.Resolve(declared, [new("Limit", "42")], NullLogger.Instance);
        var missing = PipelineRunParameterValues.Resolve(declared, [new("Region", "West")], NullLogger.Instance);
        var unknown = PipelineRunParameterValues.Resolve(declared, [new("Limit", "1"), new("Nope", "x")], NullLogger.Instance);

        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        resolved.Value!["Limit"].ShouldBe(42);
        missing.IsFailure.ShouldBeTrue();
        unknown.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheNamesAScheduleWouldSupplyAreCheckedAgainstAGraphsParameters()
    {
        var declared = ((IDeclaresRunParameters)WithParameters(Parameter("Limit", required: true))).DeclaredRunParameters;

        PipelineRunParameterValues.VerifyNames(declared, ["Limit"], NullLogger.Instance).IsSuccess.ShouldBeTrue();
        PipelineRunParameterValues.VerifyNames(declared, ["Other"], NullLogger.Instance).IsFailure.ShouldBeTrue();
        PipelineRunParameterValues.VerifyNames(declared, [], NullLogger.Instance).IsFailure.ShouldBeTrue();
    }
}
