using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-1957, FDW-2042: a Set variable component with a transformation sets a declared variable to the result of one named field transformation applied to a value of
/// the run, so a file name can be worked out from a date parameter. The transformation is found by name, like a field mapping's; a
/// step that names one that does not exist, leaves out a required parameter or gives a value the transformation cannot take fails and
/// names itself; the save check says the same before the pipeline is run.
/// </summary>
public sealed class SetVariableCalculationTests
{
    private static PipelineComponentConfiguration Calculate(
        string variable, string input, string transformation, params (string Name, string Value)[] parameters)
    {
        var step = new PipelineComponentConfiguration
        {
            Name = "calc", ComponentType = "SetVariable", VariableName = variable, ConstantText = input, TransformationName = transformation,
        };
        foreach (var (name, value) in parameters)
        {
            step.Parameters.Add(new PipelineComponentParameterConfiguration { ParameterName = name, ValueText = value });
        }

        return step;
    }

    private static async Task<(IGenericResult Result, FlowVariables Variables)> Run(
        PipelineComponentConfiguration step, Action<FlowVariables> declare)
    {
        var variables = new FlowVariables();
        declare(variables);
        var context = ContextWith(new GraphPipelineConfiguration { Name = "Ingest" }, new Dictionary<string, object?>());
        var built = new SetVariableFlowStepBuilder().Create(step, [], context);
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);

        var ran = await built.Value!.Run(StepContext(variables, new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);
        return (ran, variables);
    }

