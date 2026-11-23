using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Messages;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// FDW-6718 (slice 2e of FDW-6141): a graph pipeline states its type at a create and the type's own requirements are judged at the save, the update and the draft by ONE
/// method (<see cref="GraphPipelineTypeJudgment"/>). The four values of a pipeline type (ClaimKeyTemplate, HalfDoneRule, MaxRunSeconds, MaxSteps) ride the same payload, so an
/// Interactive pipeline can be saved through the product. No default stands in for the type: a create that does not state it is refused.
/// </summary>
public sealed class GraphImplementationTests
{
    private static GraphPayload Complete(string? implementation) => new()
    {
        Name = "Nightly",
        Implementation = implementation,
        IsEnabled = true,
        LinkCapacity = 4,
        MaxParallelRuns = 3,
        OverflowBehavior = "Refuse",
    };

    private static string[] Codes(Fdw.Results.IGenericResult result) => [.. result.Messages.Select(message => message.Code ?? string.Empty)];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACreateThatStatesNoTypeIsRefusedWithACodeAndNothingStandsInForIt()
    {
        var read = GraphPayloadReader.Read(Complete(null), Guid.NewGuid());

        read.IsFailure.ShouldBeTrue();
        Codes(read).ShouldContain("PIPELINES-20846");
        read.Messages.Select(message => message.Message).ShouldContain(text => text.Contains("implementation", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Teleported")]
    [InlineData("Unrun")]
    public void ATypeThatIsNotAGraphPipelineTypeIsRefusedNamingIt(string implementation)
    {
        var read = GraphPayloadReader.Read(Complete(implementation), Guid.NewGuid());

        read.IsFailure.ShouldBeTrue();
        Codes(read).ShouldContain("PIPELINES-20847");
        read.Messages.Select(message => message.Message).ShouldContain(text => text.Contains(implementation, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphStatesItsTypeAndIsReadAsThatType()
    {
        var read = GraphPayloadReader.Read(Complete("Graph"), Guid.NewGuid());

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value.ShouldNotBeNull().Implementation.ShouldBe("Graph");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnInteractivePipelineThatLeavesOutItsRequiredValuesIsRefusedOnceForEachFact()
    {
        var read = GraphPayloadReader.Read(Complete("Interactive"), Guid.NewGuid());

        read.IsFailure.ShouldBeTrue();
        var missing = read.Messages.Where(message => string.Equals(message.Code, "PIPELINES-20840", StringComparison.Ordinal)).Select(message => message.Message).ToList();
        missing.Count.ShouldBe(4);
        missing.ShouldContain(text => text.Contains("HalfDoneRule", StringComparison.Ordinal));
        missing.ShouldContain(text => text.Contains("MaxRunSeconds", StringComparison.Ordinal));
        missing.ShouldContain(text => text.Contains("MaxSteps", StringComparison.Ordinal));
        missing.ShouldContain(text => text.Contains("ClaimKeyTemplate", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnInteractivePipelineThatStatesItsValuesIsReadWithAllFour()
    {
        var payload = Complete("Interactive");
        payload.HalfDoneRule = "End";
        payload.MaxRunSeconds = 60;
        payload.MaxSteps = 50;
        payload.ClaimKeyTemplate = "{conversation}";

        var read = GraphPayloadReader.Read(payload, Guid.NewGuid());

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        var graph = read.Value.ShouldNotBeNull();
        graph.Implementation.ShouldBe("Interactive");
        graph.HalfDoneRule.ShouldBe("End");
        graph.MaxRunSeconds.ShouldBe(60);
        graph.MaxSteps.ShouldBe(50);
        graph.ClaimKeyTemplate.ShouldBe("{conversation}");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphPipelineMayStateTheFourValuesAndNeedsNoneOfThem()
    {
        var stated = Complete("Graph");
        stated.MaxRunSeconds = 30;
        stated.MaxSteps = 10;

        GraphPayloadReader.Read(stated, Guid.NewGuid()).IsSuccess.ShouldBeTrue();
        GraphPayloadReader.Read(Complete("Graph"), Guid.NewGuid()).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheJudgmentIsOneMethodTheCreateAndTheDraftBothUse()
    {
        var graph = new Fdw.Services.Etl.Graphs.GraphPipelineConfiguration { Implementation = "Interactive", Name = "Nightly" };

        var judged = GraphPipelineTypeJudgment.Judge(graph, NullLogger.Instance);

        judged.Select(message => message.Code).Count(code => string.Equals(code, "PIPELINES-20840", StringComparison.Ordinal)).ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheHalfDoneRulesOfThisReleaseAreEndAndNothingElse()
    {
        Fdw.Services.Pipelines.Abstractions.HalfDoneRules.All().Select(rule => rule.Name).ShouldBe(["End"]);
        ReferenceEquals(Fdw.Services.Pipelines.Abstractions.HalfDoneRules.ByName("StartAgain"), Fdw.Services.Pipelines.Abstractions.HalfDoneRules.NotFound).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AStatedRuleThatIsNoOptionIsRefusedOnceWithItsOwnCodeAndNoRuleStandsInForIt()
    {
        var graph = new Fdw.Services.Etl.Graphs.GraphPipelineConfiguration
        {
            Implementation = "Interactive", Name = "Nightly", HalfDoneRule = "StartAgain", MaxRunSeconds = 60, MaxSteps = 50, ClaimKeyTemplate = "{conversation}",
        };

        var judged = GraphPipelineTypeJudgment.Judge(graph, NullLogger.Instance);

        judged.Select(message => message.Code).ShouldBe(["PIPELINES-20854"]);
        judged.Single().Message.Contains("StartAgain", StringComparison.Ordinal).ShouldBeTrue();
        judged.Single().Message.Contains("End", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnInteractivePipelineThatStatesEveryFactAndTheEndRuleIsNotRefused()
    {
        var graph = new Fdw.Services.Etl.Graphs.GraphPipelineConfiguration
        {
            Implementation = "Interactive", Name = "Nightly", HalfDoneRule = "End", MaxRunSeconds = 60, MaxSteps = 50, ClaimKeyTemplate = "{conversation}",
        };

        GraphPipelineTypeJudgment.Judge(graph, NullLogger.Instance).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADraftThatStatesNoTypeSaysInACodedNoteThatTheTypesRequirementsWereNotJudged()
    {
        var problems = new List<GraphProblem>();
        var notes = new List<IGenericMessage>();

        var read = GraphPayloadReader.ReadDraft(Complete(null), Guid.NewGuid(), problems, notes);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        notes.Select(note => note.Code).ShouldContain("PIPELINES-60849");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADraftThatStatesATypeIsJudgedExactlyAsAtCreate()
    {
        var problems = new List<GraphProblem>();
        var notes = new List<IGenericMessage>();

        GraphPayloadReader.ReadDraft(Complete("Interactive"), Guid.NewGuid(), problems, notes);

        notes.ShouldBeEmpty();
        problems.Count(problem => problem.Reason.Contains("HalfDoneRule", StringComparison.Ordinal)).ShouldBe(1);
        problems.Count(problem => problem.Reason.Contains("MaxRunSeconds", StringComparison.Ordinal)).ShouldBe(1);
        problems.Count(problem => problem.Reason.Contains("MaxSteps", StringComparison.Ordinal)).ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADraftThatStatesAnUnknownTypeIsRefusedAsAtCreate()
    {
        var problems = new List<GraphProblem>();

        GraphPayloadReader.ReadDraft(Complete("Teleported"), Guid.NewGuid(), problems, new List<IGenericMessage>());

        problems.ShouldContain(problem => problem.Reason.Contains("Teleported", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUpdateThatStatesNoTypeKeepsTheStoredOne()
    {
        var chosen = GraphUpdateType.Choose(requested: null, stored: "Interactive");

        chosen.IsSuccess.ShouldBeTrue(chosen.CurrentMessage);
        chosen.Value.ShouldBe("Interactive");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUpdateThatStatesADifferentTypeIsRefusedBecauseChangingATypeIsNotPartOfAnUpdate()
    {
        var chosen = GraphUpdateType.Choose(requested: "Graph", stored: "Interactive");

        chosen.IsFailure.ShouldBeTrue();
        Codes(chosen).ShouldContain("PIPELINES-20848");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUpdateThatStatesTheStoredTypeIsAccepted()
    {
        GraphUpdateType.Choose(requested: "Interactive", stored: "Interactive").IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheWriterStatesTheTypeAndTheFourValuesOfTheStoredRow()
    {
        var graph = new Fdw.Services.Etl.Graphs.GraphPipelineConfiguration
        {
            Implementation = "Interactive", Name = "Nightly", IsEnabled = true, LinkCapacity = 4, MaxParallelRuns = 3, OverflowBehavior = "Refuse",
            HalfDoneRule = "End", MaxRunSeconds = 60, MaxSteps = 50, ClaimKeyTemplate = "{conversation}",
        };

        var payload = GraphPayloadWriter.Write(graph);

        payload.Implementation.ShouldBe("Interactive");
        payload.HalfDoneRule.ShouldBe("End");
        payload.MaxRunSeconds.ShouldBe(60);
        payload.MaxSteps.ShouldBe(50);
        payload.ClaimKeyTemplate.ShouldBe("{conversation}");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACreateFromATemplateCreatesTheTypeTheTemplateWasSavedAsAndRefusesAnotherOne()
    {
        var stored = new Fdw.Services.Etl.Graphs.GraphPipelineConfiguration { Implementation = "Interactive", Name = "T", IsTemplate = true };

        GraphTemplateType.Of(stored, requested: null).Value.ShouldBe("Interactive");
        GraphTemplateType.Of(stored, requested: "Interactive").IsSuccess.ShouldBeTrue();
        Codes(GraphTemplateType.Of(stored, requested: "Graph")).ShouldContain("PIPELINES-20848");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AStoredTemplateThatStatesNoTypeIsACodedFailureNotAGraph()
    {
        var stored = new Fdw.Services.Etl.Graphs.GraphPipelineConfiguration { Implementation = string.Empty, Name = "T", IsTemplate = true };

        var type = GraphTemplateType.Of(stored, requested: null);

        type.IsFailure.ShouldBeTrue();
        Codes(type).ShouldContain("PIPELINES-20846");
    }

    private static Mock<IEffectiveSettingsProvider> Ceilings(Guid tenant, int runSeconds = 600, int steps = 200)
    {
        var settings = new Mock<IEffectiveSettingsProvider>(MockBehavior.Strict);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.InteractiveMaxRunSecondsCeiling, tenant, null)).Returns(runSeconds);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.InteractiveMaxStepsCeiling, tenant, null)).Returns(steps);
        return settings;
    }

    private static GraphPipelineConfiguration Stated(string implementation, int? seconds, int? steps) => new()
    {
        Implementation = implementation, Name = "Nightly", HalfDoneRule = "End", MaxRunSeconds = seconds, MaxSteps = steps,
    };

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(601, 50, "MaxRunSeconds")]
    [InlineData(60, 201, "MaxSteps")]
    public void AnInteractiveValueAboveItsCeilingIsRefusedAtTheSaveNamingTheValue(int seconds, int steps, string named)
    {
        var tenant = Guid.NewGuid();

        var judged = GraphRunLimitJudgment.Judge(Stated("Interactive", seconds, steps), Ceilings(tenant).Object, tenant, NullLogger.Instance);

        judged.IsFailure.ShouldBeTrue();
        Codes(judged).ShouldContain("PIPELINERUN-20841");
        judged.CurrentMessage.ShouldNotBeNull().ShouldContain(named);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnInteractiveValueAtItsCeilingIsAcceptedAndAGraphIsNotJudgedByTheAtOnceCeilings()
    {
        var tenant = Guid.NewGuid();

        GraphRunLimitJudgment.Judge(Stated("Interactive", 600, 200), Ceilings(tenant).Object, tenant, NullLogger.Instance).IsSuccess.ShouldBeTrue();
        GraphRunLimitJudgment.Judge(Stated("Graph", 100000, 100000), new Mock<IEffectiveSettingsProvider>(MockBehavior.Strict).Object, tenant, NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }
}
