using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.Authentication;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// FDW-1202: a connection owns the secrets its credential slots name, so deleting the connection deletes them. They are
/// read before the connection goes and deleted only after it is gone; one that cannot be deleted is left and never fails a
/// delete that happened.
/// </summary>
public class ConnectionCredentialDeleteTests
{
    private const string Store = "MsSqlSecrets";
    private const string Field = "SecretKeyName";
    private const string Secret = "Connection-0b833d9aebd94376a308e45a9555fe04-SecretKeyName";
    private const string OlderSecret = "Connection.0b833d9aebd94376a308e45a9555fe04.SecretKeyName";
    private const string OperatorSecret = "base-power-api-key";

    private static readonly ConnectionCredentialField Password = new(Field, "Password", secret: true);

    private static (Mock<IConnectionType> Type, IConnectionImplementationConfiguration Connection) Connection(Dictionary<string, string?> values)
    {
        var connection = new Mock<IConnectionImplementationConfiguration>();
        connection.SetupGet(c => c.Name).Returns("Warehouse");
        var type = new Mock<IConnectionType>();
        type.SetupGet(t => t.Name).Returns("MsSql");
        type.Setup(t => t.CredentialSlots(connection.Object))
            .Returns(() => GenericResult<ConnectionCredentialSlots>.Success(ConnectionCredentialSlots.From("SqlAuth", [Password], values)));
        return (type, connection.Object);
    }

    private static (IServiceProvider Services, List<string> Events) Services(bool secretDeleteSucceeds = true)
    {
        var events = new List<string>();
        var manager = new Mock<ISecretManager>();
        manager.Setup(m => m.Execute(It.IsAny<ISecretManagerCommand<IGenericResult>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ISecretManagerCommand<IGenericResult> command, CancellationToken _) =>
            {
                events.Add($"secret:{command.SecretKey}");
                return secretDeleteSucceeds
                    ? GenericResult<IGenericResult>.Success(GenericResult.Success())
                    : GenericResult<IGenericResult>.Failure(
                        ConnectionsResultCodes.ByName("CredentialsUndetermined"),
                        ResultDetails.Create("name", "Warehouse", "reason", "the secret manager refused the delete"));
            });

        var secrets = new Mock<ISecretManagerProvider>();
        secrets.Setup(p => p.Get(Store, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ISecretManager>.Success(manager.Object));

        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(ISecretManagerProvider))).Returns(secrets.Object);
        return (services.Object, events);
    }

    private static Func<CancellationToken, Task<IGenericResult>> DeleteThat(List<string> events, bool succeeds) => _ =>
    {
        events.Add("connection");
        return Task.FromResult<IGenericResult>(succeeds
            ? GenericResult.Success()
            : GenericResult.Failure(
                ConnectionsResultCodes.ByName("CredentialsUndetermined"),
                ResultDetails.Create("name", "Warehouse", "reason", "the delete was refused")));
    };

    private static Dictionary<string, string?> Stored() =>
        new() { [ConnectionCredentialProperties.SecretManager] = Store, [Field] = Secret };

    [Fact]
    public async Task DeletingAConnectionDeletesTheSecretsItNames()
    {
        var (type, connection) = Connection(Stored());
        var (services, events) = Services();

        var result = await ConnectionCredentialStore.DeleteConnection(
            type.Object, connection, DeleteThat(events, succeeds: true), services, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        events.ShouldBe(["connection", $"secret:{Secret}"]);
    }

    [Fact]
    public async Task DeletingAConnectionLeavesASecretTheStoreDidNotMint()
    {
        var (type, connection) = Connection(new Dictionary<string, string?> { [ConnectionCredentialProperties.SecretManager] = Store, [Field] = OperatorSecret });
        var (services, events) = Services();

        var result = await ConnectionCredentialStore.DeleteConnection(
            type.Object, connection, DeleteThat(events, succeeds: true), services, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        events.ShouldBe(["connection"]);
    }

    [Fact]
    public async Task DeletingAConnectionDeletesASecretMintedUnderTheOlderDottedName()
    {
        var (type, connection) = Connection(new Dictionary<string, string?> { [ConnectionCredentialProperties.SecretManager] = Store, [Field] = OlderSecret });
        var (services, events) = Services();

        await ConnectionCredentialStore.DeleteConnection(
            type.Object, connection, DeleteThat(events, succeeds: true), services, NullLogger.Instance, TestContext.Current.CancellationToken);

        events.ShouldBe(["connection", $"secret:{OlderSecret}"]);
    }

    [Fact]
    public async Task RetiringSecretsDeletesTheMintedOnesAndLeavesTheOthers()
    {
        var (services, events) = Services();

        await ConnectionCredentialStore.Retire(
            [new StoredSecret(Store, OperatorSecret), new StoredSecret(Store, Secret)],
            "Warehouse",
            services,
            NullLogger.Instance,
            TestContext.Current.CancellationToken);

        events.ShouldBe([$"secret:{Secret}"]);
    }

    [Fact]
    public async Task ASecretIsDeletedOnlyAfterTheConnectionIsGone()
    {
        var (type, connection) = Connection(Stored());
        var (services, events) = Services();

        await ConnectionCredentialStore.DeleteConnection(
            type.Object, connection, DeleteThat(events, succeeds: true), services, NullLogger.Instance, TestContext.Current.CancellationToken);

        events.IndexOf("connection").ShouldBeLessThan(events.IndexOf($"secret:{Secret}"));
    }

    [Fact]
    public async Task AConnectionThatNamesNoSecretsDeletesAsBefore()
    {
        var (type, connection) = Connection(new Dictionary<string, string?> { ["Username"] = "app" });
        var (services, events) = Services();

        var result = await ConnectionCredentialStore.DeleteConnection(
            type.Object, connection, DeleteThat(events, succeeds: true), services, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        events.ShouldBe(["connection"]);
    }

    [Fact]
    public async Task AConnectionThatFailsToDeleteKeepsItsSecrets()
    {
        var (type, connection) = Connection(Stored());
        var (services, events) = Services();

        var result = await ConnectionCredentialStore.DeleteConnection(
            type.Object, connection, DeleteThat(events, succeeds: false), services, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        events.ShouldBe(["connection"]);
    }

    [Fact]
    public async Task ASecretThatCannotBeDeletedIsLeftAndTheDeleteStillSucceeds()
    {
        var (type, connection) = Connection(Stored());
        var (services, events) = Services(secretDeleteSucceeds: false);

        var result = await ConnectionCredentialStore.DeleteConnection(
            type.Object, connection, DeleteThat(events, succeeds: true), services, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        events.ShouldBe(["connection", $"secret:{Secret}"]);
    }

    [Fact]
    public async Task AConnectionWhoseCredentialsCannotBeDeterminedIsRefusedAndNotDeleted()
    {
        var connection = new Mock<IConnectionImplementationConfiguration>();
        connection.SetupGet(c => c.Name).Returns("Warehouse");
        var type = new Mock<IConnectionType>();
        type.Setup(t => t.CredentialSlots(connection.Object)).Returns(GenericResult<ConnectionCredentialSlots>.Failure(
            ConnectionsResultCodes.ByName("CredentialsUndetermined"),
            ResultDetails.Create("name", "Warehouse", "reason", "its type is not registered")));
        var (services, events) = Services();

        var result = await ConnectionCredentialStore.DeleteConnection(
            type.Object, connection.Object, DeleteThat(events, succeeds: true), services, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code!.Name.ShouldBe("CredentialsUndetermined");
        events.ShouldBeEmpty();
    }
}
