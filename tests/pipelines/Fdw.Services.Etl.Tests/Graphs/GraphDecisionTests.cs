using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-6134: decisions and loops as components. A condition, a gate and a switch fire outputs; a line from an output means "run when this output fired"; a component none of whose lines fired
/// is skipped; a while loop judges its test before its body and a loop may take a decision's output as its test; a container groups components. Every rule the save checks has a test, by its message.
/// </summary>
[Trait("Category", "GraphDecisions")]
public sealed class GraphDecisionTests
{
    private static PipelineComponentConfiguration Condition(string name, string expression, int ordinal = 0, string? parent = null) =>
        Placed(new PipelineComponentConfiguration { Name = name, ComponentType = "Condition", ConditionExpression = expression }, ordinal, parent);

    private static PipelineComponentConfiguration Declared(PipelineComponentConfiguration component, string setting, string text)
    {
        // Since FDW-7275 the setting is kept in the column its binding names, as a stored graph holds it.
        ComponentSettingBindings.Find(setting).ShouldNotBeNull(setting).Set(component, text);
        return component;
    }

    private static PipelineComponentConfiguration Gate(string name, string logic, int ordinal = 0, string? parent = null) =>
        Declared(Placed(new PipelineComponentConfiguration { Name = name, ComponentType = "Gate" }, ordinal, parent), "logic", logic);

    private static PipelineComponentConfiguration Switch(string name, string variable, string outcomes, bool otherwise, int ordinal = 0, string? parent = null) =>
        Declared(Placed(new PipelineComponentConfiguration { Name = name, ComponentType = "Switch", VariableName = variable, OtherwiseFires = otherwise }, ordinal, parent), "outcomes", outcomes);

    private static PipelineComponentConfiguration While(string name, string? condition, string? testOutput, int maximum, int ordinal = 0, string? parent = null)
    {
        var loop = Placed(new PipelineComponentConfiguration { Name = name, ComponentType = "While", ConditionExpression = condition, MaxIterations = maximum }, ordinal, parent);
        return testOutput is null ? loop : Declared(loop, "testOutput", testOutput);
    }

    private static PipelineComponentConfiguration DoWhileTesting(string name, string? condition, string? testOutput, int maximum, int ordinal = 0, string? parent = null)
    {
        var loop = Placed(new PipelineComponentConfiguration { Name = name, ComponentType = "DoWhile", ConditionExpression = condition, MaxIterations = maximum }, ordinal, parent);
        return testOutput is null ? loop : Declared(loop, "testOutput", testOutput);
    }

    private static PipelineComponentConfiguration Container(string name, int ordinal = 0, string? parent = null) =>
        Placed(new PipelineComponentConfiguration { Name = name, ComponentType = "Container" }, ordinal, parent);

    private static PipelineLinkConfiguration Signal(string from, string output, string to, string input) => Link(from, to, output, input);

    private static void AddLinks(GraphPipelineConfiguration graph, params PipelineLinkConfiguration[] links)
    {
        foreach (var link in links)
        {
            graph.Links.Add(link);
        }
    }

