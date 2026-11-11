using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Results;
using Fdw.Services.EtlGateway;

namespace Fdw.Services.Etl.Tests.Gateway;

/// <summary>
/// Tests for the ETL gateway (FDW-1479): a script is refused without the DataEngineer role, for a store the host declares and when
/// it is unsound, and otherwise runs on the connection of the store, resolved by the store's name.
/// </summary>
public sealed class EtlGatewayServiceTests
{
    private static readonly Guid ConnectionId = Guid.NewGuid();

    private sealed class ScriptedConnection
    {
        public ScriptedConnection()
        {
            Mock = new Mock<IDataConnection>();
            Mock.As<IScriptedDataConnection>()
                .Setup(c => c.Execute(It.IsAny<IScriptedDataCommand<int>>(), It.IsAny<CancellationToken>()))
                .Returns((IScriptedDataCommand<int> command, CancellationToken token) =>
                {
                    Ran.Add(command);
                    return Task.FromResult<IGenericResult<int>>(GenericResult<int>.Success(1));
                });
        }

        public Mock<IDataConnection> Mock { get; }

        public List<IScriptedDataCommand> Ran { get; } = [];
    }

    private static (EtlGatewayService Sut, ScriptedConnection Connection, Mock<IDataConnectionProvider> Connections) Build(
        string[]? roles, bool hostDeclared = false, bool systemContext = false, IGenericResult<DataStoreReference>? lookup = null)
    {
        var connection = new ScriptedConnection();
        var store = new Mock<IDataStoreImplementationConfiguration>();
        store.SetupGet(s => s.ConnectionId).Returns(ConnectionId);
        var stores = new Mock<IDomainConfigurationProvider<IDataStoreImplementationConfiguration>>();
        stores.Setup(p => p.Get("Warehouse", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Success(store.Object));
        var connections = new Mock<IDataConnectionProvider>();
        connections.Setup(p => p.Get(ConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(connection.Mock.Object));
        var dataStores = new Mock<IDataStoreProvider>();
        dataStores.Setup(p => p.GetReference("Warehouse", It.IsAny<CancellationToken>()))
            .ReturnsAsync(lookup ?? GenericResult<DataStoreReference>.Success(new DataStoreReference("Warehouse", ConnectionId, hostDeclared)));
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.Roles).Returns(roles ?? []);
        caller.SetupGet(c => c.IsSystemContext).Returns(systemContext);
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(roles is null && !systemContext ? null : caller.Object);
        return (new EtlGatewayService(stores.Object, connections.Object, dataStores.Object, accessor.Object), connection, connections);
    }

    private static ScriptRunCommand Command(string text = "DELETE FROM dbo.T") => new(text, []);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerWithoutTheDataEngineerRoleIsRefusedAndNoConnectionIsResolved()
    {
        var (sut, connection, connections) = Build(["Admin"]);

        var ran = await sut.Execute(Command(), "Warehouse", TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("DataEngineer");
        connection.Ran.ShouldBeEmpty();
        connections.Verify(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheSystemContextAloneIsRefused()
    {
        var (sut, connection, _) = Build([], systemContext: true);

        var ran = await sut.Execute(Command(), "Warehouse", TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        connection.Ran.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AStoreTheHostDeclaresIsRefusedEvenToADataEngineer()
    {
        var (sut, connection, _) = Build(["DataEngineer"], hostDeclared: true);

        var ran = await sut.Execute(Command(), "Warehouse", TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("Warehouse");
        connection.Ran.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AHostDeclarationThatCannotBeReadRefusesTheScriptAndRunsNothing()
    {
        // Why: a lookup that broke says nothing about whether the store is the host's, so it is never read as "not the host's" (FDW-4281).
        var (sut, connection, connections) = Build(
            ["DataEngineer"], lookup: GenericResult<DataStoreReference>.Failure(new GenericMessage("the configuration read broke")));

        var ran = await sut.Execute(Command(), "Warehouse", TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("could not be read");
        connection.Ran.ShouldBeEmpty();
        connections.Verify(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AStoreTheProviderSaysDoesNotExistIsNotTheHostsAndGoesOnToBeResolved()
    {
        var (sut, connection, _) = Build(
            ["DataEngineer"], lookup: GenericResult<DataStoreReference>.Failure(DataServiceResultCodes.ByName("DataStoreNotFound")));

        var ran = await sut.Execute(Command(), "Warehouse", TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue();
        connection.Ran.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnUnsoundScriptIsRefusedBeforeAnythingIsResolved()
    {
        var (sut, connection, connections) = Build(["DataEngineer"]);

        var ran = await sut.Execute(Command(" "), "Warehouse", TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        connection.Ran.ShouldBeEmpty();
        connections.Verify(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADataEngineerRunsTheScriptOnTheConnectionOfTheNamedStore()
    {
        var (sut, connection, _) = Build(["dataengineer"]);
        var command = Command();

        var ran = await sut.Execute(command, "Warehouse", TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        connection.Ran.ShouldHaveSingleItem().ShouldBeSameAs(command);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStoreWithNoNameIsRefused()
    {
        var (sut, _, _) = Build(["DataEngineer"]);

        var ran = await sut.Execute(Command(), " ", TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
    }

    // What the caller is shown: the mapper puts a message's client text in the answer, and a message with none leaves the caller the
    // category's generic title (FDW-3338: a script on a connection that could not be built answered "The request was invalid").
    private static List<string> ClientTexts(IGenericResult result)
        => result.Messages.OfType<IClientMessage>().Select(message => message.ClientText).ToList();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFailureTheStoresConnectionReturnsNamesThatConnection()
    {
        var (sut, connection, _) = Build(["DataEngineer"]);
        connection.Mock.As<IScriptedDataConnection>()
            .Setup(c => c.Execute(It.IsAny<IScriptedDataCommand<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Failure(new GenericMessage("The server was not found or was not accessible")));

        var ran = await sut.Execute(Command(), "Warehouse", TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ConnectionFailures.ConnectionOf(ran).ShouldBe(ConnectionId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStoreWhoseConnectionTheProviderWillNotGiveNamesThatConnection()
    {
        var (sut, _, connections) = Build(["DataEngineer"]);
        connections.Setup(p => p.Get(ConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Failure(new GenericMessage("Connection 'Orders' has been disabled")));

        var ran = await sut.Execute(Command(), "Warehouse", TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ConnectionFailures.ConnectionOf(ran).ShouldBe(ConnectionId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARefusalNoConnectionRaisedNamesNone()
    {
        var (callerWithoutRole, _, _) = Build(["Reader"]);
        var (hostStore, _, _) = Build(["DataEngineer"], hostDeclared: true);
        var (unsound, _, _) = Build(["DataEngineer"]);

        var withoutRole = await callerWithoutRole.Execute(Command(), "Warehouse", TestContext.Current.CancellationToken);
        var host = await hostStore.Execute(Command(), "Warehouse", TestContext.Current.CancellationToken);
        var empty = await unsound.Execute(Command(" "), "Warehouse", TestContext.Current.CancellationToken);

        withoutRole.IsFailure.ShouldBeTrue();
        host.IsFailure.ShouldBeTrue();
        empty.IsFailure.ShouldBeTrue();
        ConnectionFailures.ConnectionOf(withoutRole).ShouldBeNull();
        ConnectionFailures.ConnectionOf(host).ShouldBeNull();
        ConnectionFailures.ConnectionOf(empty).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AStoreWhoseConnectionCannotBeResolvedTellsTheCallerWhy()
    {
        var (sut, _, connections) = Build(["DataEngineer"]);
        connections.Setup(p => p.Get(ConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Failure(
                new GenericMessage("Connection 'Orders' does not state MaxSerialInsert, the most rows an insert runs serially")));

        var ran = await sut.Execute(Command(), "Warehouse", TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        var shown = ClientTexts(ran).ShouldHaveSingleItem();
        shown.ShouldContain("Warehouse");
        shown.ShouldContain("does not state MaxSerialInsert");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AConnectionWithNoScriptExtensionTellsTheCallerWhy()
    {
        var (sut, _, connections) = Build(["DataEngineer"]);
        connections.Setup(p => p.Get(ConnectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataConnection>.Success(new Mock<IDataConnection>().Object));

        var ran = await sut.Execute(Command(), "Warehouse", TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ClientTexts(ran).ShouldHaveSingleItem().ShouldContain("does not take scripts on this host");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ACallerWithoutTheRoleIsToldWhichRoleIsNeeded()
    {
        var (sut, _, _) = Build(["Admin"]);

        var ran = await sut.Execute(Command(), "Warehouse", TestContext.Current.CancellationToken);

        ClientTexts(ran).ShouldHaveSingleItem().ShouldContain("DataEngineer");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AStoreTheHostDeclaresAndAnUnsoundScriptTellTheCallerWhy()
    {
        var (hosted, _, _) = Build(["DataEngineer"], hostDeclared: true);
        var (sound, _, _) = Build(["DataEngineer"]);

        var declared = await hosted.Execute(Command(), "Warehouse", TestContext.Current.CancellationToken);
        var unsound = await sound.Execute(Command(" "), "Warehouse", TestContext.Current.CancellationToken);

        ClientTexts(declared).ShouldHaveSingleItem().ShouldContain("declared by the host");
        ClientTexts(unsound).ShouldHaveSingleItem().ShouldContain("is refused");
    }
}
