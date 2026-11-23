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
/// Tests for the Read data options step (FDW-5347, FDW-6344): it reads the current, active rules of a column and their values in one query through the ETL gateway, applies
/// them (In puts its values in the list, NotIn takes them out) and keeps the list they leave in a declared variable.
/// </summary>
public sealed class ReadDataOptionsTests
{
    private const string InPath = "DataConfigurationRules:Comparison:In.To";
    private const string NotInPath = "DataConfigurationRules:Comparison:NotIn.To";

    private static Dictionary<string, object?> Row(string rule, string path, string? value, int ordinal = 1, string valueKind = "Literal", string? parameter = null) =>
        new Dictionary<string, object?>
        {
            ["RuleId"] = rule, ["RuleName"] = "Rule " + rule, ["RulePath"] = path, ["ValueKind"] = valueKind, ["Value"] = value, ["Parameter"] = parameter, ["Ordinal"] = ordinal,
        };

    private static Dictionary<string, object?> Include(string value, int ordinal = 1, string rule = "A") => Row(rule, InPath, value, ordinal);

    private static Dictionary<string, object?> Exclude(string value, int ordinal = 1, string rule = "B") => Row(rule, NotInPath, value, ordinal);

    private static FlowReadDataOptions Step(ScriptingGateway fake, int maxRows = 10, string rulesContainer = "data.DataConfigurationRule", string resultVariable = "Values") =>
        new("options", fake.Gateway, "Settings", rulesContainer, "data.DataConfigurationRuleValue", "Warehouse", "dbo", "Orders", "Region", maxRows, resultVariable);

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
    public async Task TheStepAppliesTheRulesAndKeepsTheListInTheVariable()
    {
        var fake = new ScriptingGateway([Include("A", 1), Include("B", 2), Include("C", 3), Exclude("B")]);
        var variables = Declared();

        var ran = await Run(Step(fake), variables);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Kept(variables).ShouldBeAssignableTo<IReadOnlyList<string>>().ShouldBe(["A", "C"]);
    }

    [Fact]
    public async Task TwoRulesOnOneColumnAreBothApplied()
    {
        var fake = new ScriptingGateway([Include("A", 1, "R1"), Include("B", 1, "R2"), Exclude("A", 1, "R3")]);
        var variables = Declared();

        var ran = await Run(Step(fake), variables);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Kept(variables).ShouldBeAssignableTo<IReadOnlyList<string>>().ShouldBe(["B"]);
    }

    [Fact]
    public async Task TheValuesAreListedInTheRulesAndValuesOwnOrderWhateverOrderTheStoreReturnsThem()
    {
        var shuffled = new ScriptingGateway([Exclude("A"), Include("C", 3), Include("B", 2), Include("A", 1)]);
        var variables = Declared();

        await Run(Step(shuffled), variables);

        Kept(variables).ShouldBeAssignableTo<IReadOnlyList<string>>().ShouldBe(["B", "C"]);
    }

    [Fact]
    public async Task AValueTwoRulesPickIsInTheListOnce()
    {
        var fake = new ScriptingGateway([Include("A", 1, "R1"), Include("A", 1, "R2")]);
        var variables = Declared();

        await Run(Step(fake), variables);

        Kept(variables).ShouldBeAssignableTo<IReadOnlyList<string>>().ShouldBe(["A"]);
    }

    [Fact]
    public async Task TheRulesAreReadInOneQueryThatTakesOnlyCurrentNotDeletedActiveRulesAndValuesWithTheColumnAsParameters()
    {
        var fake = new ScriptingGateway([Include("A")]);

        await Run(Step(fake, maxRows: 10), Declared());

        var (store, command) = fake.Commands.ShouldHaveSingleItem();
        store.ShouldBe("Settings");
        var stream = command.ShouldBeOfType<ScriptStreamCommand>();
        stream.Parameters.ShouldBe(
            [
                new ScriptParameter("DataStore", "Warehouse"), new ScriptParameter("DataPath", "dbo"), new ScriptParameter("Container", "Orders"), new ScriptParameter("Column", "Region"),
                new ScriptParameter("Active", true), new ScriptParameter("IsCurrent", true), new ScriptParameter("IsDeleted", false),
            ]);
        stream.Take.ShouldBe(11);
        stream.Text.ShouldContain("FROM data.DataConfigurationRule r INNER JOIN data.DataConfigurationRuleValue v ");
        stream.Text.ShouldContain("\"IsCurrent\" = @IsCurrent");
        stream.Text.ShouldContain("\"IsDeleted\" = @IsDeleted");
        stream.Text.ShouldContain("\"Active\" = @Active");
    }