    private static object? Value(FlowVariables variables, string name)
    {
        variables.TryGet(name, out var value);
        return value;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ADateParameterIsFormattedIntoTheVariableKeepingItsType()
    {
        var (ran, variables) = await Run(
            Calculate("FileName", "{{ArrivedAt}}", "FormatDateTime", ("format", "yyyyMMdd")),
            v =>
            {
                v.DeclareParameter("ArrivedAt", new DateTimeOffset(2026, 10, 19, 23, 30, 0, TimeSpan.Zero), false);
                v.Declare("FileName", null);
            });

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Value(variables, "FileName").ShouldBe("20261019");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AStepAddsTextBeforeAndAfterAndAnotherStepCanReplaceInTheResult()
    {
        var (added, variables) = await Run(
            Calculate("FileName", "{{Stamp}}", "Concatenate", ("prefix", "orders-"), ("suffix", ".csv")),
            v =>
            {
                v.Declare("Stamp", "20261019");
                v.Declare("FileName", null);
            });
        added.IsSuccess.ShouldBeTrue(added.CurrentMessage);
        Value(variables, "FileName").ShouldBe("orders-20261019.csv");

        var (replaced, chained) = await Run(
            Calculate("Clean", "{{FileName}}", "Replace", ("oldValue", "orders-"), ("newValue", "")),
            v =>
            {
                v.Declare("FileName", "orders-20261019.csv");
                v.Declare("Clean", null);
            });
        replaced.IsSuccess.ShouldBeTrue(replaced.CurrentMessage);
        Value(chained, "Clean").ShouldBe("20261019.csv");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ATransformationNobodyRegisteredFailsTheStepNamingIt()
    {
        var (ran, _) = await Run(Calculate("V", "x", "NoSuchTransformation"), v => v.Declare("V", null));

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("NoSuchTransformation");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AStepThatLeavesOutARequiredParameterFailsNamingIt()
    {
        var (ran, _) = await Run(Calculate("V", "{{When}}", "FormatDateTime"), v =>
        {
            v.Declare("When", DateTimeOffset.UtcNow);
            v.Declare("V", null);
        });

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("format");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AValueTheTransformationCannotTakeFailsTheStepInsteadOfCrashingTheRun()
    {
        var (ran, variables) = await Run(
            Calculate("V", "{{Text}}", "FormatDateTime", ("format", "yyyyMMdd")),
            v =>
            {
                v.Declare("Text", "not a date");
                v.Declare("V", "before");
            });

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("FormatDateTime");
        Value(variables, "V").ShouldBe("before");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AVariableNobodyDeclaredIsNotCreated()
    {
        var (ran, variables) = await Run(Calculate("Typo", "x", "Concatenate", ("prefix", "a")), _ => { });

        ran.IsFailure.ShouldBeTrue();
        variables.IsDeclared("Typo").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheSaveCheckRefusesAnUnknownTransformationAMissingParameterAndNamesNotInScope()
    {
        var graph = Steps(Calculate("FileName", "{{Nowhere}}", "FormatDateTime"));
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "FileName", DataType = "String", ValueText = "" });

        var reasons = GraphShapeValidator.Problems(graph, NullLogger.Instance).Select(problem => problem.Reason).ToList();

        reasons.ShouldContain(reason => reason.Contains("Nowhere", StringComparison.Ordinal));
        reasons.ShouldContain(reason => reason.Contains("required parameter 'format'", StringComparison.Ordinal));

        var unknown = Steps(Calculate("FileName", "x", "NoSuchTransformation"));
        unknown.Variables.Add(new PipelineVariableConfiguration { Name = "FileName", DataType = "String", ValueText = "" });
        GraphShapeValidator.Problems(unknown, NullLogger.Instance).ShouldHaveSingleItem().Path.ShouldBe(["components", "calc", "settings", "transformation"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheSaveCheckRefusesADataTypeGivenWithATransformationAndNoValueToApplyItTo()
    {
        var typed = Calculate("FileName", "x", "Concatenate", ("prefix", "a"));
        typed.ConstantType = "String";
        var graph = Steps(typed);
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "FileName", DataType = "String", ValueText = "" });
        GraphShapeValidator.Problems(graph, NullLogger.Instance).ShouldHaveSingleItem().Path.ShouldBe(["components", "calc", "settings", "valueType"]);

        var valueless = Calculate("FileName", "x", "Concatenate", ("prefix", "a"));
        valueless.ConstantText = null;
        var none = Steps(valueless);
        none.Variables.Add(new PipelineVariableConfiguration { Name = "FileName", DataType = "String", ValueText = "" });
        GraphShapeValidator.Problems(none, NullLogger.Instance).ShouldHaveSingleItem().Path.ShouldBe(["components", "calc", "settings", "value"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ASetVariableWithNoTransformationNeedsADataTypeAndTakesNoTransformationParameters()
    {
        var plain = new PipelineComponentConfiguration { Name = "calc", ComponentType = "SetVariable", VariableName = "V", ConstantText = "1" };
        var untyped = Steps(plain);
        untyped.Variables.Add(new PipelineVariableConfiguration { Name = "V", DataType = "String", ValueText = "" });
        GraphShapeValidator.Problems(untyped, NullLogger.Instance).ShouldHaveSingleItem().Path.ShouldBe(["components", "calc", "settings", "valueType"]);

        var withParameters = new PipelineComponentConfiguration { Name = "calc", ComponentType = "SetVariable", VariableName = "V", ConstantType = "String", ConstantText = "1" };
        withParameters.Parameters.Add(new PipelineComponentParameterConfiguration { ParameterName = "prefix", ValueText = "a" });
        var stray = Steps(withParameters);
        stray.Variables.Add(new PipelineVariableConfiguration { Name = "V", DataType = "String", ValueText = "" });
        GraphShapeValidator.Problems(stray, NullLogger.Instance).ShouldHaveSingleItem().Path.ShouldBe(["components", "calc", "settings", "parameters"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheSaveCheckRefusesAComponentThatSetsAReadOnlyParameter()
    {
        var graph = Steps(Calculate("Locked", "x", "Concatenate", ("prefix", "a")));
        graph.Parameters.Add(new PipelineParameterConfiguration { Name = "Locked", ParameterType = "String", IsReadOnly = true });

        GraphShapeValidator.Problems(graph, NullLogger.Instance).ShouldHaveSingleItem().Reason.ShouldContain("read-only");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AValidComponentIsAcceptedAndItsVariableCountsAsAssigned()
    {
        var graph = Steps(Calculate("FileName", "{{Stamp}}", "Concatenate", ("prefix", "orders-")));
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "Stamp", DataType = "String", ValueText = "20261019" });
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "FileName", DataType = "String", ValueText = "" });

        GraphShapeValidator.Problems(graph, NullLogger.Instance).ShouldBeEmpty();
        GraphScope.AssignedVariables(graph).ShouldContain("FileName");
    }
}
