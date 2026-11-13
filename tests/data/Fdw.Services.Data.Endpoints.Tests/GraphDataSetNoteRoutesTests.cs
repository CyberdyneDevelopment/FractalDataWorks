using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-5452: notes and intended sources worked on one at a time. Each has a stable id, an author and a time; adding one leaves the others (and other writers') as they are;
/// answering records who and when; a part that does not exist is refused by its own code and never made.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataSets.FDW5452")]
public sealed class GraphDataSetNoteRoutesTests
{
    private static readonly Guid Ann = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid Bob = Guid.Parse("00000000-0000-0000-0000-0000000000b2");
    private static readonly DateTimeOffset Earlier = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid BobsNote = Guid.Parse("00000000-0000-0000-0000-00000000c001");

    private static GraphDataSetStamp By(Guid? caller) => GraphDataSetStamp.Saving(caller);

    private static GraphDataSetConfiguration Stored()
    {
        var graph = new GraphDataSetConfiguration { Name = "GasPrices", Implementation = "Graph" };
        graph.GraphFields.Add(new GraphDataSetFieldConfiguration
        {
            Name = "Amount",
            Notes = [new GraphDataSetFieldNoteConfiguration { Id = BobsNote, NoteKind = "Question", Body = "Which tariff?", Ordinal = 0, AuthorUserId = Bob, AuthoredAt = Earlier }],
        });
        return graph;
    }

    private static Mock<IDataSetConfigurationProvider> Providing(GraphDataSetConfiguration graph)
    {
        var provider = new Mock<IDataSetConfigurationProvider>();
        provider.Setup(p => p.Get("GasPrices", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(graph));
        provider.Setup(p => p.Update(It.IsAny<GraphDataSetConfiguration>(), "DataSet", "Graph", "GasPrices", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        return provider;
    }

    private static void SavedNever(Mock<IDataSetConfigurationProvider> provider)
        => provider.Verify(p => p.Update(It.IsAny<GraphDataSetConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

    private static void SavedOnce(Mock<IDataSetConfigurationProvider> provider)
        => provider.Verify(p => p.Update(It.IsAny<GraphDataSetConfiguration>(), "DataSet", "Graph", "GasPrices", It.IsAny<CancellationToken>()), Times.Once);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AddingANoteGivesItAnIdAnAuthorAndATimeAndLeavesAnotherWritersNoteAsItWas()
    {
        var graph = Stored();
        var provider = Providing(graph);
        var stamp = By(Ann);

        var added = await GraphDataSetEdits.AddNote(provider.Object, new AddGraphFieldNoteRequest { Name = "GasPrices", Field = "Amount", Kind = "Reply", Body = "Spot" }, stamp, NullLogger.Instance, Ct);

        added.IsSuccess.ShouldBeTrue();
        var note = added.Value.ShouldNotBeNull();
        note.Id.ShouldNotBeNull().ShouldNotBe(Guid.Empty);
        note.AuthorUserId.ShouldBe(Ann);
        note.CreatedAt.ShouldBe(stamp.Now);
        note.IsAnswered.ShouldBeFalse();
        var notes = graph.GraphFields.Single().Notes;
        notes.Count.ShouldBe(2);
        notes.Single(n => n.Id == BobsNote).AuthorUserId.ShouldBe(Bob);
        notes.Single(n => n.Id == BobsNote).AuthoredAt.ShouldBe(Earlier);
        notes.Select(n => n.Ordinal).ShouldBe([0, 1]);
        SavedOnce(provider);
    }

    [Fact]
    public async Task ANoteAddedToAFieldTheDataSetLacksIsRefusedByNameAndNothingIsSaved()
    {
        var provider = Providing(Stored());

        var added = await GraphDataSetEdits.AddNote(provider.Object, new AddGraphFieldNoteRequest { Name = "GasPrices", Field = "Nope", Kind = "Question", Body = "?" }, By(Ann), NullLogger.Instance, Ct);

        added.IsFailure.ShouldBeTrue();
        added.Code.ShouldBeOfType<GraphDataSetFieldNotFoundCode>();
        SavedNever(provider);
    }

    [Fact]
    public async Task ACallerThatCannotBeNamedAddsNoNoteAndNothingIsSaved()
    {
        var provider = Providing(Stored());

        var added = await GraphDataSetEdits.AddNote(provider.Object, new AddGraphFieldNoteRequest { Name = "GasPrices", Field = "Amount", Kind = "Question", Body = "?" }, By(null), NullLogger.Instance, Ct);

        added.IsFailure.ShouldBeTrue();
        added.Code.ShouldBeOfType<GraphDataSetAuthorUnresolvedCode>();
        SavedNever(provider);
    }

    [Fact]
    public async Task ADataSetThatIsNotAGraphIsRefusedAsNotAGraph()
    {
        var provider = new Mock<IDataSetConfigurationProvider>();
        provider.Setup(p => p.Get("GasPrices", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(new SimpleDataSetConfiguration { Name = "GasPrices", Implementation = "Simple" }));

        var added = await GraphDataSetEdits.AddNote(provider.Object, new AddGraphFieldNoteRequest { Name = "GasPrices", Field = "Amount", Kind = "Question", Body = "?" }, By(Ann), NullLogger.Instance, Ct);

        added.Code.ShouldBeOfType<GraphDataSetNotGraphCode>();
    }

    [Fact]
    public async Task AnsweringANoteRecordsWhoAnsweredAndWhen()
    {
        var graph = Stored();
        var provider = Providing(graph);
        var stamp = By(Ann);

        var answered = await GraphDataSetEdits.AnswerNote(provider.Object, new GraphFieldNoteRouteRequest { Name = "GasPrices", Field = "Amount", NoteId = BobsNote }, stamp, NullLogger.Instance, Ct);

        answered.IsSuccess.ShouldBeTrue();
        var note = answered.Value.ShouldNotBeNull();
        note.IsAnswered.ShouldBeTrue();
        note.AnsweredByUserId.ShouldBe(Ann);
        note.AnsweredAt.ShouldBe(stamp.Now);
        note.AuthorUserId.ShouldBe(Bob);
        note.CreatedAt.ShouldBe(Earlier);
        SavedOnce(provider);
    }

    [Fact]
    public async Task ANoteAlreadyAnsweredKeepsWhoAnsweredAndWhenAndNothingIsSaved()
    {
        var graph = Stored();
        var first = graph.GraphFields.Single().Notes.Single();
        first.IsAnswered = true;
        first.AnsweredByUserId = Bob;
        first.AnsweredAt = Earlier;
        var provider = Providing(graph);

        var answered = await GraphDataSetEdits.AnswerNote(provider.Object, new GraphFieldNoteRouteRequest { Name = "GasPrices", Field = "Amount", NoteId = BobsNote }, By(Ann), NullLogger.Instance, Ct);

        var note = answered.Value.ShouldNotBeNull();
        note.AnsweredByUserId.ShouldBe(Bob);
        note.AnsweredAt.ShouldBe(Earlier);
        SavedNever(provider);
    }

    [Fact]
    public async Task AReplyCannotBeAnsweredAndNothingIsSaved()
    {
        var graph = Stored();
        var reply = new GraphDataSetFieldNoteConfiguration { Id = Guid.CreateVersion7(), NoteKind = "Reply", Body = "Spot", Ordinal = 1 };
        graph.GraphFields.Single().Notes.Add(reply);
        var provider = Providing(graph);

        var answered = await GraphDataSetEdits.AnswerNote(provider.Object, new GraphFieldNoteRouteRequest { Name = "GasPrices", Field = "Amount", NoteId = reply.Id }, By(Ann), NullLogger.Instance, Ct);

        answered.IsFailure.ShouldBeTrue();
        answered.Code.ShouldBeOfType<GraphDataSetNoteCannotBeAnsweredCode>();
        SavedNever(provider);
    }

    [Fact]
    public async Task AnIdThatMatchesNoNoteIsRefusedByNameNotMadeIntoANewNote()
    {
        var provider = Providing(Stored());

        var answered = await GraphDataSetEdits.AnswerNote(provider.Object, new GraphFieldNoteRouteRequest { Name = "GasPrices", Field = "Amount", NoteId = Guid.CreateVersion7() }, By(Ann), NullLogger.Instance, Ct);
        var removed = await GraphDataSetEdits.RemoveNote(provider.Object, new GraphFieldNoteRouteRequest { Name = "GasPrices", Field = "Amount", NoteId = Guid.CreateVersion7() }, NullLogger.Instance, Ct);

        answered.Code.ShouldBeOfType<GraphDataSetNoteNotFoundCode>();
        removed.Code.ShouldBeOfType<GraphDataSetNoteNotFoundCode>();
        SavedNever(provider);
    }

    [Fact]
    public async Task RemovingANoteRemovesOnlyThatNote()
    {
        var graph = Stored();
        var other = new GraphDataSetFieldNoteConfiguration { Id = Guid.CreateVersion7(), NoteKind = "Reply", Body = "Spot", Ordinal = 1 };
        graph.GraphFields.Single().Notes.Add(other);
        var provider = Providing(graph);

        var removed = await GraphDataSetEdits.RemoveNote(provider.Object, new GraphFieldNoteRouteRequest { Name = "GasPrices", Field = "Amount", NoteId = BobsNote }, NullLogger.Instance, Ct);

        removed.IsSuccess.ShouldBeTrue();
        graph.GraphFields.Single().Notes.Select(n => n.Id).ShouldBe([other.Id]);
        SavedOnce(provider);
    }

    [Fact]
    public async Task AddingAnIntendedSourceGivesItAnIdAnAuthorAndATimeAndLeavesTheOthersAsTheyWere()
    {
        var graph = Stored();
        graph.Intents.Add(new GraphDataSetIntentConfiguration { Id = Guid.CreateVersion7(), IntentKind = "DataStore", DataStoreName = "Gas", Ordinal = 0, AuthorUserId = Bob, AuthoredAt = Earlier });
        var provider = Providing(graph);
        var stamp = By(Ann);

        var added = await GraphDataSetEdits.AddIntendedSource(provider.Object, new AddGraphIntendedSourceRequest { Name = "GasPrices", Kind = "DataStore", DataStore = "Power" }, stamp, NullLogger.Instance, Ct);

        added.IsSuccess.ShouldBeTrue();
        var source = added.Value.ShouldNotBeNull();
        source.Id.ShouldNotBeNull().ShouldNotBe(Guid.Empty);
        source.AuthorUserId.ShouldBe(Ann);
        source.CreatedAt.ShouldBe(stamp.Now);
        graph.Intents.Select(i => (i.DataStoreName, i.Ordinal)).ShouldBe([("Gas", 0), ("Power", 1)]);
        graph.Intents[0].AuthorUserId.ShouldBe(Bob);
        SavedOnce(provider);
    }

    [Fact]
    public async Task AnIntendedSourceTheDataSetAlreadyStatesIsRefusedAsARepeat()
    {
        var graph = Stored();
        graph.Intents.Add(new GraphDataSetIntentConfiguration { Id = Guid.CreateVersion7(), IntentKind = "DataStore", DataStoreName = "Gas", Ordinal = 0 });
        var provider = Providing(graph);

        var added = await GraphDataSetEdits.AddIntendedSource(provider.Object, new AddGraphIntendedSourceRequest { Name = "GasPrices", Kind = "DataStore", DataStore = "gas" }, By(Ann), NullLogger.Instance, Ct);

        added.Code.ShouldBeOfType<GraphDataSetIntendedSourceDuplicateCode>();
        SavedNever(provider);
    }

    [Fact]
    public async Task AGraphThatHasComponentsTakesNoIntendedSource()
    {
        var graph = Stored();
        graph.Components.Add(new GraphDataSetComponentConfiguration { Name = "Teams", ComponentType = "DataSetSource" });
        var provider = Providing(graph);

        var added = await GraphDataSetEdits.AddIntendedSource(provider.Object, new AddGraphIntendedSourceRequest { Name = "GasPrices", Kind = "DataStore", DataStore = "Gas" }, By(Ann), NullLogger.Instance, Ct);

        added.Code.ShouldBeOfType<GraphDataSetIntentOnBoundGraphCode>();
        SavedNever(provider);
    }

    [Fact]
    public async Task RemovingAnIntendedSourceRemovesOnlyThatOneAndAnUnknownIdIsRefusedByName()
    {
        var graph = Stored();
        var gas = new GraphDataSetIntentConfiguration { Id = Guid.CreateVersion7(), IntentKind = "DataStore", DataStoreName = "Gas", Ordinal = 0 };
        var power = new GraphDataSetIntentConfiguration { Id = Guid.CreateVersion7(), IntentKind = "DataStore", DataStoreName = "Power", Ordinal = 1 };
        graph.Intents.Add(gas);
        graph.Intents.Add(power);
        var provider = Providing(graph);

        var unknown = await GraphDataSetEdits.RemoveIntendedSource(provider.Object, new GraphIntendedSourceRouteRequest { Name = "GasPrices", SourceId = Guid.CreateVersion7() }, NullLogger.Instance, Ct);
        var removed = await GraphDataSetEdits.RemoveIntendedSource(provider.Object, new GraphIntendedSourceRouteRequest { Name = "GasPrices", SourceId = gas.Id }, NullLogger.Instance, Ct);

        unknown.Code.ShouldBeOfType<GraphDataSetIntendedSourceNotFoundCode>();
        removed.IsSuccess.ShouldBeTrue();
        graph.Intents.Select(i => i.Id).ShouldBe([power.Id]);
        SavedOnce(provider);
    }

    [Fact]
    public void ThePayloadAnswersEachNoteWithItsIdAuthorTimeAndAnswer()
    {
        var note = new GraphDataSetFieldNoteConfiguration
        {
            Id = BobsNote, NoteKind = "Question", Body = "Which tariff?", IsAnswered = true, AuthorUserId = Bob, AuthoredAt = Earlier, AnsweredByUserId = Ann, AnsweredAt = Earlier.AddHours(1),
        };

        var payload = GraphDataSetGraphPayloads.Note(note);

        payload.Id.ShouldBe(BobsNote);
        payload.AuthorUserId.ShouldBe(Bob);
        payload.CreatedAt.ShouldBe(Earlier);
        payload.AnsweredByUserId.ShouldBe(Ann);
        payload.AnsweredAt.ShouldBe(Earlier.AddHours(1));
        payload.IsAnswered.ShouldBeTrue();
    }
}
