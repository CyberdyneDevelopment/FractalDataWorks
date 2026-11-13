using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for the Read data options step (FDW-5347): it reads the stored options of a column in one query through the ETL gateway, applies
/// them in the options' own order, and keeps the list they leave in a declared variable.
/// </summary>
public sealed class ReadDataOptionsTests
{
    private static Dictionary<string, object?> Row(string? value, string optionType, string option, object? active = null) =>
        new Dictionary<string, object?> { ["Value"] = value, ["OptionType"] = optionType, ["Option"] = option, ["Active"] = active ?? true };

    private static Dictionary<string, object?> Include(string value, object? active = null) => Row(value, "SelectList", "Include", active);

    private static Dictionary<string, object?> Exclude(string value, object? active = null) => Row(value, "SelectList", "Exclude", active);

    private static FlowReadDataOptions Step(ScriptingGateway fake, int maxRows = 10, string container = "cfg.DataOptions", string resultVariable = "Values") =>
        new("options", fake.Gateway, "Settings", container, "Warehouse", "Orders", "Region", maxRows, resultVariable);

    private static FlowVariables Declared(string name = "Values")
    {
        var variables = new FlowVariables();
        variables.Declare(name, null);
        return variables;
    }

    private static Task<IGenericResult> Run(FlowReadDataOptions step, FlowVariables variables) =>
        step.Run(GraphTestKit.StepContext(variables, new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);

    private static object? Kept(FlowVariables variables)
    {
        variables.TryGet("Values", out var value);
        return value;
    }

    [Fact]
    public async Task TheStepAppliesTheStoredOptionsAndKeepsTheListInTheVariable()
    {
        var fake = new ScriptingGateway([Include("A"), Include("B"), Include("C"), Exclude("B")]);
        var variables = Declared();

        var ran = await Run(Step(fake), variables);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Kept(variables).ShouldBeAssignableTo<IReadOnlyList<string>>().ShouldBe(["A", "C"]);
    }

    [Fact]
    public async Task AnInactiveRowChangesNothing()
    {
        var fake = new ScriptingGateway([Include("A"), Include("B", active: false), Exclude("A", active: false)]);
        var variables = Declared();

        var ran = await Run(Step(fake), variables);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Kept(variables).ShouldBeAssignableTo<IReadOnlyList<string>>().ShouldBe(["A"]);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task AStoreMayKeepTheActiveFlagAsABooleanOrANumber(object active, bool listed)
    {
        var fake = new ScriptingGateway([Include("A", active)]);
        var variables = Declared();

        var ran = await Run(Step(fake), variables);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Kept(variables).ShouldBeAssignableTo<IReadOnlyList<string>>().ShouldNotBeNull().Count.ShouldBe(listed ? 1 : 0);
    }

    [Fact]
    public async Task TheRowsAreAppliedInTheOptionsOwnOrderWhateverOrderTheStoreReturnsThem()
    {
        var excludedFirst = new ScriptingGateway([Exclude("A"), Include("A"), Include("B")]);
        var includedFirst = new ScriptingGateway([Include("A"), Include("B"), Exclude("A")]);
        var first = Declared();
        var second = Declared();

        await Run(Step(excludedFirst), first);
        await Run(Step(includedFirst), second);

        Kept(first).ShouldBeAssignableTo<IReadOnlyList<string>>().ShouldBe(["B"]);
        Kept(second).ShouldBeAssignableTo<IReadOnlyList<string>>().ShouldBe(["B"]);
    }

    [Fact]
    public async Task TheOptionsAreReadInOneQueryWithTheColumnAsParameters()
    {
        var fake = new ScriptingGateway([Include("A"), Include("B")]);

        await Run(Step(fake, maxRows: 10), Declared());

        var (store, command) = fake.Commands.ShouldHaveSingleItem();
        store.ShouldBe("Settings");
        var stream = command.ShouldBeOfType<ScriptStreamCommand>();
        stream.Parameters.ShouldBe([new ScriptParameter("DataStore", "Warehouse"), new ScriptParameter("Container", "Orders"), new ScriptParameter("Column", "Region")]);
        stream.Take.ShouldBe(11);
        stream.Text.ShouldContain("FROM cfg.DataOptions ");
    }

    [Fact]
    public async Task AnEmptyColumnKeepsAnEmptyList()
    {
        var variables = Declared();

        var ran = await Run(Step(new ScriptingGateway([])), variables);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Kept(variables).ShouldBeAssignableTo<IReadOnlyList<string>>().ShouldBeEmpty();
    }

    [Fact]
    public async Task AnOptionTypeThatIsNotKnownFailsTheStepAndLeavesTheVariableAlone()
    {
        var variables = Declared();

        var ran = await Run(Step(new ScriptingGateway([Row("A", "NoSuchType", "Include")])), variables);

        ran.IsFailure.ShouldBeTrue();
        ran.Messages.ShouldContain(message => message.Code == "DATAOPTION-31000");
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("NoSuchType");
        Kept(variables).ShouldBeNull();
    }

    [Fact]
    public async Task AnOptionThatIsNotKnownFailsTheStepAndLeavesTheVariableAlone()
    {
        var variables = Declared();

        var ran = await Run(Step(new ScriptingGateway([Row("A", "SelectList", "NoSuchOption")])), variables);

        ran.IsFailure.ShouldBeTrue();
        ran.Messages.ShouldContain(message => message.Code == "DATAOPTION-30000");
        Kept(variables).ShouldBeNull();
    }

    [Fact]
    public async Task AnActiveRowWithNoValueFailsTheStepWithTheValueRequiredCode()
    {
        var variables = Declared();

        var ran = await Run(Step(new ScriptingGateway([Include("A"), Row(null, "SelectList", "Include")])), variables);

        ran.IsFailure.ShouldBeTrue();
        ran.Messages.ShouldContain(message => message.Code == "DATAOPTION-20000");
        Kept(variables).ShouldBeNull();
    }

    [Theory]
    [InlineData("OptionType")]
    [InlineData("Option")]
    public async Task ARowThatNamesNoOptionFailsTheStepNamingTheRowAndTheField(string field)
    {
        var row = Include("A");
        row[field] = null;

        var ran = await Run(Step(new ScriptingGateway([Include("B"), row])), Declared());

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("row 2");
        ran.CurrentMessage.ShouldContain(field);
    }

    [Fact]
    public async Task AnActiveFlagThatIsNotAFlagFailsTheStep()
    {
        var ran = await Run(Step(new ScriptingGateway([Include("A", "yes")])), Declared());

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("Active");
    }

    [Fact]
    public async Task MoreRowsThanTheStepMayReadFailTheStep()
    {
        var variables = Declared();

        var ran = await Run(Step(new ScriptingGateway([Include("A"), Include("B"), Include("C")]), maxRows: 2), variables);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("more than the 2");
        Kept(variables).ShouldBeNull();
    }

    [Fact]
    public async Task AReadThatFailsFailsTheStepNamingTheStoreAndLeavesTheVariableAlone()
    {
        var variables = Declared();

        var ran = await Run(Step(new ScriptingGateway([], fails: true)), variables);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("Settings");
        Kept(variables).ShouldBeNull();
    }

    [Theory]
    [InlineData("x; DROP TABLE T")]
    [InlineData("[T]")]
    [InlineData("a b")]
    public async Task AContainerThatIsNotANameIsRefusedBeforeAnyQueryRuns(string container)
    {
        var fake = new ScriptingGateway([Include("A")]);

        var ran = await Run(Step(fake, container: container), Declared());

        ran.IsFailure.ShouldBeTrue();
        fake.Commands.ShouldBeEmpty();
    }

    [Fact]
    public async Task AVariableThatIsNotDeclaredIsRefusedBeforeAnyQueryRuns()
    {
        var fake = new ScriptingGateway([Include("A")]);

        var ran = await Run(Step(fake), new FlowVariables());

        ran.IsFailure.ShouldBeTrue();
        fake.Commands.ShouldBeEmpty();
    }

    [Fact]
    public async Task AForEachWithABatchSizeTakesTheListInBatches()
    {
        var fake = new ScriptingGateway([Include("A"), Include("B"), Include("C")]);
        var variables = Declared();
        var batches = new List<IReadOnlyList<string>>();
        var each = new FlowForEach("each", "Batch", "Values", [new Capture("capture", batches)], 2);

        await Run(Step(fake), variables);
        var ran = await each.Run(GraphTestKit.StepContext(variables, new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        batches.Select(batch => string.Join(",", batch)).ShouldBe(["A,B", "C"]);
    }

    [Fact]
    public async Task TheListRendersQuotedAndCommaSeparatedWhereAVariableBecomesText()
    {
        var fake = new ScriptingGateway([Include("A"), Include("B"), Include("C")]);
        var variables = Declared();
        await Run(Step(fake), variables);

        VariableText.Render("IN ({{Values}})", variables).ShouldBe("IN (\"A\",\"B\",\"C\")");
    }

    [Fact]
    public void TheStepIsAnActionOfTheCatalogueWithEverySettingBound()
    {
        var kind = ComponentTypes.ByName("ReadDataOptions");

        kind.IsAction.ShouldBeTrue();
        kind.HoldsComponents.ShouldBeFalse();
        kind.Settings.Where(setting => setting.IsRequired).Select(setting => setting.Name).ShouldBe(
            ["optionsDataStore", "optionsContainer", "dataStore", "container", "field", "maxRows", "resultVariable"], ignoreOrder: true);
        kind.Settings.ShouldAllBe(setting => setting.Name == "stage" || ComponentSettingBindings.Find(setting.Name) != null);
    }

    private static PipelineComponentConfiguration Stored(string? resultVariable = "Values", int? maxRows = 10) => new()
    {
        Name = "options",
        ComponentType = "ReadDataOptions",
        OptionsDataStoreName = "Settings",
        OptionsContainerName = "cfg.DataOptions",
        DataStoreName = "Warehouse",
        ContainerName = "Orders",
        FieldName = "Region",
        MaxRows = maxRows,
        ResultVariable = resultVariable,
        Stage = 1,
    };

    private static GraphPipelineConfiguration Graph(PipelineComponentConfiguration component)
    {
        var graph = GraphTestKit.Steps(component);
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "Values", DataType = "String" });
        return graph;
    }

    [Fact]
    public void AStoredStepIsBuilt()
    {
        var graph = Graph(Stored());

        var built = GraphFlowBuilder.Build(graph, GraphTestKit.Context(graph, new ScriptingGateway([])));

        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
    }

    [Fact]
    public void AStepMissingASettingIsRefusedNamingIt()
    {
        var graph = Graph(Stored(resultVariable: null));

        var built = GraphFlowBuilder.Build(graph, GraphTestKit.Context(graph, new ScriptingGateway([])));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("options");
    }

    [Fact]
    public void AVariableThatIsNotInScopeIsRefusedNamingTheSetting()
    {
        var graph = Graph(Stored(resultVariable: "Nowhere"));

        var problems = GraphComponentValidator.Problems(graph, [], NullLogger.Instance).Select(problem => problem.Reason).ToList();

        problems.ShouldContain(reason => reason.Contains("Nowhere", System.StringComparison.Ordinal));
    }

    [Fact]
    public void AStepThatMayReadNoRowsIsRefused()
    {
        var graph = Graph(Stored(maxRows: 0));

        var problems = GraphComponentValidator.Problems(graph, [], NullLogger.Instance).Select(problem => problem.Reason).ToList();

        problems.ShouldContain(reason => reason.Contains("not at least one", System.StringComparison.Ordinal));
    }

    [Fact]
    public void AStepUsesTheStoreThatHoldsTheOptionsAsAReadAndNotTheStoreTheyAreAboutAsSql()
    {
        var graph = Graph(Stored());

        var uses = GraphPipelineUses.Of(graph, new Dictionary<string, object?>(), NullLogger.Instance);

        uses.IsSuccess.ShouldBeTrue(uses.CurrentMessage);
        var use = uses.Value.ShouldNotBeNull().ShouldHaveSingleItem();
        use.Relation.ShouldBe(PipelineUseRelations.Reads);
        use.Kind.ShouldBe(PipelineUseKinds.DataStore);
        use.Name.ShouldBe("Settings");
    }

    private sealed class Capture : IFlowStep
    {
        private readonly List<IReadOnlyList<string>> _batches;

        public Capture(string name, List<IReadOnlyList<string>> batches)
        {
            Name = name;
            _batches = batches;
        }

        public string Name { get; }

        public Task<IGenericResult> Run(FlowStepContext context, System.Threading.CancellationToken cancellationToken = default)
        {
            context.Variables.TryGet("Batch", out var batch);
            _batches.Add(((IEnumerable<object?>)batch!).Select(item => (string)item!).ToList());
            return Task.FromResult(GenericResult.Success());
        }
    }
}