    private static GraphPipelineConfiguration WithVariables(GraphPipelineConfiguration graph, params (string Name, int Value)[] variables)
    {
        foreach (var (name, value) in variables)
        {
            graph.Variables.Add(new PipelineVariableConfiguration { Name = name, DataType = "Int32", ValueText = value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        }

        return graph;
    }

    private static async Task<(FlowVariables Variables, IGenericResult Ran)> Run(GraphPipelineConfiguration graph)
    {
        var gateway = new PagedSourceGateway(PagedSourceGateway.Rows(1), []);
        var built = GraphFlowBuilder.Build(graph, Context(graph, gateway));
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        var variables = GraphVariables.Declare(graph, null, NullLogger.Instance);
        variables.IsSuccess.ShouldBeTrue(variables.CurrentMessage);
        var ran = await built.Value!.Run(StepContext(variables.Value!, new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);
        return (variables.Value!, ran);
    }

    private static int Read(FlowVariables variables, string name)
    {
        variables.TryGet(name, out var value).ShouldBeTrue(name);
        return System.Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AConditionFiresItsTrueOutputAndOnlyTheComponentOnThatLineRuns()
    {
        var graph = WithVariables(
            Steps(Condition("big", "Level > 5"), SetVariable("whenTrue", "Yes", "Int32", "1"), SetVariable("whenFalse", "No", "Int32", "1")),
            ("Level", 7), ("Yes", 0), ("No", 0));
        graph.Links.Add(Signal("big", "true", "whenTrue", "after1"));
        graph.Links.Add(Signal("big", "false", "whenFalse", "after1"));

        var (variables, ran) = await Run(graph);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Read(variables, "Yes").ShouldBe(1);
        Read(variables, "No").ShouldBe(0, "the component on the false line is skipped");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AConditionThatDoesNotHoldFiresItsFalseOutput()
    {
        var graph = WithVariables(
            Steps(Condition("big", "Level > 5"), SetVariable("whenTrue", "Yes", "Int32", "1"), SetVariable("whenFalse", "No", "Int32", "1")),
            ("Level", 2), ("Yes", 0), ("No", 0));
        graph.Links.Add(Signal("big", "true", "whenTrue", "after1"));
        graph.Links.Add(Signal("big", "false", "whenFalse", "after1"));

        var (variables, ran) = await Run(graph);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Read(variables, "Yes").ShouldBe(0);
        Read(variables, "No").ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASkippedDecisionFiresNothingSoWhatItLeadsToIsSkippedInItsTurn()
    {
        var graph = WithVariables(
            Steps(Condition("first", "Level > 5"), Condition("second", "Level > 0"), SetVariable("leaf", "Yes", "Int32", "1")),
            ("Level", 2), ("Yes", 0));
        graph.Links.Add(Signal("first", "true", "second", "after1"));
        graph.Links.Add(Signal("second", "true", "leaf", "after1"));

        var (variables, ran) = await Run(graph);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Read(variables, "Yes").ShouldBe(0, "first did not hold, so second was skipped and fired nothing, so leaf was skipped");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("And", 1, 1, true)]
    [InlineData("And", 1, 0, false)]
    [InlineData("Or", 0, 1, true)]
    [InlineData("Or", 0, 0, false)]
    [InlineData("Nand", 1, 1, false)]
    [InlineData("Nor", 0, 0, true)]
    [InlineData("Xor", 1, 0, true)]
    [InlineData("Xor", 1, 1, false)]
    [InlineData("Xnor", 1, 1, true)]
    [InlineData("Xnor", 1, 0, false)]
    public async Task AGateCombinesTheLinesThatFeedItByItsLogic(string logic, int a, int b, bool expected)
    {
        var graph = WithVariables(
            Steps(
                Condition("ca", "A == 1"), Condition("cb", "B == 1"), Gate("g", logic), SetVariable("yes", "Yes", "Int32", "1"), SetVariable("no", "No", "Int32", "1")),
            ("A", a), ("B", b), ("Yes", 0), ("No", 0));
        graph.Links.Add(Signal("ca", "true", "g", "in1"));
        graph.Links.Add(Signal("cb", "true", "g", "in2"));
        graph.Links.Add(Signal("g", "true", "yes", "after1"));
        graph.Links.Add(Signal("g", "false", "no", "after1"));

        var (variables, ran) = await Run(graph);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Read(variables, "Yes").ShouldBe(expected ? 1 : 0);
        Read(variables, "No").ShouldBe(expected ? 0 : 1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AGateReadsALineWhoseConditionWasSkippedAsFalseAndIsNeverSkippedItself()
    {
        var graph = WithVariables(
            Steps(Condition("first", "Level > 5"), Condition("second", "Level > 0"), Gate("g", "Not"), SetVariable("leaf", "Yes", "Int32", "1")),
            ("Level", 2), ("Yes", 0));
        graph.Links.Add(Signal("first", "true", "second", "after1"));
        graph.Links.Add(Signal("second", "true", "g", "in1"));
        graph.Links.Add(Signal("g", "true", "leaf", "after1"));

        var (variables, ran) = await Run(graph);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Read(variables, "Yes").ShouldBe(1, "second was skipped, so it fired nothing, so Not of that line is true and the gate ran");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASwitchFiresTheOutputThatNamesTheValueOfItsVariable()
    {
        var graph = WithVariables(
            Steps(Switch("pick", "Level", "1,2", otherwise: false), SetVariable("one", "One", "Int32", "1"), SetVariable("two", "Two", "Int32", "1")),
            ("Level", 2), ("One", 0), ("Two", 0));
        graph.Links.Add(Signal("pick", "1", "one", "after1"));
        graph.Links.Add(Signal("pick", "2", "two", "after1"));

        var (variables, ran) = await Run(graph);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Read(variables, "One").ShouldBe(0);
        Read(variables, "Two").ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASwitchWithOtherwiseFiresItForAValueNoOutcomeNames()
    {
        var graph = WithVariables(
            Steps(Switch("pick", "Level", "1,2", otherwise: true), SetVariable("other", "Other", "Int32", "1")),
            ("Level", 9), ("Other", 0));
        graph.Links.Add(Signal("pick", "otherwise", "other", "after1"));

        var (variables, ran) = await Run(graph);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Read(variables, "Other").ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASwitchWithoutOtherwiseFailsTheRunForAValueNoOutcomeNames()
    {
        var graph = WithVariables(Steps(Switch("pick", "Level", "1,2", otherwise: false)), ("Level", 9));

        var (_, ran) = await Run(graph);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("none of its outcomes");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AWhileLoopJudgesItsTestBeforeItsBodySoTheBodyMayRunNoTime()
    {
        var graph = WithVariables(
            Steps(While("again", "Count < 0", null, 3), SetVariable("body", "Ran", "Int32", "1", parent: "again")),
            ("Count", 0), ("Ran", 0));

        var (variables, ran) = await Run(graph);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Read(variables, "Ran").ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AWhileLoopRunsItsBodyUntilItsTestStopsHolding()
    {
        var graph = WithVariables(
            Steps(While("again", "Count < 3", null, 3), SetVariable("body", "Count", "Int32", "5", parent: "again")),
            ("Count", 0));

        var (variables, ran) = await Run(graph);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Read(variables, "Count").ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AWhileLoopWhoseTestStillHoldsAfterItsStatedMostFailsAndNamesIt()
    {
        var graph = WithVariables(
            Steps(While("again", "Count < 10", null, 2), SetVariable("body", "Count", "Int32", "1", parent: "again")),
            ("Count", 0));

        var (_, ran) = await Run(graph);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("again");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AWhileLoopMayTakeTheOutputOfADecisionInsideItAsItsTest()
    {
        var graph = WithVariables(
            Steps(
                While("again", null, "more.true", 3),
                Condition("more", "Count < 3", ordinal: 0, parent: "again"),
                SetVariable("advance", "Count", "Int32", "5", ordinal: 1, parent: "again")),
            ("Count", 0));

        var (variables, ran) = await Run(graph);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Read(variables, "Count").ShouldBe(5, "the decision ran ahead of the first round, held, the body set Count, and the test then failed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADoWhileLoopMayTakeTheOutputOfADecisionInsideItAsItsTestJudgedAfterEachRound()
    {
        var graph = WithVariables(
            Steps(
                DoWhileTesting("again", null, "more.true", 4),
                Condition("more", "Count < 3", ordinal: 0, parent: "again"),
                SetVariable("advance", "Count", "Int32", "5", ordinal: 1, parent: "again")),
            ("Count", 0));

        var (variables, ran) = await Run(graph);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Read(variables, "Count").ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AContainerRunsTheComponentsInsideItOnce()
    {
        var graph = WithVariables(Steps(Container("box"), SetVariable("inside", "Ran", "Int32", "1", parent: "box")), ("Ran", 0));

        var (variables, ran) = await Run(graph);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Read(variables, "Ran").ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AContainerWhoseLineDidNotFireIsSkippedWithEverythingInsideIt()
    {
        var graph = WithVariables(
            Steps(Condition("big", "Level > 5"), Container("box"), SetVariable("inside", "Ran", "Int32", "1", parent: "box")),
            ("Level", 2), ("Ran", 0));
        graph.Links.Add(Signal("big", "true", "box", "after1"));

        var (variables, ran) = await Run(graph);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Read(variables, "Ran").ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ASoundDecisionGraphHasNoProblem()
    {
        var graph = WithVariables(
            Steps(Condition("big", "Level > 5"), SetVariable("whenTrue", "Yes", "Int32", "1"), SetVariable("whenFalse", "No", "Int32", "1")),
            ("Level", 7), ("Yes", 0), ("No", 0));
        graph.Links.Add(Signal("big", "true", "whenTrue", "after1"));
        graph.Links.Add(Signal("big", "false", "whenFalse", "after1"));

        GraphShapeValidator.Problems(graph, NullLogger.Instance).ShouldBeEmpty();
    }

    private static IReadOnlyList<string> Reasons(GraphPipelineConfiguration graph) =>
        [.. DecisionProblems.Of(graph, NullLogger.Instance).Select(problem => problem.Reason)];

    [Fact]
    [Trait("Priority", "P0")]
    public void AnOutputNothingReadsIsRefusedWithItsCode()
    {
        var graph = Steps(Condition("big", "Level > 5"), SetVariable("whenTrue", "Yes", "Int32", "1"));
        graph.Links.Add(Signal("big", "true", "whenTrue", "after1"));

        Reasons(graph).ShouldHaveSingleItem().ShouldContain("'false' of the Condition 'big' has no line leaving it");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AGateNoLineFeedsIsRefusedAsUnfed()
    {
        var graph = Steps(Gate("g", "And"), SetVariable("yes", "Yes", "Int32", "1"), SetVariable("no", "No", "Int32", "1"));
        graph.Links.Add(Signal("g", "true", "yes", "after1"));
        graph.Links.Add(Signal("g", "false", "no", "after1"));

        Reasons(graph).ShouldHaveSingleItem().ShouldContain("The gate 'g' is not fed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AGateWhoseLogicIsUnknownOrWhoseLineCountItsLogicDoesNotTakeIsRefused()
    {
        var unknown = Steps(Condition("c", "A = 1"), Gate("g", "Maybe"), SetVariable("y", "Y", "Int32", "1"), SetVariable("n", "N", "Int32", "1"));
        AddLinks(unknown, Signal("c", "true", "g", "in1"), Signal("c", "false", "y", "after1"), Signal("g", "true", "y", "after2"), Signal("g", "false", "n", "after1"));
        var twoForNot = Steps(Condition("c", "A = 1"), Condition("d", "B = 1"), Gate("g", "Not"), SetVariable("y", "Y", "Int32", "1"), SetVariable("n", "N", "Int32", "1"));
        AddLinks(
            twoForNot,
            Signal("c", "true", "g", "in1"), Signal("c", "false", "y", "after1"), Signal("d", "true", "g", "in2"), Signal("d", "false", "y", "after2"),
            Signal("g", "true", "y", "after3"), Signal("g", "false", "n", "after1"));

        Reasons(unknown).ShouldHaveSingleItem().ShouldContain("states the logic 'Maybe', which is not one of");
        Reasons(twoForNot).ShouldHaveSingleItem().ShouldContain("takes exactly 1 input line(s), and the gate has 2");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ASwitchThatNamesAnOutcomeTwiceIsRefused()
    {
        var graph = Steps(Switch("pick", "Level", "a, A", otherwise: false), SetVariable("x", "X", "Int32", "1"));
        graph.Links.Add(Signal("pick", "a", "x", "after1"));

        Reasons(graph).ShouldHaveSingleItem().ShouldContain("the outcome 'a' is named more than once");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void DecisionsThatFeedOneAnotherInACircleAreRefused()
    {
        var graph = Steps(Gate("g1", "Not"), Gate("g2", "Not"), SetVariable("x", "X", "Int32", "1"));
        AddLinks(
            graph,
            Signal("g1", "true", "g2", "in1"), Signal("g2", "true", "g1", "in1"), Signal("g1", "false", "x", "after1"), Signal("g2", "false", "x", "after2"));

        Reasons(graph).ShouldContain(reason => reason.Contains("feed one another in a circle") && reason.Contains("'g1', 'g2'"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ALoopThatStatesItsTestTwiceOrNotAtAllIsRefused()
    {
        var both = Steps(DoWhileTesting("again", "Count < 3", "more.true", 3), Condition("more", "Count < 3", parent: "again"), SetVariable("x", "X", "Int32", "1", parent: "again"));
        var neither = Steps(DoWhileTesting("again", null, null, 3), SetVariable("x", "X", "Int32", "1", parent: "again"));

        Reasons(both).ShouldContain(reason => reason.Contains("exactly once") && reason.Contains("states both"));
        Reasons(neither).ShouldHaveSingleItem().ShouldContain("states neither");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ALoopWhoseTestOutputIsNotADecisionInsideItIsRefused()
    {
        var graph = Steps(DoWhileTesting("again", null, "ghost.true", 3), SetVariable("x", "X", "Int32", "1", parent: "again"));

        Reasons(graph).ShouldHaveSingleItem().ShouldContain("'ghost.true' as its test, which names 'ghost', which is not a component inside the loop");
    }
}
