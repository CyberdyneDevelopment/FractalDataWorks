using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.Authentication;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.MsSql.Authentication;
using Fdw.Services.Connections.Results;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// FDW-1086: a connection's credentials are written through the connection and never read back. The caller sends the
/// values; the server chooses the secret manager (a server setting) and the secret names, points the connection at them,
/// and deletes the secrets the connection no longer names. A request that names a secret itself is refused.
/// </summary>
public class ConnectionCredentialStoreTests
{
    private const string Store = "MsSqlSecrets";
    private const string Field = "SecretKeyName";

    private static readonly ConnectionCredentialField Password = new(Field, "Password", secret: true);

    private static (Mock<IConnectionType> Type, IConnectionImplementationConfiguration Connection) SqlAuthConnection(
        Dictionary<string, string?> values)
    {
        var connection = new Mock<IConnectionImplementationConfiguration>();
        connection.SetupGet(c => c.Name).Returns("Warehouse");
        var type = new Mock<IConnectionType>();
        type.SetupGet(t => t.Name).Returns("MsSql");
        type.Setup(t => t.CredentialSlots(connection.Object))
            .Returns(() => GenericResult<ConnectionCredentialSlots>.Success(ConnectionCredentialSlots.From("SqlAuth", [Password], values)));
        type.Setup(t => t.BindCredentials(connection.Object, It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns((IConnectionImplementationConfiguration _, string manager, IReadOnlyDictionary<string, string> keys) =>
            {
                ConnectionCredentialSlots.Bind(values, manager, keys);
                return GenericResult.Success();
            });
        return (type, connection.Object);
    }

    private const string UsernameField = "UsernameSecretName";
    private const string PasswordField = "PasswordSecretName";

    private static (Mock<IConnectionType> Type, IConnectionImplementationConfiguration Connection) BasicConnection(
        Dictionary<string, string?> values)
    {
        var connection = new Mock<IConnectionImplementationConfiguration>();
        connection.SetupGet(c => c.Name).Returns("Warehouse");
        var fields = new List<ConnectionCredentialField>
        {
            new(UsernameField, "User name", secret: false),
            new(PasswordField, "Password", secret: true)
        };
        var type = new Mock<IConnectionType>();
        type.SetupGet(t => t.Name).Returns("Http");
        type.Setup(t => t.CredentialSlots(connection.Object))
            .Returns(() => GenericResult<ConnectionCredentialSlots>.Success(ConnectionCredentialSlots.From("Basic", fields, values)));
        type.Setup(t => t.BindCredentials(connection.Object, It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns((IConnectionImplementationConfiguration _, string manager, IReadOnlyDictionary<string, string> keys) =>
            {
                ConnectionCredentialSlots.Bind(values, manager, keys);
                return GenericResult.Success();
            });
        return (type, connection.Object);
    }

    private static (IServiceProvider Services, Mock<ISecretManager> Manager) Services(string? credentialStore, bool setSucceeds = true)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings.Setup(s => s.GetEffectiveValue<string>(SettingDefinitions.ConnectionCredentialSecretManager, null, null))
            .Returns(credentialStore!);

        var manager = new Mock<ISecretManager>();
        manager.Setup(m => m.Execute(It.IsAny<ISecretManagerCommand<SecretValue>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ISecretManagerCommand<SecretValue> command, CancellationToken _) => setSucceeds
                ? GenericResult<SecretValue>.Success(new SecretValue(command.SecretKey!, "stored"))
                : GenericResult<SecretValue>.Failure(
                    ConnectionsResultCodes.ByName("CredentialsUndetermined"),
                    ResultDetails.Create("name", "Warehouse", "reason", "the secret manager refused the write")));
        manager.Setup(m => m.Execute(It.IsAny<ISecretManagerCommand<IGenericResult>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IGenericResult>.Success(GenericResult.Success()));

        var secrets = new Mock<ISecretManagerProvider>();
        secrets.Setup(p => p.Get(Store, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ISecretManager>.Success(manager.Object));

        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(IEffectiveSettingsProvider))).Returns(settings.Object);
        services.Setup(s => s.GetService(typeof(ISecretManagerProvider))).Returns(secrets.Object);
        return (services.Object, manager);
    }

    private static string? Code(IGenericResult result) => result.Code?.Name;

    [Fact]
    public void EveryMsSqlMethodStatesItsCredentialFields()
    {
        MsSqlAuthenticationTypes.ByName("SqlAuth").CredentialFields.ShouldBe([Field]);
        MsSqlAuthenticationTypes.ByName("WindowsAuth").CredentialFields.ShouldBeEmpty();
        MsSqlAuthenticationTypes.ByName("ManagedIdentity").CredentialFields.ShouldBeEmpty();
    }

    [Fact]
    public void TheServerSetsTheCredentialFieldsAndTheSecretManagerAndNothingElse()
    {
        var sqlAuth = MsSqlAuthenticationTypes.ByName("SqlAuth");

        sqlAuth.IsServerSet(Field).ShouldBeTrue();
        sqlAuth.IsServerSet(ConnectionCredentialProperties.SecretManager).ShouldBeTrue();
        sqlAuth.IsServerSet("Username").ShouldBeFalse();
    }

    [Fact]
    public void ANewConnectionThatNamesASecretIsRefused()
    {
        var (type, connection) = SqlAuthConnection(new Dictionary<string, string?> { ["Username"] = "app", [Field] = "TokenSigningKey" });

        var result = ConnectionCredentialStore.RefuseCallerReferences(type.Object, before: null, connection, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Code(result).ShouldBe("CredentialReferenceReserved");
    }

    [Fact]
    public void ANewConnectionThatNamesASecretManagerIsRefused()
    {
        var (type, connection) = SqlAuthConnection(new Dictionary<string, string?> { [ConnectionCredentialProperties.SecretManager] = Store });

        ConnectionCredentialStore.RefuseCallerReferences(type.Object, before: null, connection, NullLogger.Instance)
            .IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void AnUpdateThatKeepsTheStoredReferencesIsAllowed()
    {
        var values = new Dictionary<string, string?> { [ConnectionCredentialProperties.SecretManager] = Store, [Field] = "Connection.a.SecretKeyName" };
        var (type, connection) = SqlAuthConnection(values);
        var before = ConnectionCredentialSlots.From("SqlAuth", [Password], values);

        ConnectionCredentialStore.RefuseCallerReferences(type.Object, before, connection, NullLogger.Instance)
            .IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AnUpdateThatPointsAStoredCredentialAtAnotherSecretIsRefused()
    {
        var before = ConnectionCredentialSlots.From("SqlAuth", [Password],
            new Dictionary<string, string?> { [ConnectionCredentialProperties.SecretManager] = Store, [Field] = "Connection.a.SecretKeyName" });
        var (type, connection) = SqlAuthConnection(
            new Dictionary<string, string?> { [ConnectionCredentialProperties.SecretManager] = Store, [Field] = "TokenSigningKey" });

        ConnectionCredentialStore.RefuseCallerReferences(type.Object, before, connection, NullLogger.Instance)
            .IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task AConnectionWhoseMethodNeedsACredentialIsRefusedWithoutOne()
    {
        var (type, connection) = SqlAuthConnection(new Dictionary<string, string?> { ["Username"] = "app" });
        var (services, _) = Services(Store);

        var result = await ConnectionCredentialStore.Store(type.Object, connection, credentials: null, services, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        Code(result).ShouldBe("CredentialRequired");
    }

    [Fact]
    public async Task CredentialsMustNameExactlyTheFieldsTheMethodUses()
    {
        var (type, connection) = SqlAuthConnection(new Dictionary<string, string?> { ["Username"] = "app" });
        var (services, manager) = Services(Store);

        var result = await ConnectionCredentialStore.Store(
            type.Object, connection, new Dictionary<string, string> { ["Password"] = "x" }, services, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        Code(result).ShouldBe("CredentialFieldsInvalid");
        manager.Verify(m => m.Execute(It.IsAny<ISecretManagerCommand<SecretValue>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NothingIsStoredWhenNoCredentialSecretManagerIsConfigured()
    {
        var (type, connection) = SqlAuthConnection(new Dictionary<string, string?> { ["Username"] = "app" });
        var (services, manager) = Services(credentialStore: string.Empty);

        var result = await ConnectionCredentialStore.Store(
            type.Object, connection, new Dictionary<string, string> { [Field] = "s3cret" }, services, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        Code(result).ShouldBe("CredentialStoreNotConfigured");
        manager.Verify(m => m.Execute(It.IsAny<ISecretManagerCommand<SecretValue>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StoringWritesTheValueUnderAServerChosenSecretAndPointsTheConnectionAtIt()
    {
        var values = new Dictionary<string, string?> { ["Username"] = "app" };
        var (type, connection) = SqlAuthConnection(values);
        var (services, manager) = Services(Store);

        var result = await ConnectionCredentialStore.Store(
            type.Object, connection, new Dictionary<string, string> { [Field] = "s3cret" }, services, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        values[ConnectionCredentialProperties.SecretManager].ShouldBe(Store);
        values[Field].ShouldStartWith("Connection-");
        ConnectionCredentialSecretName.IsStorable(values[Field]!).ShouldBeTrue();
        values[Field].ShouldNotBe("s3cret");
        result.Value!.Written.ShouldBe([new StoredSecret(Store, values[Field]!)]);
        manager.Verify(m => m.Execute(
            It.Is<ISecretManagerCommand<SecretValue>>(c => c.SecretKey == values[Field]), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplacingACredentialRetiresTheSecretItReplaced()
    {
        var values = new Dictionary<string, string?> { [ConnectionCredentialProperties.SecretManager] = Store, [Field] = "Connection.old.SecretKeyName" };
        var (type, connection) = SqlAuthConnection(values);
        var (services, _) = Services(Store);

        var result = await ConnectionCredentialStore.Store(
            type.Object, connection, new Dictionary<string, string> { [Field] = "n3w" }, services, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Retiring.ShouldBe([new StoredSecret(Store, "Connection.old.SecretKeyName")]);
        values[Field].ShouldNotBe("Connection.old.SecretKeyName");
    }

    [Fact]
    public async Task AFailedWriteLeavesTheConnectionPointingAtNothingNew()
    {
        var values = new Dictionary<string, string?> { ["Username"] = "app" };
        var (type, connection) = SqlAuthConnection(values);
        var (services, _) = Services(Store, setSucceeds: false);

        var result = await ConnectionCredentialStore.Store(
            type.Object, connection, new Dictionary<string, string> { [Field] = "s3cret" }, services, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        values.ContainsKey(Field).ShouldBeFalse();
        type.Verify(t => t.BindCredentials(It.IsAny<IConnectionImplementationConfiguration>(), It.IsAny<string>(),
            It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Never);
    }

    [Fact]
    public async Task BasicCredentialsAreStoredEachUnderItsOwnServerChosenNameWithItsValueAndAType()
    {
        var values = new Dictionary<string, string?> { ["BaseUrl"] = "https://example.com/api" };
        var (type, connection) = BasicConnection(values);
        var (services, manager) = Services(Store);

        var result = await ConnectionCredentialStore.Store(
            type.Object, connection, new Dictionary<string, string> { [UsernameField] = "u", [PasswordField] = "p" }, services,
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var commands = manager.Invocations.Select(invocation => invocation.Arguments[0]).OfType<SetSecretManagerCommand>().ToList();
        commands.Count.ShouldBe(2);
        commands.Single(command => command.SecretKey == values[UsernameField]).SecretValue.ShouldBe("u");
        commands.Single(command => command.SecretKey == values[PasswordField]).SecretValue.ShouldBe("p");
        values[UsernameField].ShouldEndWith("-" + UsernameField);
        values[PasswordField].ShouldEndWith("-" + PasswordField);
        commands.ShouldAllBe(command => command.SecretType == "ConnectionCredential");
    }

    [Fact]
    public async Task ACredentialTheStoreRefusesIsReportedAsTheStoreFailingNotAsABadRequest()
    {
        var values = new Dictionary<string, string?> { ["BaseUrl"] = "https://example.com/api" };
        var (type, connection) = BasicConnection(values);
        var (services, _) = Services(Store, setSucceeds: false);

        var result = await ConnectionCredentialStore.Store(
            type.Object, connection, new Dictionary<string, string> { [UsernameField] = "u", [PasswordField] = "p" }, services,
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        Code(result).ShouldBe("CredentialPersistFailed");
        (result.Code!.EventId / 10000).ShouldBe(7);
        var shown = result.Code.FormatClientMessage(result.Details);
        shown.ShouldNotBeNull();
        shown.ShouldContain("Warehouse");
        shown.ShouldContain(UsernameField);
        shown.ShouldContain(ConnectionsResultCodes.ByName("CredentialsUndetermined").Code);
    }

    [Fact]
    public async Task AStoreThatLogsACodedMessageInsteadOfReturningACodeStillShowsItsCode()
    {
        var message = new Mock<IGenericMessage>();
        message.SetupGet(m => m.Message).Returns("Secret value is required for SetSecret command");
        message.SetupGet(m => m.Code).Returns("MSSQL2-21007");
        var values = new Dictionary<string, string?> { ["BaseUrl"] = "https://example.com/api" };
        var (type, connection) = BasicConnection(values);
        var (services, manager) = Services(Store);
        manager.Setup(m => m.Execute(It.IsAny<ISecretManagerCommand<SecretValue>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<SecretValue>.Failure(message.Object));

        var result = await ConnectionCredentialStore.Store(
            type.Object, connection, new Dictionary<string, string> { [UsernameField] = "u", [PasswordField] = "p" }, services,
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        Code(result).ShouldBe("CredentialPersistFailed");
        result.Code!.FormatClientMessage(result.Details).ShouldNotBeNull().ShouldContain("MSSQL2-21007");
        values.ContainsKey(UsernameField).ShouldBeFalse();
    }

    [Fact]
    public void SlotsReportWhichFieldsAreSetAndNeverAValue()
    {
        var slots = ConnectionCredentialSlots.From("Basic", [new("UsernameSecretName", "User name", secret: false), new("PasswordSecretName", "Password", secret: true)],
            new Dictionary<string, string?> { [ConnectionCredentialProperties.SecretManager] = Store, ["UsernameSecretName"] = "Connection.a.UsernameSecretName" });

        slots.SetFields.ShouldBe(["UsernameSecretName"]);
        slots.MissingFields.ShouldBe(["PasswordSecretName"]);
    }

    [Fact]
    public void TheNewCredentialCodesShowTheCallerWhy()
    {
        foreach (var name in new[] { "CredentialsUndetermined", "CredentialReferenceReserved", "CredentialFieldsInvalid", "CredentialRequired", "CredentialTestNotSupported", "CredentialPersistFailed", "CredentialFieldNameRefused" })
        {
            ConnectionsResultCodes.ByName(name).FormatClientMessage(ResultDetails.Create("name", "Warehouse"))
                .ShouldNotBeNullOrWhiteSpace(name);
        }
    }
}
