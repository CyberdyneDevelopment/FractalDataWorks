using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for the SQL source component and the Execute SQL step (FDW-1443): a script runs through the data gateway with its
/// named parameters, a read holds to the most rows it states, and a kind missing a setting is refused with a message naming it.
/// </summary>
public sealed class ScriptKindsTests
{
    private static List<IDictionary<string, object?>> Rows(int count) =>
        [.. Enumerable.Range(0, count).Select(i => (IDictionary<string, object?>)new Dictionary<string, object?> { ["Id"] = i })];

    private static PipelineComponentConfiguration SqlSource(string name, string? script = "SELECT Id FROM dbo.T WHERE Year = @Year", int? maxRows = 10)
    {
        var component = new PipelineComponentConfiguration
        {
            Name = name, ComponentType = "SqlSource", DataStoreName = "Warehouse", ScriptText = script, MaxRows = maxRows, BatchSize = 2,
        };
        component.Fields.Add(new PipelineComponentFieldConfiguration
        {
            SettingName = "parameters", TargetField = "Year", ConstantType = "Int32", ConstantText = "2024",
        });
        return component;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AScriptSourceReadsTheScriptWithItsParametersAndSendsTheRowsInBatches()
    {
        var fake = new ScriptingGateway(Rows(5));
        var (sink, rows) = Sink("dst");
        var parameters = new[] { new ScriptParameter("Year", 2024) };
        var graph = Graph([new FlowScriptSource("src", fake.Gateway, "Warehouse", "SELECT Id FROM dbo.T", parameters, 10, 2), sink], [Link("src", "dst")]);

        var result = await new FlowExecutor().Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        rows.Count.ShouldBe(5);
        var (store, command) = fake.Commands.ShouldHaveSingleItem();
        store.ShouldBe("Warehouse");
        var read = command.ShouldBeOfType<ScriptReadCommand>();
        read.Parameters.ShouldBe(parameters);
        read.Take.ShouldBe(11);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AScriptThatSelectsMoreThanItsMostRowsFailsTheRunAndSendsNothing()
    {
        var fake = new ScriptingGateway(Rows(4));
        var (sink, rows) = Sink("dst");
        var graph = Graph([new FlowScriptSource("src", fake.Gateway, "Warehouse", "SELECT Id FROM dbo.T", [], 3, 2), sink], [Link("src", "dst")]);

        var result = await new FlowExecutor().Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("src");
        rows.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AScriptReadThatTheGatewayRefusesFailsTheRunNamingTheSource()
    {
        var fake = new ScriptingGateway([], fails: true);
        var (sink, _) = Sink("dst");
        var graph = Graph([new FlowScriptSource("src", fake.Gateway, "Warehouse", "SELECT 1", [], 3, 2), sink], [Link("src", "dst")]);

        var result = await new FlowExecutor().Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("src");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnExecuteSqlStepRunsTheCommandWithParametersMadeFromVariablesAndKeepsTheRowsAffected()
    {
        var fake = new ScriptingGateway([], affected: 7);
        var variables = new FlowVariables();
        variables.Declare("Season", 2024);
        variables.Declare("Changed", 0);
        var step = new FlowExecuteSql(
            "purge", fake.Gateway, "Warehouse", "DELETE FROM dbo.T WHERE Year = @Year",
            [new FlowSqlParameter("Year", VariableText.Typed("{{Season}}"))], "Changed");

        var ran = await step.Run(GraphTestKit.StepContext(variables, new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        variables.TryGet("Changed", out var changed).ShouldBeTrue();
        changed.ShouldBe(7);
        var (store, command) = fake.Commands.ShouldHaveSingleItem();
        store.ShouldBe("Warehouse");
        command.ShouldBeOfType<ScriptRunCommand>().Parameters.ShouldBe([new ScriptParameter("Year", 2024)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AParameterThatIsOneNullVariableSendsNullAndNeverTheEmptyText()
    {
        var fake = new ScriptingGateway([], affected: 1);
        var variables = new FlowVariables();
        variables.Declare("Season", null);
        var step = new FlowExecuteSql(
            "purge", fake.Gateway, "Warehouse", "DELETE FROM dbo.T WHERE Year = @Year",
            [new FlowSqlParameter("Year", VariableText.Typed("{{Season}}")), new FlowSqlParameter("Label", VariableText.Typed("Year {{Season}}"))], null);

        var ran = await step.Run(GraphTestKit.StepContext(variables, new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        fake.Commands.ShouldHaveSingleItem().Command.ShouldBeOfType<ScriptRunCommand>().Parameters
            .ShouldBe([new ScriptParameter("Year", null), new ScriptParameter("Label", "Year ")]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AResultVariableThatIsNotDeclaredIsRefusedBeforeTheCommandRuns()
    {
        var fake = new ScriptingGateway([], affected: 1);
        var step = new FlowExecuteSql("purge", fake.Gateway, "Warehouse", "DELETE FROM dbo.T", [], "Missing");

        var ran = await step.Run(GraphTestKit.StepContext(new FlowVariables(), new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        fake.Commands.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnExecuteSqlStepWhoseCommandIsRefusedFailsNamingTheStep()
    {
        var fake = new ScriptingGateway([], fails: true);
        var step = new FlowExecuteSql("purge", fake.Gateway, "Warehouse", "DELETE FROM dbo.T", [], null);

        var ran = await step.Run(GraphTestKit.StepContext(new FlowVariables(), new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("purge");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnExecuteSqlStepWhoseResultVariableIsNotDeclaredFailsNamingTheVariable()
    {
        var fake = new ScriptingGateway([], affected: 1);
        var step = new FlowExecuteSql("purge", fake.Gateway, "Warehouse", "DELETE FROM dbo.T", [], "Missing");

        var ran = await step.Run(GraphTestKit.StepContext(new FlowVariables(), new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("Missing");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AStoredSqlSourceAndExecuteSqlStepAreBuilt()
    {
        var execute = new PipelineComponentConfiguration
        {
            Name = "purge", ComponentType = "ExecuteSql", DataStoreName = "Warehouse", ScriptText = "DELETE FROM dbo.T WHERE Year = @Year", Stage = 1,
        };
        execute.Parameters.Add(new PipelineComponentParameterConfiguration { ParameterName = "Year", ValueText = "{{Season}}" });
        var graph = GraphTestKit.Steps(
            GraphTestKit.DataFlow("flow", [SqlSource("src"), GraphTestKit.Mapped("map", ("Key", "Id"))], [GraphTestKit.Link("src", "map")], [GraphTestKit.Dropped("map")]),
            execute);
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "Season", DataType = "Int32", ValueText = "2024" });

        var built = GraphFlowBuilder.Build(graph, GraphTestKit.Context(graph, new ScriptingGateway([])));

        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASqlSourceWithNoScriptIsRefusedNamingItAndTheSetting()
    {
        var graph = GraphTestKit.Steps(GraphTestKit.DataFlow("flow", [SqlSource("src", script: null)], [], [GraphTestKit.Dropped("src")]));

        var built = GraphFlowBuilder.Build(graph, GraphTestKit.Context(graph, new ScriptingGateway([])));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("src");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AScriptParameterTakenFromAFieldIsRefusedBecauseAScriptHasNoRow()
    {
        var component = SqlSource("src");
        component.Fields[0].SourceField = "Id";
        component.Fields[0].ConstantType = null;
        component.Fields[0].ConstantText = null;
        var graph = GraphTestKit.Steps(GraphTestKit.DataFlow("flow", [component], [], [GraphTestKit.Dropped("src")]));

        var built = GraphFlowBuilder.Build(graph, GraphTestKit.Context(graph, new ScriptingGateway([])));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("Year");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnExecuteSqlStepThatNamesAVariableNobodyDeclaredOrKeepsItsResultInOneIsReported()
    {
        var step = new PipelineComponentConfiguration
        {
            Name = "purge", ComponentType = "ExecuteSql", DataStoreName = "Warehouse", ScriptText = "DELETE FROM dbo.T", ResultVariable = "Changed",
        };
        step.Parameters.Add(new PipelineComponentParameterConfiguration { ParameterName = "Year", ValueText = "{{Season}}" });

        var problems = GraphShapeValidator.Problems(GraphTestKit.Steps(step), NullLogger.Instance);

        problems.Select(problem => problem.Path[^1]).ShouldBe(["resultVariable", "parameters"], ignoreOrder: true);
    }
}
