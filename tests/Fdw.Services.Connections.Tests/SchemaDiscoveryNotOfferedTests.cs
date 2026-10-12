using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Connections.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// FDW-1051: a connection saved without a schema discovery option, or with one its type does not offer, is refused, and
/// the 400 says which option and what the type offers. Until the refusal carried a code that shows its reason, the caller
/// got "The request was invalid" and the cause was in the log only.
/// </summary>
public class SchemaDiscoveryNotOfferedTests
{
    private static ResultDetails Details(string option)
        => ResultDetails.Create("name", "UsgsQuakes", "option", option, "connectionType", "Http").With("offered", "None");

    [Fact]
    public void TheReasonNamesTheOptionGivenTheTypeAndWhatTheTypeOffers()
        => ConnectionsResultCodes.ByName("SchemaDiscoveryNotOffered")
            .FormatClientMessage(Details("Full"))
            .ShouldBe("Connection 'UsgsQuakes' cannot use schema discovery option 'Full': connection type 'Http' offers None");

    [Fact]
    public void AnOmittedOptionIsShownAsEmptyAndStillSaysWhatIsOffered()
        => ConnectionsResultCodes.ByName("SchemaDiscoveryNotOffered")
            .FormatClientMessage(Details(string.Empty))
            .ShouldBe("Connection 'UsgsQuakes' cannot use schema discovery option '': connection type 'Http' offers None");

    [Fact]
    public void TheCallerGetsA400WhoseDetailIsTheReasonNotTheGenericLine()
    {
        var result = GenericResult.Failure(ConnectionsResultCodes.ByName("SchemaDiscoveryNotOffered"), Details(string.Empty));

        var (status, response) = ResultHttpStatusMapper.Map(result, new DefaultHttpContext());

        status.ShouldBe(400);
        response.Detail.ShouldNotBeNull().ShouldContain("offers None");
        response.Title.ShouldBe("The request was invalid");
    }
}
