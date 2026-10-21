using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-1722: a setting of a component can take its value from a run parameter. The rules say which settings may, which type of
/// parameter fits, and what is wrong with a binding; the run converts the parameter's value onto a copy and never changes the stored
/// component or step; a required setting that is bound counts as stated.
/// </summary>
public sealed class SettingBindingTests
{
    private static PipelineComponentConfiguration Source(string name = "src") => new() { Name = name, ComponentType = "DataSetSource" };

    private static PipelineComponentSettingBindingConfiguration Bind(string setting, string parameter) => new() { SettingName = setting, ParameterName = parameter };

    private static GraphPipelineConfiguration Graph(PipelineComponentConfiguration component, params PipelineParameterConfiguration[] parameters) => new()
    {
        Name = "Bound",
        Parameters = [.. parameters],
        Components = [component],
    };

    private static PipelineParameterConfiguration Parameter(string name, string type) => new() { Name = name, ParameterType = type };

    private static string[] Reasons(GraphPipelineConfiguration graph) => [.. SettingBindingRules.Problems(graph).Select(problem => problem.Reason)];

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ABindingToADeclaredParameterOfTheRightTypeHasNoProblem()
    {
        var component = Source();
        component.SettingBindings = [Bind("dataSet", "Which"), Bind("batchSize", "Size")];

        Reasons(Graph(component, Parameter("Which", "String"), Parameter("Size", "Int32"))).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ABindingIsRefusedForAnUndeclaredParameterTheWrongTypeAndASettingThatAlsoHoldsAValue()
    {
        var component = Source();
        component.DataSetName = "Orders";
        component.SettingBindings = [Bind("dataSet", "Which"), Bind("batchSize", "Size"), Bind("truncateFirst", "Missing")];

        var reasons = Reasons(Graph(component, Parameter("Which", "String"), Parameter("Size", "String")));

        reasons.Length.ShouldBe(3);
        reasons.ShouldContain(reason => reason.Contains("states a value for 'dataSet'"));
        reasons.ShouldContain(reason => reason.Contains("takes a parameter of type Int32") && reason.Contains("declared as String"));
        reasons.ShouldContain(reason => reason.Contains("declares no parameter 'Missing'"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ASettingThatShapesTheGraphCannotBeChosenByARun()
    {
        var component = new PipelineComponentConfiguration { Name = "split", ComponentType = "ConditionalSplit" };
        component.SettingBindings = [Bind("errorOutput", "Out"), Bind("noSuchSetting", "Out")];

        var reasons = Reasons(Graph(component, Parameter("Out", "String")));

        reasons.ShouldContain(reason => reason.Contains("'errorOutput'") && reason.Contains("shapes the graph"));
        reasons.ShouldContain(reason => reason.Contains("'noSuchSetting'") && reason.Contains("not a single-value setting"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ABoundSettingCountsAsStatedSoARequiredOneThatIsBoundIsNotMissing()
    {
        var component = Source();
        component.SettingBindings = [Bind("dataSet", "Which")];
        component.BatchSize = 50;

        ComponentSettings.Problems(component).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheRunPutsTheConvertedValuesOnACopyAndLeavesTheStoredComponentAlone()
    {
        var stored = Source();
        stored.SettingBindings = [Bind("dataSet", "Which"), Bind("batchSize", "Size")];
        var context = GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?> { ["which"] = "Breweries", ["Size"] = 25L });

        var applied = BoundSettings.Apply(stored, context);

        applied.IsSuccess.ShouldBeTrue(applied.CurrentMessage);
        applied.Value!.DataSetName.ShouldBe("Breweries");
        applied.Value.BatchSize.ShouldBe(25);
        stored.DataSetName.ShouldBeNull();
        stored.BatchSize.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AParameterTheRunWasGivenNoValueForOrOfTheWrongKindFailsTheRunNamingIt()
    {
        var stored = Source();
        stored.SettingBindings = [Bind("truncateFirst", "Empty")];

        var none = BoundSettings.Apply(stored, GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?> { ["Empty"] = null }));
        var wrong = BoundSettings.Apply(stored, GraphTestKit.ContextWith(Graph(stored), new Dictionary<string, object?> { ["Empty"] = "yes" }));

        none.IsFailure.ShouldBeTrue();
        none.CurrentMessage.ShouldNotBeNull().ShouldContain("Empty");
        wrong.IsFailure.ShouldBeTrue();
        wrong.CurrentMessage.ShouldNotBeNull().ShouldContain("true or false");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ALoopSettingTakesItsValueTheSameWay()
    {
        var stored = new PipelineComponentConfiguration { Name = "loop", ComponentType = "ForLoop", StartValue = 1 };
        stored.SettingBindings = [Bind("end", "Last")];
        var context = GraphTestKit.ContextWith(new GraphPipelineConfiguration { Name = "Bound" }, new Dictionary<string, object?> { ["Last"] = 7 });

        var applied = BoundSettings.Apply(stored, context);

        applied.Value!.EndValue.ShouldBe(7);
        stored.EndValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ASourceWhoseDataSetIsBoundHasUnknownFields()
    {
        var component = Source();
        component.SettingBindings = [Bind("dataSet", "Which")];

        ParameterSettings.IsBound(component, "dataSet").ShouldBeTrue();
        ParameterSettings.IsBound(component, "batchSize").ShouldBeFalse();
    }

    private static GraphPipelineConfiguration WithComponents(params PipelineComponentConfiguration[] components) => new()
    {
        Name = "Vars",
        Variables = [new PipelineVariableConfiguration { Name = "Total", DataType = "Int32", ValueText = "0" }],
        Parameters = [new PipelineParameterConfiguration { Name = "Target", ParameterType = "String" }, new PipelineParameterConfiguration { Name = "Fixed", ParameterType = "String", IsReadOnly = true }],
        Components = [.. components],
    };

    private static PipelineComponentConfiguration SetVariable(string? variable = null) => new()
    {
        Name = "set", ComponentType = "SetVariable", ConstantType = "Int32", ConstantText = "1", VariableName = variable,
    };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheSaveDoesNotJudgeAVariableNameOnlyTheRunKnows()
    {
        var set = SetVariable();
        set.SettingBindings = [Bind("variable", "Target")];

        var problems = GraphComponentValidator.Problems(WithComponents(set), [], Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);

        problems.ShouldNotContain(problem => problem.Reason.Contains("not a variable in scope"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AVariableNameLeftUnboundAndUnknownIsStillRefusedAtSave()
    {
        var problems = GraphComponentValidator.Problems(WithComponents(SetVariable("Nope")), [], Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);

        problems.ShouldContain(problem => problem.Reason.Contains("'Nope'") && problem.Reason.Contains("not a variable in scope"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheRunJudgesTheComponentWithTheNameItWasGiven()
    {
        var graph = WithComponents(SetVariable("Total"));
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        GraphComponentValidator.ComponentProblems(graph, SetVariable("Total"), logger).ShouldBeEmpty();
        GraphComponentValidator.ComponentProblems(graph, SetVariable("Missing"), logger)
            .ShouldContain(problem => problem.Reason.Contains("'Missing'") && problem.Reason.Contains("not a variable in scope"));
        GraphComponentValidator.ComponentProblems(graph, SetVariable("Fixed"), logger)
            .ShouldContain(problem => problem.Reason.Contains("'Fixed'") && problem.Reason.Contains("read-only parameter"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AComponentInsideALoopWhoseVariableIsBoundIsNotJudgedForNamesInScopeAtSave()
    {
        var loop = new PipelineComponentConfiguration { Name = "loop", ComponentType = "ForLoop", StartValue = 1, EndValue = 3, StepValue = 1 };
        loop.SettingBindings = [Bind("variable", "Target")];
        var inner = SetVariable("Counter");
        inner.ParentComponent = "loop";

        var problems = GraphComponentValidator.Problems(WithComponents(loop, inner), [], Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);

        problems.ShouldNotContain(problem => problem.Reason.Contains("'Counter'"));
    }
}
