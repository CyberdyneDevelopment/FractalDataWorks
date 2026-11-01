using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Scripts;
using Fdw.Services.EtlGateway;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Scripts;

/// <summary>
/// Tests for the stored script API (FDW-1442): the check names every problem with the part of the draft it concerns, what a save
/// reads is what a read writes, and a run states its rows, gives every required value and reads each as its declared type.
/// </summary>
public sealed class ScriptPayloadTests
{
    private static ScriptPayload Sample() => new()
    {
        Name = "SeasonGames",
        Description = "Games of one season",
        Connection = "Warehouse",
        Kind = "Query",
        Text = "SELECT * FROM dbo.Game WHERE Season = @Season",
        Parameters = [new ScriptParameterPayload { Name = "Season", Type = "Int32", Required = true }],
    };

    private static (IEtlGateway Gateway, List<(string Store, IScriptedDataCommand Command)> Calls) Gateway(int affected = 0, int rows = 2)
    {
        List<(string Store, IScriptedDataCommand Command)> calls = [];
        var mock = new Mock<IEtlGateway>();
        mock.Setup(g => g.Execute<IReadOnlyList<IDictionary<string, object?>>>(
                It.IsAny<IScriptedDataCommand<IReadOnlyList<IDictionary<string, object?>>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((IScriptedDataCommand<IReadOnlyList<IDictionary<string, object?>>> command, string store, CancellationToken token) =>
            {
                calls.Add((store, command));
                IReadOnlyList<IDictionary<string, object?>> result =
                    [.. Enumerable.Range(0, rows).Select(i => (IDictionary<string, object?>)new Dictionary<string, object?> { ["Id"] = i })];
                return Task.FromResult<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>>(
                    GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Success(result));
            });
        mock.Setup(g => g.Execute<int>(It.IsAny<IScriptedDataCommand<int>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((IScriptedDataCommand<int> command, string store, CancellationToken token) =>
            {
                calls.Add((store, command));
                return Task.FromResult<IGenericResult<int>>(GenericResult<int>.Success(affected));
            });
        return (mock.Object, calls);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASoundDraftHasNoProblems()
    {
        ScriptChecker.Problems(Sample()).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EveryProblemOfADraftIsNamedWithThePartItConcerns()
    {
        var draft = new ScriptPayload { Kind = "Merge" };
        draft.Parameters.Add(new ScriptParameterPayload { Name = "A", Type = "Nothing" });
        draft.Parameters.Add(new ScriptParameterPayload { Name = "a", Type = "Int32" });

        var problems = ScriptChecker.Problems(draft);

        problems.Select(problem => problem.Path[0]).ShouldBe(["name", "connection", "text", "kind", "parameters", "parameters"], ignoreOrder: true);
        problems.Single(problem => problem.Reason.Contains("Nothing")).Path.ShouldBe(["parameters", "A", "type"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void WhatASaveReadsIsWhatAReadWrites()
    {
        var read = ScriptPayloadReader.Read(Sample(), System.Guid.CreateVersion7());

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value!.Domain.ShouldBe("Script");
        read.Value.Implementation.ShouldBe("Sql");
        var written = ScriptPayloadWriter.Write(read.Value);
        written.Name.ShouldBe("SeasonGames");
        written.Connection.ShouldBe("Warehouse");
        written.Kind.ShouldBe("Query");
        written.Text.ShouldBe(Sample().Text);
        written.Parameters.Single().Name.ShouldBe("Season");
        written.Parameters.Single().Required.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADraftThatIsNotSoundIsRefusedWhole()
    {
        var read = ScriptPayloadReader.Read(new ScriptPayload { Name = "X" }, System.Guid.CreateVersion7());

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("data store");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AQueryRunsOnItsStoreWithTheValueReadAsItsTypeAndAnswersRows()
    {
        var (gateway, calls) = Gateway(rows: 3);

        var ran = await new ScriptService(gateway).Run(Sample(), new ScriptRunPayload { Values = { ["Season"] = "2024" }, MaxRows = 10 }, TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        ran.Value!.Rows!.Count.ShouldBe(3);
        ran.Value.HasMore.ShouldBe(false);
        ran.Value.Columns.ShouldNotBeNull().Select(column => (column.Name, column.Type)).ShouldBe([("Id", "Int32")]);
        var (store, command) = calls.ShouldHaveSingleItem();
        store.ShouldBe("Warehouse");
        var read = command.ShouldBeOfType<ScriptReadCommand>();
        read.Take.ShouldBe(11);
        read.Parameters.ShouldBe([new ScriptParameter("Season", 2024)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AQueryThatHasMoreRowsThanAskedForAnswersTheMostRowsAndSaysThereIsMore()
    {
        var (gateway, _) = Gateway(rows: 4);

        var ran = await new ScriptService(gateway).Run(Sample(), new ScriptRunPayload { Values = { ["Season"] = "2024" }, MaxRows = 3 }, TestContext.Current.CancellationToken);

        ran.Value!.Rows!.Count.ShouldBe(3);
        ran.Value.HasMore.ShouldBe(true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AQueryRunWithoutItsMostRowsIsRefusedAndNothingIsRun()
    {
        var (gateway, calls) = Gateway();

        var ran = await new ScriptService(gateway).Run(Sample(), new ScriptRunPayload { Values = { ["Season"] = "2024" } }, TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        calls.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ANonQueryAnswersHowManyRowsItAffectedAndStatesNoMostRows()
    {
        var (gateway, calls) = Gateway(affected: 7);
        var script = new ScriptPayload { Name = "Purge", Connection = "Warehouse", Kind = "NonQuery", Text = "DELETE FROM dbo.Game" };

        var ran = await new ScriptService(gateway).Run(script, new ScriptRunPayload(), TestContext.Current.CancellationToken);
        var withMostRows = await new ScriptService(gateway).Run(script, new ScriptRunPayload { MaxRows = 5 }, TestContext.Current.CancellationToken);

        ran.Value!.RowsAffected.ShouldBe(7);
        calls.ShouldHaveSingleItem().Command.ShouldBeOfType<ScriptRunCommand>();
        withMostRows.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AMissingRequiredValueAnUnknownValueAndAValueOfTheWrongTypeAreRefused()
    {
        var (gateway, calls) = Gateway();

        var missing = await new ScriptService(gateway).Run(Sample(), new ScriptRunPayload { MaxRows = 1 }, TestContext.Current.CancellationToken);
        var unknown = await new ScriptService(gateway).Run(Sample(), new ScriptRunPayload { Values = { ["Season"] = "1", ["Year"] = "2" }, MaxRows = 1 }, TestContext.Current.CancellationToken);
        var wrong = await new ScriptService(gateway).Run(Sample(), new ScriptRunPayload { Values = { ["Season"] = "many" }, MaxRows = 1 }, TestContext.Current.CancellationToken);

        missing.CurrentMessage.ShouldNotBeNull().ShouldContain("Season");
        unknown.CurrentMessage.ShouldNotBeNull().ShouldContain("Year");
        wrong.CurrentMessage.ShouldNotBeNull().ShouldContain("many");
        calls.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AValueIsFoundForItsParameterWhateverTheCaseOfItsName()
    {
        var (gateway, calls) = Gateway();

        var ran = await new ScriptService(gateway).Run(
            Sample(), new ScriptRunPayload { Values = new Dictionary<string, string?> { ["season"] = "2024" }, MaxRows = 5 }, TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        calls.ShouldHaveSingleItem().Command.ShouldBeOfType<ScriptReadCommand>().Parameters.ShouldBe([new ScriptParameter("Season", 2024)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AKindNobodyKnowsIsRefusedAndNothingIsRun()
    {
        var (gateway, calls) = Gateway();
        var script = Sample();
        script.Kind = "Merge";

        var ran = await new ScriptService(gateway).Run(
            script, new ScriptRunPayload { Values = { ["Season"] = "2024" }, MaxRows = 5 }, TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        calls.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheKindsOfScriptAreQueryAndNonQueryAndOnlyAQueryAnswersRows()
    {
        ScriptKinds.All().Select(kind => kind.Name).ShouldBe(["NonQuery", "Query"], ignoreOrder: true);
        ScriptKinds.ByName("Query").AnswersRows.ShouldBeTrue();
        ScriptKinds.ByName("NonQuery").AnswersRows.ShouldBeFalse();
    }
}
