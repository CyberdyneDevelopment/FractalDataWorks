using System;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Abstractions.Tests;

/// <summary>
/// FDW-4279: a failure names the connection that raised it, and the name survives <c>Caused</c> and <c>ToNewResult</c>.
/// </summary>
public sealed class ConnectionFailuresTests
{
    private static readonly Guid Connection = new("cccccccc-0000-0000-0000-000000000001");

    private static IGenericResult Failed() => GenericResult.Failure(new GenericMessage(MessageSeverity.Error, "login failed", "MSSQL-50001"));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AFailureARaisedByConnectionNamesItAndKeepsItsCode()
    {
        var raised = ConnectionFailures.RaisedBy<int>(Failed(), Connection);

        ConnectionFailures.ConnectionOf(raised).ShouldBe(Connection);
        raised.Messages[0].Code.ShouldBe("MSSQL-50001");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AFailureNoConnectionRaisedNamesNone()
    {
        ConnectionFailures.ConnectionOf(Failed()).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CausedKeepsTheNameOfTheConnectionThatRaisedTheCause()
    {
        var cause = ConnectionFailures.RaisedBy<int>(Failed(), Connection);

        var caused = ConnectionFailures.Caused<string>(cause, new GenericMessage(MessageSeverity.Error, "store could not be resolved", "DATA-31010"));

        ConnectionFailures.ConnectionOf(caused).ShouldBe(Connection);
        caused.Messages[0].Code.ShouldBe("MSSQL-50001");
        caused.CurrentMessage.ShouldBe("store could not be resolved");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CausedOfAFailureThatNamesNoConnectionNamesNone()
    {
        var caused = ConnectionFailures.Caused<string>(Failed(), new GenericMessage(MessageSeverity.Error, "store could not be resolved", "DATA-31010"));

        ConnectionFailures.ConnectionOf(caused).ShouldBeNull();
    }
}
