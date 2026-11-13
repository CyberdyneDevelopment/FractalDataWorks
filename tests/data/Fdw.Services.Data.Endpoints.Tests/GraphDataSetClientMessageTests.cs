using Fdw.Results;
using Fdw.Services.Data.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-5442: a refused Graph data set save, a preview or a read tells its caller why, because that text is the caller's own input named back
/// to them. The dev walk found the 400 of an unsound draft carried only DATA-21414 and the generic text.
/// </summary>
public sealed class GraphDataSetClientMessageTests
{
    [Fact]
    public void AnUnsoundDraftShowsTheCallerEveryProblemWithItsCodeAndPath()
        => DataServiceResultCodes.ByName("GraphDataSetDraftNotSound")
            .FormatClientMessage(ResultDetails.Create("dataSet", "Walk", "problems", "DATA-21400 Field out 'Name' is not fed (fieldsOut/Name); DATA-21409 cycle (components/u1)"))
            .ShouldBe("Graph data set 'Walk' is not sound, so it is not saved: DATA-21400 Field out 'Name' is not fed (fieldsOut/Name); DATA-21409 cycle (components/u1)");

    [Fact]
    public void ARowLimitShowsTheCallerWhichDataSetAndWhichLimit()
        => DataServiceResultCodes.ByName("GraphDataSetRowLimitExceeded")
            .FormatClientMessage(ResultDetails.Create("dataSet", "Walk", "limit", "100000"))
            .ShouldBe("The graph of data set 'Walk' produced more than the 100000 rows a read of it may return, so no rows are returned");

    [Fact]
    public void ATimeLimitShowsTheCallerWhichDataSetAndWhichLimit()
        => DataServiceResultCodes.ByName("GraphDataSetTimeLimitExceeded")
            .FormatClientMessage(ResultDetails.Create("dataSet", "Walk", "limit", "60 seconds"))
            .ShouldBe("The graph of data set 'Walk' did not finish within the 60 seconds a read of it may take, so no rows are returned");

    [Fact]
    public void AnInvalidPreviewCapShowsTheCallerTheCapTheyStated()
        => DataServiceResultCodes.ByName("GraphDataSetPreviewCapInvalid")
            .FormatClientMessage(ResultDetails.Create("dataSet", "Walk", "cap", "0"))
            .ShouldBe("The preview of Graph data set draft 'Walk' states 0 rows from each source, which is not a whole number of at least one, so nothing is read");

    [Fact]
    public void APreviewEndThatIsNotFoundShowsTheCallerTheComponentAndOutputTheyNamed()
        => DataServiceResultCodes.ByName("GraphDataSetPreviewEndNotFound")
            .FormatClientMessage(ResultDetails.Create("dataSet", "Walk", "component", "nope", "output", "output").With("reason", "the graph has no component 'nope'"))
            .ShouldBe("The preview of Graph data set draft 'Walk' cannot be read at component 'nope', output 'output': the graph has no component 'nope'");

    [Fact]
    public void AFieldRouteTheKindDoesNotTakeShowsTheCallerWhichDataSetAndField()
    {
        DataServiceResultCodes.ByName("DataSetFieldHistoryNotKept")
            .FormatClientMessage(ResultDetails.Create("dataSet", "Walk", "field", "Name"))
            .ShouldBe("Data set 'Walk' keeps no version history for its fields, so there is no history of field 'Name' to read");
        DataServiceResultCodes.ByName("DataSetFieldStandInsNotTaken")
            .FormatClientMessage(ResultDetails.Create("dataSet", "Walk", "field", "Name"))
            .ShouldBe("Data set 'Walk' does not read stand-ins or samples for its fields, so none can be set, read or cleared on field 'Name'");
    }

    [Fact]
    public void AnUpdateThatSendsHalfTheGraphShowsTheCallerWhichHalf()
        => DataServiceResultCodes.ByName("GraphDataSetBodyIncomplete")
            .FormatClientMessage(ResultDetails.Create("dataSet", "Walk", "sent", "components"))
            .ShouldBe("The update of Graph data set 'Walk' sends only the components of its graph: send the components and the links together");

    [Fact]
    public void ARouteAboutAWholeDataSetThatTakesNoStandInsNamesNoField()
        => DataServiceResultCodes.ByName("DataSetStandInsNotTaken")
            .FormatClientMessage(ResultDetails.Create("dataSet", "Walk"))
            .ShouldBe("Data set 'Walk' does not read stand-ins or samples for its fields, so none can be set, read, listed or cleared on it");
}
