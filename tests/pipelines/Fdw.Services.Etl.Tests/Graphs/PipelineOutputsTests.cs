using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-6136: a graph states the outputs it gives back. The save judges them against the graph's variables (a name once, a variable the graph declares, the variable's own type) and a run that ends
/// collects each from its variable as the stated type: a value that does not read as its type fails the run, and a variable with no value gives a null output with its type.
/// </summary>
public sealed class PipelineOutputsTests
{
    private static GraphPipelineConfiguration Graph(params PipelineOutputConfiguration[] outputs) => new()
    {
        Name = "Report",
        Variables =
        [
            new PipelineVariableConfiguration { Name = "Total", DataType = "Int32", ValueText = "0" },
            new PipelineVariableConfiguration { Name = "Label", DataType = "String", ValueText = null },
        ],
        Outputs = [.. outputs],
    };

    private static PipelineOutputConfiguration Output(string name, string variable, string dataType) => new() { Name = name, VariableName = variable, DataType = dataType };

    private static string[] Reasons(GraphPipelineConfiguration graph) =>
        [.. GraphComponentValidator.Problems(graph, [], NullLogger.Instance).Where(problem => problem.Path.Count > 0 && problem.Path[0] == "outputs").Select(problem => problem.Reason)];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void OutputsOfDeclaredVariablesWithTheirOwnTypesAreAccepted()
    {
        Reasons(Graph(Output("total", "Total", "Int32"), Output("label", "Label", "String"))).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AnOutputOfAVariableTheGraphDoesNotDeclareIsRefusedNamingIt()
    {
        var reasons = Reasons(Graph(Output("total", "Missing", "Int32")));

        reasons.ShouldHaveSingleItem().ShouldContain("'Missing'");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData("String")]
    [InlineData("NotAType")]
    public void AnOutputWhoseTypeIsNotItsVariablesTypeOrNoDataTypeIsRefused(string dataType)
    {
        Reasons(Graph(Output("total", "Total", dataType))).ShouldHaveSingleItem().ShouldContain("'total'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AnOutputNameUsedTwiceIsRefused()
    {
        Reasons(Graph(Output("total", "Total", "Int32"), Output("TOTAL", "Total", "Int32"))).ShouldHaveSingleItem().ShouldContain("more than once");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ARunCollectsEachOutputFromItsVariableAsItsStatedType()
    {
        var variables = new FlowVariables();
        variables.Declare("Total", 42, "Int32");
        variables.Declare("Label", null, "String");

        var collected = GraphRunOutputs.Collect(Graph(Output("total", "Total", "Int32"), Output("label", "Label", "String")), variables, NullLogger.Instance);

        collected.IsSuccess.ShouldBeTrue(collected.CurrentMessage);
        collected.Value!.Select(output => (output.Name, output.DataType, output.Text)).ToArray()
            .ShouldBe([("total", "Int32", "42"), ("label", "String", (string?)null)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AValueThatIsNotOfTheStatedTypeFailsTheRunAndNothingIsCoerced()
    {
        var variables = new FlowVariables();
        variables.Declare("Total", "forty-two", "Int32");

        var collected = GraphRunOutputs.Collect(Graph(Output("total", "Total", "Int32")), variables, NullLogger.Instance);

        collected.IsFailure.ShouldBeTrue();
        collected.CurrentMessage.ShouldNotBeNull().ShouldContain("'total'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AGraphThatDeclaresNoOutputsGivesNone()
    {
        var collected = GraphRunOutputs.Collect(Graph(), new FlowVariables(), NullLogger.Instance);

        collected.IsSuccess.ShouldBeTrue();
        collected.Value.ShouldBeEmpty();
    }
}
