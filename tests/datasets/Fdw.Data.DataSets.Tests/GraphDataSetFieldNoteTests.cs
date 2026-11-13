using System;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

/// <summary>FDW-5294: the discussion on a Graph data set's fields out is stored with the field, and a field's state is worked out from it, never stored.</summary>
public class GraphDataSetFieldNoteTests
{
    private static readonly GraphDataSetFieldNoteConfigurationValidator Validator = new();

    private static GraphDataSetFieldNoteConfiguration Question(bool answered = false) => new()
    {
        Id = Guid.NewGuid(),
        NoteKind = "Question",
        Body = "Is this the day's average, or its high and low as two fields?",
        IsAnswered = answered
    };

    private static GraphDataSetFieldNoteConfiguration Reply() => new()
    {
        Id = Guid.NewGuid(),
        NoteKind = "Reply",
        Body = "The feed has hourly readings, so either can be calculated.",
        Ordinal = 1
    };

    private static (GraphDataSetConfiguration Graph, GraphDataSetFieldConfiguration Field) DataSetWithAField(bool intended)
    {
        var graph = new GraphDataSetConfiguration();
        var field = new GraphDataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Temperature" };
        graph.GraphFields.Add(field);
        if (intended)
            graph.Intents.Add(new GraphDataSetIntentConfiguration { Id = Guid.NewGuid(), IntentKind = "DataStore", DataStoreName = "WeatherFeed" });
        return (graph, field);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void TheNoteKindsAreAQuestionAndAReplyAndOnlyAQuestionCanBeAnswered()
    {
        GraphFieldNoteKinds.ByName("Question").CanBeAnswered.ShouldBeTrue();
        GraphFieldNoteKinds.ByName("Reply").CanBeAnswered.ShouldBeFalse();
        ReferenceEquals(GraphFieldNoteKinds.ByName("Decision"), GraphFieldNoteKinds.NotFound).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void TheGeneratedMapperOfAFieldCascadesItsNotesAndTheNoteCascadesNothing()
    {
        new GraphDataSetFieldConfigurationPocoMapper().CascadeChildren.Select(child => child.ChildType)
            .ShouldBe([typeof(GraphDataSetFieldNoteConfiguration)]);
        new GraphDataSetFieldNoteConfigurationPocoMapper().CascadeChildren.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void AQuestionAndAReplyAreBothValidNotes()
    {
        Validator.Validate(Question()).IsValid.ShouldBeTrue();
        Validator.Validate(Question(answered: true)).IsValid.ShouldBeTrue();
        Validator.Validate(Reply()).IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void AReplyMarkedAnsweredIsRefused()
    {
        var reply = Reply();
        reply.IsAnswered = true;

        Validator.Validate(reply).Errors.Select(e => e.ErrorMessage)
            .ShouldContain("Only a note of a kind that can be answered is marked answered");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void ANoteWithNoKindAnUnknownKindOrNoBodyIsRefused()
    {
        var none = Question();
        none.NoteKind = string.Empty;
        var unknown = Question();
        unknown.NoteKind = "Decision";
        var empty = Question();
        empty.Body = string.Empty;

        Validator.Validate(none).Errors.Select(e => e.ErrorMessage).ShouldContain("A note must say what it is");
        Validator.Validate(unknown).Errors.Select(e => e.ErrorMessage).ShouldContain("A note must name a GraphFieldNoteKinds option");
        Validator.Validate(empty).Errors.Select(e => e.ErrorMessage).ShouldContain("A note must say something");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void AFieldWithNothingSaidAboutItIsDefined()
    {
        var (graph, field) = DataSetWithAField(intended: false);

        graph.StateOf(field).Name.ShouldBe("Defined");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void AnUnfedFieldOfADataSetThatStatesWhatItIntendsToReadIsIntended()
    {
        var (graph, field) = DataSetWithAField(intended: true);

        graph.StateOf(field).Name.ShouldBe("Intended");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void AFedFieldIsNotIntendedEvenWhenItsDataSetStatesIntents()
    {
        var (graph, field) = DataSetWithAField(intended: true);
        field.FedByComponent = "Source1";
        field.FedByField = "Temperature";

        graph.StateOf(field).Name.ShouldBe("Defined");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void AQuestionNobodyHasAnsweredMakesTheFieldQuestionOpenWhateverElseIsTrue()
    {
        var (graph, field) = DataSetWithAField(intended: true);
        field.Notes.Add(Question());

        graph.StateOf(field).Name.ShouldBe("QuestionOpen");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void ARepliesAloneDoNotKeepAQuestionOpenAndAnAnsweredQuestionClosesIt()
    {
        var (graph, field) = DataSetWithAField(intended: false);
        field.Notes.Add(Reply());

        graph.StateOf(field).Name.ShouldBe("Defined");

        field.Notes.Add(Question(answered: true));

        graph.StateOf(field).Name.ShouldBe("Defined");

        field.Notes.Add(Question());

        graph.StateOf(field).Name.ShouldBe("QuestionOpen");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void TheStatesAreTriedInPrecedenceOrderAndDefinedAppliesToEveryField()
    {
        GraphFieldStates.All().OrderBy(state => state.Precedence).Select(state => state.Name)
            .ShouldBe(["QuestionOpen", "Intended", "Defined"]);
        GraphFieldStates.ByName("Defined").Applies(new GraphDataSetFieldConfiguration(), dataSetHasIntendedSource: false).ShouldBeTrue();
    }
}
