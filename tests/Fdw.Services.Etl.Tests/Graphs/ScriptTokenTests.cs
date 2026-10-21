using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-1970: a SQL script names a schema, table or column as <c>$(Name)</c>, taken from a parameter or variable when the step runs. Values stay
/// <c>@parameters</c>, so a value put into the text must be a name (letters, digits, underscore and dot) and anything else refuses the script
/// without running it and without writing the value.
/// </summary>
public sealed class ScriptTokenTests
{
    private static IReadOnlyDictionary<string, object?> Values(params (string Name, object? Value)[] values) =>
        values.ToDictionary(pair => pair.Name, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TheTokensOfATextAreFoundAndATextWithNoneIsLeftAlone()
    {
        ScriptTokens.Holds("SELECT * FROM dbo.$(Table) WHERE Year = @Year").ShouldBeTrue();
        ScriptTokens.Holds("SELECT * FROM dbo.T WHERE Year = @Year").ShouldBeFalse();
        ScriptTokens.Names("SELECT $( A ), $(B) FROM $(A)").ShouldBe(["A", "B", "A"]);

        var untouched = ScriptTokens.Render("run", "SELECT 1", Values(), NullLogger.Instance);

        untouched.IsSuccess.ShouldBeTrue(untouched.CurrentMessage);
        untouched.Value.ShouldBe("SELECT 1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ATokenIsReplacedByTheValueOfItsNameWhateverTheCase()
    {
        var rendered = ScriptTokens.Render(
            "run", "SELECT * FROM $(schema).$(TABLE), $(Table)", Values(("Schema", "BreweryData"), ("table", "BreweryMicro")), NullLogger.Instance);

        rendered.IsSuccess.ShouldBeTrue(rendered.CurrentMessage);
        rendered.Value.ShouldBe("SELECT * FROM BreweryData.BreweryMicro, BreweryMicro");
    }

    [Theory]
    [InlineData("x; DROP TABLE T")]
    [InlineData("T'--")]
    [InlineData("[T]")]
    [InlineData("a b")]
    [InlineData("")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AValueThatIsNotANameIsRefusedAndIsNotWritten(string value)
    {
        var rendered = ScriptTokens.Render("run", "SELECT * FROM $(Table)", Values(("Table", value)), NullLogger.Instance);

        rendered.IsFailure.ShouldBeTrue();
        rendered.CurrentMessage.ShouldNotBeNull().ShouldContain("Table");
        if (value.Length > 0)
        {
            rendered.CurrentMessage.ShouldNotContain(value, Case.Sensitive);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ANameWithNoValueIsRefusedNamingTheOwnerAndTheName()
    {
        var absent = ScriptTokens.Render("purge", "SELECT * FROM $(Table)", Values(), NullLogger.Instance);
        var empty = ScriptTokens.Render("purge", "SELECT * FROM $(Table)", Values(("Table", null)), NullLogger.Instance);

        foreach (var rendered in new[] { absent, empty })
        {
            rendered.IsFailure.ShouldBeTrue();
            rendered.CurrentMessage.ShouldNotBeNull().ShouldContain("purge");
            rendered.CurrentMessage.ShouldContain("Table");
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnExecuteSqlStepRunsItsTextWithTheTokenReplacedByTheVariableAsTheStepsBeforeLeftIt()
    {
        var fake = new ScriptingGateway([], affected: 0);
        var variables = new FlowVariables();
        variables.Declare("Table", null);
        variables.TrySet("Table", "BreweryMicro");
        var step = new FlowExecuteSql("purge", fake.Gateway, "Warehouse", "DELETE FROM dbo.$(Table) WHERE Year = @Year", [], null);

        var ran = await step.Run(StepContext(variables, new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        fake.Commands.ShouldHaveSingleItem().Command.ShouldBeOfType<ScriptRunCommand>().Text.ShouldBe("DELETE FROM dbo.BreweryMicro WHERE Year = @Year");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnExecuteSqlStepWhoseTokenValueIsNotANameRunsNothing()
    {
        var fake = new ScriptingGateway([], affected: 0);
        var variables = new FlowVariables();
        variables.Declare("Table", "T; DROP TABLE U");
        var step = new FlowExecuteSql("purge", fake.Gateway, "Warehouse", "DELETE FROM dbo.$(Table)", [], null);

        var ran = await step.Run(StepContext(variables, new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        fake.Commands.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ASqlSourceThatNamesATokenIsBuiltWhenItsStageStartsAndItsScriptIsRenderedThen()
    {
        var source = new PipelineComponentConfiguration
        {
            Name = "src", ComponentType = "SqlSource", DataStoreName = "Warehouse", ScriptText = "SELECT * FROM dbo.$(Table)", MaxRows = 10, BatchSize = 2,
        };
        var graph = Graph([source, Mapped("map", ("Key", "Id"))], [Link("src", "map")], Dropped("map"));

        // Why this shows it is deferred: a source built now would refuse for want of an ETL gateway, which this context has none of.
        var deferred = GraphFlowBuilder.Build(graph, ContextWith(graph, new Dictionary<string, object?>()));

        deferred.IsSuccess.ShouldBeTrue(deferred.CurrentMessage);

        var rendered = ComponentParameters.RenderLate(source, Values(("Table", "BreweryMicro")), NullLogger.Instance);

        rendered.IsSuccess.ShouldBeTrue(rendered.CurrentMessage);
        rendered.Value!.ScriptText.ShouldBe("SELECT * FROM dbo.BreweryMicro");
        source.ScriptText.ShouldBe("SELECT * FROM dbo.$(Table)");
    }

    private static PipelineComponentConfiguration ExecuteSql(string script) => new()
    {
        Name = "purge", ComponentType = "ExecuteSql", DataStoreName = "Warehouse", ScriptText = script, Stage = 5,
    };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheSaveCheckRefusesATokenThatNamesNothingTheStepCanSee()
    {
        var graph = Steps(ExecuteSql("DELETE FROM dbo.$(Nowhere)"));

        var problem = GraphShapeValidator.Problems(graph, NullLogger.Instance).ShouldHaveSingleItem();

        problem.Path.ShouldBe(["components", "purge", "settings", "script"]);
        problem.Reason.ShouldContain("Nowhere");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheSaveCheckAcceptsATokenThatNamesAParameterOrAVariable()
    {
        var graph = Steps(ExecuteSql("DELETE FROM dbo.$(Table) WHERE C = $(Column)"));
        graph.Parameters.Add(new PipelineParameterConfiguration { Name = "Table", ParameterType = "String" });
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "Column", DataType = "String", ValueText = "Id" });

        GraphShapeValidator.Problems(graph, NullLogger.Instance).ShouldBeEmpty();
    }

    private static GraphPipelineConfiguration SqlSourceAfter(int sourceOrdinal)
    {
        var source = new PipelineComponentConfiguration
        {
            Name = "src", ComponentType = "SqlSource", DataStoreName = "Warehouse", ScriptText = "SELECT * FROM dbo.$(Table)", MaxRows = 10, BatchSize = 2,
        };
        var flow = DataFlow("flow", [source, Mapped("map", ("Key", "Id"))], [Link("src", "map")], [Dropped("map")], ordinal: sourceOrdinal);
        var sets = new PipelineComponentConfiguration { Name = "calc", ComponentType = "SetVariable", VariableName = "Table", ConstantType = "String", ConstantText = "T", Stage = 5 };
        var graph = Steps(sets, flow);
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "Table", DataType = "String", ValueText = "" });
        return graph;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ASqlSourceMayNameAVariableAComponentOfALowerStageSetsAndNotOneThatNoLowerStageSets()
    {
        GraphShapeValidator.Problems(SqlSourceAfter(sourceOrdinal: 6), NullLogger.Instance)
            .ShouldNotContain(problem => problem.Reason.Contains("$(Table)", StringComparison.Ordinal));

        GraphShapeValidator.Problems(SqlSourceAfter(sourceOrdinal: 4), NullLogger.Instance)
            .ShouldContain(problem => problem.Reason.Contains("no component of a lower stage", StringComparison.Ordinal) && problem.Reason.Contains("$(Table)", StringComparison.Ordinal));
    }
}