    [Fact]
    public async Task AnEmptyColumnKeepsAnEmptyList()
    {
        var variables = Declared();

        var ran = await Run(Step(new ScriptingGateway([])), variables);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Kept(variables).ShouldBeAssignableTo<IReadOnlyList<string>>().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("NoSuchRoot:Comparison:In.To")]
    [InlineData("DataConfigurationRules:Nothing:In.To")]
    [InlineData("DataConfigurationRules:Comparison:In.Nothing")]
    public async Task APathThatDoesNotResolveFailsTheStepNamingTheRuleAndThePathAndLeavesTheVariableAlone(string path)
    {
        var variables = Declared();

        var ran = await Run(Step(new ScriptingGateway([Include("A"), Row("R9", path, "A")])), variables);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("Rule R9");
        ran.CurrentMessage.ShouldContain(path);
        Kept(variables).ShouldBeNull();
    }

    [Fact]
    public async Task AStoredPathThatIsNotAPathAndAMethodFailsTheStep()
    {
        var variables = Declared();

        var ran = await Run(Step(new ScriptingGateway([Row("R9", "DataConfigurationRules:Comparison:In", "A")])), variables);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("Rule R9");
        Kept(variables).ShouldBeNull();
    }

    [Theory]
    [InlineData("Equal")]
    [InlineData("GreaterThan")]
    [InlineData("Between")]
    public async Task AKindThatIsNotAListOfPickedValuesFailsTheStepNamingTheRuleAndItsPath(string kind)
    {
        var variables = Declared();
        var path = $"DataConfigurationRules:Comparison:{kind}.To";

        var ran = await Run(Step(new ScriptingGateway([Include("A"), Row("R9", path, "A")])), variables);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("Rule R9");
        ran.CurrentMessage.ShouldContain(path);
        Kept(variables).ShouldBeNull();
    }

    private static FlowVariables DeclaredWithParameter(string name, object? value)
    {
        var variables = Declared();
        variables.DeclareParameter(name, value, false);
        return variables;
    }

    [Fact]
    public async Task AValueThatIsAParameterIsReadFromTheRunsValueAndPlacedInTheList()
    {
        var variables = DeclaredWithParameter("Conference", "East");

        var ran = await Run(Step(new ScriptingGateway([Include("A", 1), Row("R9", InPath, null, 2, "Parameter", "Conference")])), variables);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Kept(variables).ShouldBeAssignableTo<IReadOnlyList<string>>().ShouldBe(["A", "East"]);
    }

    [Fact]
    public async Task AParameterValueOfARuleThatExcludesTakesTheValueOutOfTheList()
    {
        var variables = DeclaredWithParameter("Drop", "B");

        var ran = await Run(Step(new ScriptingGateway([Include("A", 1), Include("B", 2), Row("R9", NotInPath, null, 1, "Parameter", "Drop")])), variables);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Kept(variables).ShouldBeAssignableTo<IReadOnlyList<string>>().ShouldBe(["A"]);
    }

    [Fact]
    public async Task AParameterTheRunDoesNotCarryFailsTheStepWithTheResolversMessageAndSetsNoVariable()
    {
        var variables = Declared();

        var ran = await Run(Step(new ScriptingGateway([Include("A"), Row("R9", InPath, null, 1, "Parameter", "Conference")])), variables);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("Conference");
        ran.CurrentMessage.ShouldContain("Rule R9");
        Kept(variables).ShouldBeNull();
    }

    [Fact]
    public async Task AParameterTheRunLeftBlankFailsTheStepAndIsNeverAnEmptyValue()
    {
        var variables = Declared();
        variables.DeclareBlankParameter("Conference", false);

        var ran = await Run(Step(new ScriptingGateway([Row("R9", InPath, null, 1, "Parameter", "Conference")])), variables);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("Conference");
        Kept(variables).ShouldBeNull();
    }

