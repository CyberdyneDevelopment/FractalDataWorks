using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-5452: an update that replaces a list keeps the identity of what it names by id: a note or intended source that carries a stored id keeps its id, author and time; one that
/// carries none is new; an id that matches none is refused. A body with no ids behaves as it did.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataSets.FDW5452")]
public sealed class GraphDataSetNoteIdentityTests
{
    private static readonly Guid Ann = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid Bob = Guid.Parse("00000000-0000-0000-0000-0000000000b2");
    private static readonly DateTimeOffset Earlier = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid StoredNote = Guid.Parse("00000000-0000-0000-0000-00000000c001");

    private static GraphDataSetConfiguration Stored()
    {
        var graph = new GraphDataSetConfiguration { Name = "GasPrices" };
        graph.GraphFields.Add(new GraphDataSetFieldConfiguration
        {
            Name = "Amount",
            Notes = [new GraphDataSetFieldNoteConfiguration { Id = StoredNote, NoteKind = "Question", Body = "Which tariff?", Ordinal = 0, AuthorUserId = Bob, AuthoredAt = Earlier }],
        });
        return graph;
    }

    private static GraphFieldOutRequest Field(params GraphFieldNoteRequest[] notes) => new() { Name = "Amount", IsNullable = false, IsFilterable = true, Notes = [.. notes] };

    private static Fdw.Results.IGenericResult Replace(GraphDataSetConfiguration graph, GraphFieldOutRequest field, GraphDataSetStamp stamp)
        => GraphDataSetDefinitions.Fields(graph, [field], new DataSetRequestValues("GasPrices", NullLogger.Instance), stamp, NullLogger.Instance);

    [Fact]
    public void ANoteThatCarriesAStoredIdKeepsItsIdAuthorAndTimeAndTakesTheNewContent()
    {
        var graph = Stored();

        var result = Replace(graph, Field(new GraphFieldNoteRequest { Id = StoredNote, Kind = "Question", Body = "Which tariff, spot or fixed?" }), GraphDataSetStamp.Saving(Ann));

        result.IsSuccess.ShouldBeTrue();
        var note = graph.GraphFields.Single().Notes.Single();
        note.Id.ShouldBe(StoredNote);
        note.AuthorUserId.ShouldBe(Bob);
        note.AuthoredAt.ShouldBe(Earlier);
        note.Body.ShouldBe("Which tariff, spot or fixed?");
    }

    [Fact]
    public void ANoteThatCarriesNoIdIsNewWithAnIdOfTheServersAndTheCallerAsAuthor()
    {
        var graph = Stored();
        var stamp = GraphDataSetStamp.Saving(Ann);

        var result = Replace(graph, Field(new GraphFieldNoteRequest { Id = StoredNote, Kind = "Question", Body = "Which tariff?" }, new GraphFieldNoteRequest { Kind = "Reply", Body = "Spot" }), stamp);

        result.IsSuccess.ShouldBeTrue();
        var notes = graph.GraphFields.Single().Notes;
        notes.Count.ShouldBe(2);
        var added = notes[1];
        added.Id.ShouldNotBe(Guid.Empty);
        added.Id.ShouldNotBe(StoredNote);
        added.AuthorUserId.ShouldBe(Ann);
        added.AuthoredAt.ShouldBe(stamp.Now);
    }

    [Fact]
    public void AnIdThatMatchesNoStoredNoteIsRefusedByName()
    {
        var graph = Stored();

        var result = Replace(graph, Field(new GraphFieldNoteRequest { Id = Guid.CreateVersion7(), Kind = "Question", Body = "?" }), GraphDataSetStamp.Saving(Ann));

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetNoteNotFoundCode>();
    }

    [Fact]
    public void ACallerThatCannotBeNamedMayStillSendBackNotesThatAlreadyHaveAuthorsButNotAddOne()
    {
        var kept = Replace(Stored(), Field(new GraphFieldNoteRequest { Id = StoredNote, Kind = "Question", Body = "Which tariff?" }), GraphDataSetStamp.Saving((Guid?)null));
        var added = Replace(Stored(), Field(new GraphFieldNoteRequest { Kind = "Question", Body = "New?" }), GraphDataSetStamp.Saving((Guid?)null));

        kept.IsSuccess.ShouldBeTrue();
        added.IsFailure.ShouldBeTrue();
        added.Code.ShouldBeOfType<GraphDataSetAuthorUnresolvedCode>();
    }

