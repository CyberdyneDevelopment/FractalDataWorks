using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-4211: the failure of a data store's connection that the provider would not give keeps the provider's messages first and the gateway's sentence last, so a run is classified by
/// what the provider said (a disabled connection is a Refused failure) and the sentence readers print is the gateway's as before.
/// </summary>
public sealed class DataGatewayConnectionFailureTests
{
    private static GenericMessage Refusal() =>
        new(MessageSeverity.Error, "Connection 'Sales' has been disabled since 2026-11-06", "CONN-120000");

    private static GenericMessage Wrapper() =>
        new(MessageSeverity.Error, "Connection for DataStore 'SalesStore' (ConnectionId=1) could not be resolved: Connection 'Sales' has been disabled", "DATA-31010");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Data")]
    public void AConnectionTheProviderRefusedIsClassifiedByTheRefusalAndPrintsTheGatewaysSentence()
    {
        var failed = ConnectionFailures.Caused<int>(GenericResult.Failure(Refusal()), Wrapper());

        failed.IsFailure.ShouldBeTrue();
        failed.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe("CONN-120000");
        FailureCategory.Of(failed).ShouldNotBeNull().Name.ShouldBe("Refused");
        failed.CurrentMessage.ShouldNotBeNull().ShouldStartWith("Connection for DataStore 'SalesStore'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Data")]
    public void AProviderFailureWithNoCodedMessageLeavesTheGatewaysOwnCodedEventFirstCoded()
    {
        var failed = ConnectionFailures.Caused<int>(GenericResult.Failure(new GenericMessage("the provider said no")), Wrapper());

        failed.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe("DATA-31010");
    }
}
