using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Graphs;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Describing;
using Fdw.Services.Data.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-5522: a field out fed by a field its output does not have is a problem at the field out, on validate and on save; an output whose fields are not known cannot be judged and is
/// a note that does not block.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataSets.FDW5522")]
public sealed class GraphDataSetFedFieldsTests
{
    private static string CodeOf(string name) => DataServiceResultCodes.ByName(name).Code;

    private static CreateGraphDataSetRequest Request(string feedField, GraphComponentPayload source) => new()
    {
        Name = "Teams",
        FieldsOut = [new GraphFieldOutRequest { Name = "Name", DataType = "String", IsFilterable = true, IsNullable = true, FedByComponent = "src", FedByField = feedField }],
        FieldsOutComponent = "src",
        FieldsOutOutput = "output",
        Components = [source],
    };

    private static GraphComponentPayload DataSetSource() => new()
    {
        Name = "src",
        Kind = "DataSetSource",
        Settings = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["dataSet"] = "NflTeams", ["batchSize"] = 3 },
    };

    private static GraphComponentPayload ContainerSource() => new()
    {
        Name = "src",
        Kind = "ContainerSource",
        Settings = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["dataStore"] = "NflDb", ["container"] = "Team", ["path"] = "NflData", ["batchSize"] = 3 },
    };

    private static GraphComponentPayload SqlSource() => new()
    {
        Name = "src",
        Kind = "SqlSource",
        Settings = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["dataStore"] = "NflDb", ["script"] = "select 1 as Id", ["maxRows"] = 100, ["batchSize"] = 3 },
    };

    private static StubGraphDataSetSources TeamsWith(params string[] fields) => new StubGraphDataSetSources().WithFields("NflTeams", fields);

    private static async Task<GraphDataSetValidationPayload> Validate(CreateGraphDataSetRequest request, StubGraphDataSetSources sources) =>
        (await GraphDataSetValidator.Validate(request, sources, NullLogger.Instance, TestContext.Current.CancellationToken)).Value!;

    [Fact]
    public async Task AFieldTheDataSetDoesNotHaveIsReportedAtTheFieldOutAndTheDraftIsInvalid()
    {
        var answer = await Validate(Request("NoSuchField", DataSetSource()), TeamsWith("Id", "Name"));

        answer.IsValid.ShouldBeFalse();
        var problem = answer.Problems.ShouldHaveSingleItem();
        problem.Code.ShouldBe(CodeOf("GraphDataSetFedFieldMissing"));
        string.Join('/', problem.Path).ShouldBe("fieldsOut/Name");
        problem.Message.ShouldContain("NoSuchField");
        answer.NotChecked.ShouldBeEmpty();
    }

    [Fact]
    public async Task AFieldTheDataSetHasInAnotherCaseIsNotReportedAndNothingIsLeftUnchecked()
    {
        var answer = await Validate(Request("name", DataSetSource()), TeamsWith("Id", "Name"));

        answer.IsValid.ShouldBeTrue();
        answer.Problems.ShouldBeEmpty();
        answer.NotChecked.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnOutputWhoseFieldsAreNotKnownByDesignIsANoteThatTheFieldWasNotCheckedAndNeverAProblem()
    {
        var answer = await Validate(Request("NoSuchField", SqlSource()), new StubGraphDataSetSources().WithContainer("NflDb", "Team", "NflData"));

        answer.IsValid.ShouldBeTrue();
        var note = answer.NotChecked.ShouldHaveSingleItem();
        note.Code.ShouldBe(CodeOf("GraphDataSetFedFieldNotChecked"));
        string.Join('/', note.Path).ShouldBe("fieldsOut/Name");
        note.Message.ShouldContain("NoSuchField");
        note.Message.ShouldContain("the describer derives no fields for this output");
    }

    [Fact]
    public async Task AFieldTheContainerDoesNotHoldIsReportedAtTheFieldOutAndTheDraftIsInvalid()
    {
        var sources = new StubGraphDataSetSources().WithContainerFields("NflDb", "Team", "NflData", new GraphDataSetFieldFact("Id", "Int32"), new GraphDataSetFieldFact("Name", "String"));

        var answer = await Validate(Request("NoSuchField", ContainerSource()), sources);

        answer.IsValid.ShouldBeFalse();
        var problem = answer.Problems.ShouldHaveSingleItem();
        problem.Code.ShouldBe(CodeOf("GraphDataSetFedFieldMissing"));
        string.Join('/', problem.Path).ShouldBe("fieldsOut/Name");
        answer.NotChecked.ShouldBeEmpty();
    }

    [Fact]
    public async Task AFieldTheContainerHoldsIsNotReportedAndNothingIsLeftUnchecked()
    {
        var sources = new StubGraphDataSetSources().WithContainerFields("NflDb", "Team", "NflData", new GraphDataSetFieldFact("Id", "Int32"), new GraphDataSetFieldFact("Name", "String"));

        var answer = await Validate(Request("name", ContainerSource()), sources);

        answer.IsValid.ShouldBeTrue();
        answer.Problems.ShouldBeEmpty();
        answer.NotChecked.ShouldBeEmpty();
    }

    [Fact]
    public async Task AContainerWithNoStoredFieldsIsANoteThatTheFieldWasNotCheckedAndNeverAProblem()
    {
        var sources = new StubGraphDataSetSources().WithContainerFields("NflDb", "Team", "NflData");

        var answer = await Validate(Request("Anything", ContainerSource()), sources);

        answer.IsValid.ShouldBeTrue();
        answer.Problems.ShouldBeEmpty();
        var note = answer.NotChecked.ShouldHaveSingleItem();
        note.Code.ShouldBe(CodeOf("GraphDataSetFedFieldNotChecked"));
        note.Message.ShouldContain("the describer derives no fields for this output");
    }

    [Fact]
    public async Task AContainerWhoseFieldsCannotBeReadIsANoteThatSaysTheDescriberFailedNotAnOutputNotKnownByDesign()
    {
        var answer = await Validate(Request("Name", ContainerSource()), new StubGraphDataSetSources().WithContainer("NflDb", "Team", "NflData"));

        var note = answer.NotChecked.ShouldHaveSingleItem();
        note.Code.ShouldBe(CodeOf("GraphDataSetFedFieldNotChecked"));
        note.Message.ShouldContain("the describer failed: ");
        note.Message.ShouldNotContain("derives no fields");
    }

    [Fact]
    public async Task ADescriberThatFailsIsANoteThatSaysSoWithItsOwnMessageIsNotARefusalAndWasLoggedWhereItWasMade()
    {
        var logger = new CapturingLogger();
        var draft = new GraphDataSetConfiguration { Name = "Teams", FieldsOutComponent = "src", FieldsOutOutput = "output" };
        draft.Components.Add(new GraphDataSetComponentConfiguration { Name = "src", ComponentType = "DataSetSource" });
        draft.GraphFields.Add(new GraphDataSetFieldConfiguration { Name = "Name", FedByComponent = "src", FedByField = "NoSuchField" });

        // The describer fails the way a deriver does: a coded failure made, and so logged, where it is made. The expected text is the failure's own, so it cannot drift from the code's wording.
        var failure = GenericResult<IReadOnlyList<GraphNodeFields>>.Failure(
            DataServiceResultCodes.ByName("GraphDataSetContainerNotFound"), logger, ResultDetails.Create("component", "src", "dataSet", "Teams", "container", "Team").With("dataStore", "NflDb"));
        var loggedWhenMade = logger.Entries.Count;
        Func<GraphPipelineConfiguration, DescribeContext, ILogger, CancellationToken, Task<IGenericResult<IReadOnlyList<GraphNodeFields>>>> failing = (_, _, _, _) =>
            Task.FromResult<IGenericResult<IReadOnlyList<GraphNodeFields>>>(failure);

        var judged = await GraphDataSetFedFields.Judge(
            draft, GraphDataSetPipelines.Read(draft, 1), new StubGraphDataSetSources(), failing, logger, TestContext.Current.CancellationToken);

        judged.IsSuccess.ShouldBeTrue(judged.CurrentMessage);
        judged.Value.Problems.ShouldBeEmpty();
        var note = judged.Value.Notes.ShouldHaveSingleItem();
        note.Code.ShouldBe(CodeOf("GraphDataSetFedFieldNotChecked"));
        failure.CurrentMessage.ShouldNotBeNullOrEmpty();
        note.Message.ShouldContain($"the describer failed: {failure.CurrentMessage}");
        loggedWhenMade.ShouldBeGreaterThan(0);
        logger.Entries.Take(loggedWhenMade).ShouldContain(entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public async Task AnAbstractDraftHasNothingToJudgeAndNothingLeftUnchecked()
    {
        var answer = await Validate(
            new CreateGraphDataSetRequest { Name = "Teams", FieldsOut = [new GraphFieldOutRequest { Name = "Name", DataType = "String", IsFilterable = true, IsNullable = true }] },
            new StubGraphDataSetSources());

        answer.IsValid.ShouldBeTrue();
        answer.NotChecked.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveRefusesWhatValidateCallsUnsoundAndSavesAnUnknownOutputAsItDoes()
    {
        var missing = TeamsWith("Id", "Name");
        var drafted = await GraphDataSetValidator.Draft(Request("NoSuchField", DataSetSource()), missing, NullLogger.Instance, TestContext.Current.CancellationToken);
        var refused = await GraphDataSetSoundness.Judge(drafted.Value!.Draft, missing, NullLogger.Instance, TestContext.Current.CancellationToken);

        refused.IsFailure.ShouldBeTrue();
        refused.Code.ShouldBe(DataServiceResultCodes.ByName("GraphDataSetDraftNotSound"));

        var unknown = new StubGraphDataSetSources().WithContainer("NflDb", "Team", "NflData");
        var unknownDraft = await GraphDataSetValidator.Draft(Request("NoSuchField", SqlSource()), unknown, NullLogger.Instance, TestContext.Current.CancellationToken);
        var accepted = await GraphDataSetSoundness.Judge(unknownDraft.Value!.Draft, unknown, NullLogger.Instance, TestContext.Current.CancellationToken);

        accepted.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task TheFieldsTheCheckReadsAreTheFieldsTheDescribeRoutesRead()
    {
        var provider = new Mock<IDataSetConfigurationProvider>();
        var stored = new GraphDataSetConfiguration { Name = "NflTeams" };
        stored.GraphFields.Add(new GraphDataSetFieldConfiguration { Name = "Name", TypeName = "String", Ordinal = 2 });
        stored.GraphFields.Add(new GraphDataSetFieldConfiguration { Name = "Gone", TypeName = "String", Ordinal = 0, IsDeleted = true });
        stored.GraphFields.Add(new GraphDataSetFieldConfiguration { Name = "Id", TypeName = "Int32", Ordinal = 1 });
        provider
            .Setup(p => p.Get("NflTeams", ConfigurationLevels.Children, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(stored));

        var describe = await GraphDataSetFields.Read(provider.Object, "NflTeams", TestContext.Current.CancellationToken);
        var check = await new GraphDataSetSources(provider.Object, new Mock<IDataStoreConfigurationProvider>().Object).Fields(["NflTeams"], TestContext.Current.CancellationToken);

        describe.IsSuccess.ShouldBeTrue(describe.CurrentMessage);
        check.IsSuccess.ShouldBeTrue(check.CurrentMessage);
        check.Value!["NflTeams"].Select(field => (field.Name, field.TypeName)).ShouldBe(describe.Value!.Select(field => (field.Name, field.Type)).ToList());
        string.Join(",", check.Value["NflTeams"].Select(field => field.Name)).ShouldBe("Id,Name");
    }
}
