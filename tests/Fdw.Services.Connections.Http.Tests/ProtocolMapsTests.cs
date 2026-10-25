using Fdw.Services.Connections.Http.Protocols;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Tests;

/// <summary>
/// A protocol states which data commands it turns into a request. A connection reports the rest as having no translator,
/// so a command that expands into simpler ones runs those and any other fails with a named result.
/// </summary>
public sealed class ProtocolMapsTests
{
    [Theory]
    [InlineData("Query")]
    [InlineData("Insert")]
    [InlineData("Update")]
    [InlineData("Delete")]
    public void RestMapsTheFourBasicCommands(string commandType)
        => new RestProtocol().Maps(commandType).ShouldBeTrue();

    [Theory]
    [InlineData("Upsert")]
    [InlineData("BulkUpsert")]
    [InlineData("BulkUpdate")]
    [InlineData("Find")]
    [InlineData("ConfigurationSave")]
    [InlineData("insert")]
    public void RestDoesNotMapTheRest(string commandType)
        => new RestProtocol().Maps(commandType).ShouldBeFalse();

    [Theory]
    [InlineData("Upsert")]
    [InlineData("Find")]
    [InlineData("AnyOperation")]
    public void SoapMapsAnyCommandBecauseItNamesItsOperationAfterTheCommand(string commandType)
        => new Soap11Protocol().Maps(commandType).ShouldBeTrue();
}
