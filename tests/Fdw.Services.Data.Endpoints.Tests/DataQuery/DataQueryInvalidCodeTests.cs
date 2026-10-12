using Fdw.Results;
using Fdw.Services.Data.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests.DataQuery;

/// <summary>
/// FDW-1046: a refused data query tells its caller which data set and why, because that text is the caller's own input named
/// back to them. A validation code that has not opted in shows nothing and keeps the response's generic text.
/// </summary>
public sealed class DataQueryInvalidCodeTests
{
    [Fact]
    public void ARefusedQueryShowsTheCallerWhichDataSetAndWhy()
        => DataServiceResultCodes.ByName("DataQueryInvalid")
            .FormatClientMessage(ResultDetails.Create("dataSet", "NflTeams", "reason", "filter field 'x' is not a field of this data set"))
            .ShouldBe("Query of data set 'NflTeams' rejected: filter field 'x' is not a field of this data set");

    [Fact]
    public void AValidationCodeThatDidNotOptInShowsNothing()
        => DataServiceResultCodes.ByName("ContainerNameRequired")
            .FormatClientMessage(ResultDetails.Create("dataSet", "NflTeams"))
            .ShouldBeNull();

    [Fact]
    public void ARefusalToPageShowsTheCallerWhichDataSetAndWhichLimit()
        => DataServiceResultCodes.ByName("DataSetNotPageable")
            .FormatClientMessage(ResultDetails.Create("dataSet", "BreweriesInNflCities", "reason", "kind 'Federated' does not page in the store"))
            .ShouldBe("Data set 'BreweriesInNflCities' cannot be queried page by page yet: kind 'Federated' does not page in the store");

    [Fact]
    public void ARefusalToGroupShowsTheCallerWhichDataSetAndWhichLimit()
        => DataServiceResultCodes.ByName("DataSetNotGroupable")
            .FormatClientMessage(ResultDetails.Create("dataSet", "BreweriesInNflCities", "reason", "kind 'Federated' does not run a grouped query in the store"))
            .ShouldBe("Data set 'BreweriesInNflCities' cannot be grouped: kind 'Federated' does not run a grouped query in the store");
}
