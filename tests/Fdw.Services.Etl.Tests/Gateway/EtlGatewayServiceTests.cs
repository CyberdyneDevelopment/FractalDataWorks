using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
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
        string[]? roles, bool hostDeclared = false, bool systemContext = false)
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
        var dataStore = new Mock<IDataStore>();
        dataStore.SetupGet(s => s.DeclaredByHost).Returns(hostDeclared);
        var dataStores = new Mock<IDataStoreProvider>();
        dataStores.Setup(p => p.Get("Warehouse", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStore>.Success(dataStore.Object));
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
}
