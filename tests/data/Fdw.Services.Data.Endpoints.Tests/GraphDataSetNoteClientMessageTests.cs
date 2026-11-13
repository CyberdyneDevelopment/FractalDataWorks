using Fdw.Results;
using Fdw.Services.Data.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-5452: each refusal of a note or an intended source tells its caller why, because that text is the caller's own input named back to them (an error
/// mapper relays a reason only for a code that overrides <c>FormatClientMessage</c>).
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "DataSets.FDW5452")]
public sealed class GraphDataSetNoteClientMessageTests
{
    [Fact]
    public void ACallerThatCannotBeNamedIsToldWhichDataSetCouldNotTakeTheirNote()
        => DataServiceResultCodes.ByName("GraphDataSetAuthorUnresolved")
            .FormatClientMessage(ResultDetails.Create("dataSet", "Walk"))
            .ShouldBe("Graph data set 'Walk' cannot take a note or an intended source: the caller cannot be named, so there is no author to record");

    [Fact]
    public void ARepeatedIntendedSourceIsToldWhichDataSetAlreadyIntendsIt()
        => DataServiceResultCodes.ByName("GraphDataSetIntendedSourceDuplicate")
            .FormatClientMessage(ResultDetails.Create("dataSet", "Walk", "kind", "DataStore", "name", "feed"))
            .ShouldBe("Graph data set 'Walk' already intends to read DataStore 'feed'");

    [Fact]
    public void ANoteThatCannotBeAnsweredIsToldWhichNoteAndWhatKindItIs()
        => DataServiceResultCodes.ByName("GraphDataSetNoteCannotBeAnswered")
            .FormatClientMessage(ResultDetails.Create("note", "n1", "field", "Day", "dataSet", "Walk").With("kind", "Reply"))
            .ShouldBe("Note 'n1' on field 'Day' of Graph data set 'Walk' is a Reply and cannot be answered");

    [Fact]
    public void AMissingFieldNoteOrIntendedSourceIsToldWhichOneTheDataSetLacks()
    {
        DataServiceResultCodes.ByName("GraphDataSetFieldNotFound")
            .FormatClientMessage(ResultDetails.Create("field", "Day", "dataSet", "Walk"))
            .ShouldBe("Field 'Day' is not a field out of Graph data set 'Walk'");
        DataServiceResultCodes.ByName("GraphDataSetNoteNotFound")
            .FormatClientMessage(ResultDetails.Create("note", "n1", "field", "Day", "dataSet", "Walk"))
            .ShouldBe("Note 'n1' is not a note on field 'Day' of Graph data set 'Walk'");
        DataServiceResultCodes.ByName("GraphDataSetIntendedSourceNotFound")
            .FormatClientMessage(ResultDetails.Create("source", "s1", "dataSet", "Walk"))
            .ShouldBe("Intended source 's1' is not an intended source of Graph data set 'Walk'");
    }
}