    [Fact]
    public void MarkingAStoredNoteAnsweredRecordsTheCallerAndNowAndANoteAnsweredBeforeKeepsItsAnswer()
    {
        var graph = Stored();
        var stamp = GraphDataSetStamp.Saving(Ann);

        Replace(graph, Field(new GraphFieldNoteRequest { Id = StoredNote, Kind = "Question", Body = "Which tariff?", IsAnswered = true }), stamp).IsSuccess.ShouldBeTrue();
        var answered = graph.GraphFields.Single().Notes.Single();
        answered.AnsweredByUserId.ShouldBe(Ann);
        answered.AnsweredAt.ShouldBe(stamp.Now);

        Replace(graph, Field(new GraphFieldNoteRequest { Id = StoredNote, Kind = "Question", Body = "Which tariff?", IsAnswered = true }), GraphDataSetStamp.Saving(Bob)).IsSuccess.ShouldBeTrue();
        var again = graph.GraphFields.Single().Notes.Single();
        again.AnsweredByUserId.ShouldBe(Ann);
        again.AnsweredAt.ShouldBe(stamp.Now);
    }

    [Fact]
    public void ANoteSentAsNotAnsweredHasNeitherWhoNorWhen()
    {
        var graph = Stored();
        var first = graph.GraphFields.Single().Notes.Single();
        first.IsAnswered = true;
        first.AnsweredByUserId = Ann;
        first.AnsweredAt = Earlier;

        Replace(graph, Field(new GraphFieldNoteRequest { Id = StoredNote, Kind = "Question", Body = "Which tariff?", IsAnswered = false }), GraphDataSetStamp.Saving(Bob)).IsSuccess.ShouldBeTrue();

        var note = graph.GraphFields.Single().Notes.Single();
        note.IsAnswered.ShouldBeFalse();
        note.AnsweredByUserId.ShouldBeNull();
        note.AnsweredAt.ShouldBeNull();
    }

    [Fact]
    public void ACheckStampsNothingAndResolvesNoIdAgainstWhatIsStored()
    {
        var graph = new GraphDataSetConfiguration { Name = "GasPrices" };

        var result = Replace(graph, Field(new GraphFieldNoteRequest { Id = Guid.CreateVersion7(), Kind = "Question", Body = "?" }), GraphDataSetStamp.Checking);

        result.IsSuccess.ShouldBeTrue();
        var note = graph.GraphFields.Single().Notes.Single();
        note.AuthorUserId.ShouldBeNull();
        note.AuthoredAt.ShouldBeNull();
    }

    [Fact]
    public async Task AnIntendedSourceThatCarriesAStoredIdKeepsItsIdAuthorAndTimeAndOneWithoutIsNew()
    {
        var graph = Stored();
        var stored = new GraphDataSetIntentConfiguration { Id = Guid.CreateVersion7(), IntentKind = "DataStore", DataStoreName = "Gas", Ordinal = 0, AuthorUserId = Bob, AuthoredAt = Earlier };
        graph.Intents.Add(stored);
        var stamp = GraphDataSetStamp.Saving(Ann);

        var result = await GraphDataSetDefinitions.Intents(
            graph,
            [new GraphIntendedSourceRequest { Id = stored.Id, Kind = "DataStore", DataStore = "Gas", Note = "prices" }, new GraphIntendedSourceRequest { Kind = "DataStore", DataStore = "Power" }],
            new Mock<IDataSetConfigurationProvider>().Object, stamp, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        graph.Intents[0].Id.ShouldBe(stored.Id);
        graph.Intents[0].AuthorUserId.ShouldBe(Bob);
        graph.Intents[0].AuthoredAt.ShouldBe(Earlier);
        graph.Intents[0].Note.ShouldBe("prices");
        graph.Intents[1].Id.ShouldNotBe(Guid.Empty);
        graph.Intents[1].AuthorUserId.ShouldBe(Ann);
        graph.Intents[1].AuthoredAt.ShouldBe(stamp.Now);
    }

    [Fact]
    public async Task AnIntendedSourceIdThatMatchesNoneIsRefusedByName()
    {
        var graph = Stored();

        var result = await GraphDataSetDefinitions.Intents(
            graph, [new GraphIntendedSourceRequest { Id = Guid.CreateVersion7(), Kind = "DataStore", DataStore = "Gas" }],
            new Mock<IDataSetConfigurationProvider>().Object, GraphDataSetStamp.Saving(Ann), NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetIntendedSourceNotFoundCode>();
    }
}
