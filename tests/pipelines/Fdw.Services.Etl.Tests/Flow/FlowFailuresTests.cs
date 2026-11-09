using System;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Flow;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// FDW-4197: <see cref="FlowFailures"/> builds a component's failure with the cause's messages first and the flow's message last, for a component that returns a value
/// as for one that does not.
/// </summary>
public sealed class FlowFailuresTests
{
    private static IGenericResult Cause() => GenericResult.Failure(new GenericMessage(MessageSeverity.Error, "the server refused the connection", "MSSQL-70001"));

    private static GenericMessage Context() => new GenericMessage(MessageSeverity.Error, "Flow component 'x' failed: the server refused the connection", "FLOW-20903");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ATypedFailureKeepsTheCausesCodeFirstAndTheFlowsSentenceLast()
    {
        var failed = FlowFailures.Caused<int>(Context(), Cause());

        failed.IsFailure.ShouldBeTrue();
        failed.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe("MSSQL-70001");
        failed.CurrentMessage.ShouldBe("Flow component 'x' failed: the server refused the connection");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ATypedFailureOfAnUncodedCauseStaysUnclassified()
    {
        var failed = FlowFailures.Caused<int>(
            new GenericMessage(MessageSeverity.Error, "Flow component 'x' failed: boom"), GenericResult.Failure(new GenericMessage("boom")));

        RunFailureClassification.Of(failed).Classified.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheConnectionThatRaisedAFailureReachesTheRunsFailureEventThroughAComponentsFailure()
    {
        // The whole path of FDW-4279: the data layer names the connection at the raise site, the component keeps the cause with FlowFailures, and the run's failure
        // event reads the connection from the failure itself.
        var connection = new Guid("cccccccc-0000-0000-0000-000000000001");
        var raised = ConnectionFailures.RaisedBy<int>(Cause(), connection);

        var componentFailure = FlowFailures.Caused(Context(), raised);
        var data = RunFailureClassification.Of(componentFailure).ToEventData();

        data["connectionId"].ShouldBe(connection.ToString());
        data["code"].ShouldBe("MSSQL-70001");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AFailureNoConnectionRaisedAttributesToNoConnection()
    {
        var componentFailure = FlowFailures.Caused(Context(), Cause());

        RunFailureClassification.Of(componentFailure).ToEventData()["connectionId"].ShouldBeNull();
    }
}