    [Fact]
    public async Task AParameterValueThatCannotBePlacedFailsTheStep()
    {
        var variables = DeclaredWithParameter("Conference", "East and West");

        var ran = await Run(Step(new ScriptingGateway([Row("R9", InPath, null, 1, "Parameter", "Conference")])), variables);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("Conference");
        Kept(variables).ShouldBeNull();
    }

    [Theory]
    [InlineData(null, "Parameter is missing")]
    [InlineData("  ", "Parameter is missing")]
    [InlineData("Con ference", "Parameter is not a parameter name")]
    [InlineData("1Conference", "Parameter is not a parameter name")]
    [InlineData("A}} {{B", "Parameter is not a parameter name")]
    public async Task AParameterRowWithNoNameOrANameThatIsNotAParameterNameFailsTheStep(string? parameter, string reason)
    {
        var variables = DeclaredWithParameter("Conference", "East");

        var ran = await Run(Step(new ScriptingGateway([Row("R9", InPath, null, 1, "Parameter", parameter)])), variables);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain(reason);
        Kept(variables).ShouldBeNull();
    }

    [Fact]
    public async Task ALiteralThatHoldsBracesIsPlacedAsStoredAndIsNeverResolved()
    {
        var variables = DeclaredWithParameter("Conference", "East");

        var ran = await Run(Step(new ScriptingGateway([Row("R9", InPath, "{{Conference}}")])), variables);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        Kept(variables).ShouldBeAssignableTo<IReadOnlyList<string>>().ShouldBe(["{{Conference}}"]);
    }

    [Fact]
    public async Task AValueKindThatIsNotAValueKindFailsTheStep()
    {
        var ran = await Run(Step(new ScriptingGateway([Row("R9", InPath, "A", 1, "Sometimes")])), Declared());

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("ValueKind");
    }

    [Theory]
    [InlineData("RuleId")]
    [InlineData("RuleName")]
    [InlineData("RulePath")]
    [InlineData("ValueKind")]
    public async Task ARowThatNamesNoRuleFieldFailsTheStepNamingTheRowAndTheField(string field)
    {
        var row = Include("A");
        row[field] = null;

        var ran = await Run(Step(new ScriptingGateway([Include("B"), row])), Declared());

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("row 2");
        ran.CurrentMessage.ShouldContain(field);
    }

    [Fact]
    public async Task AnOrdinalThatIsNotAWholeNumberFailsTheStep()
    {
        var row = Include("A");
        row["Ordinal"] = "first";

        var ran = await Run(Step(new ScriptingGateway([row])), Declared());

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("Ordinal");
    }

    [Fact]
    public async Task MoreRowsThanTheStepMayReadFailTheStep()
    {
        var variables = Declared();

        var ran = await Run(Step(new ScriptingGateway([Include("A", 1), Include("B", 2), Include("C", 3)]), maxRows: 2), variables);

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

        var ran = await Run(Step(fake, rulesContainer: container), Declared());

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
        var fake = new ScriptingGateway([Include("A", 1), Include("B", 2), Include("C", 3)]);
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
        var fake = new ScriptingGateway([Include("A", 1), Include("B", 2), Include("C", 3)]);
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
            ["optionsDataStore", "optionsContainer", "optionValuesContainer", "dataStore", "path", "container", "field", "maxRows", "resultVariable"], ignoreOrder: true);
        kind.Settings.ShouldAllBe(setting => setting.Name == "stage" || ComponentSettingBindings.Find(setting.Name) != null);
    }

    private static PipelineComponentConfiguration Stored(string? resultVariable = "Values", int? maxRows = 10) => new()
    {
        Name = "options",
        ComponentType = "ReadDataOptions",
        OptionsDataStoreName = "Settings",
        OptionsContainerName = "data.DataConfigurationRule",
        OptionValuesContainerName = "data.DataConfigurationRuleValue",
        DataStoreName = "Warehouse",
        PathName = "dbo",
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
