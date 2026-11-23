using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Builders;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// FDW-6682: a container type declares the operations it supports, and one shared rule judges a command against them (<see cref="DataOperationRule"/>), as
/// <see cref="DataContainerTypeRule"/> judges the type a container states. The connection asks it before a translator that states the operations its command
/// performs (<see cref="IStatesOperations"/>) builds any native command: a container whose type does not state one of them is refused with a coded message, DATA-61148, and
/// nothing reaches the server. A translator that states none is not asked (the connection kinds that do not state them yet).
/// </summary>
public sealed class DataOperationRuleTests
{
    private static Mock<IDataContainer> Container(string name, string type, params string[] operations)
    {
        var containerType = new Mock<IContainerType>();
        containerType.Setup(t => t.Name).Returns(type);
        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.As<IStorageContainer>().Setup(c => c.Name).Returns(name);
        container.Setup(c => c.ContainerType).Returns(containerType.Object);
        container.As<IStorageContainer>().Setup(c => c.ContainerType).Returns(containerType.Object);
        container.Setup(c => c.SupportedOperations).Returns(operations);
        container.As<IStorageContainer>().Setup(c => c.SupportedOperations).Returns(operations);
        return container;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AContainerThatStatesEveryOperationTheCommandPerformsIsAccepted()
    {
        var container = Container("Orders", "Table", "Query", "Insert", "Update", "Delete");

        DataOperationRule.Check(container.Object, ["Insert", "Update"], NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AContainerThatMissesOneOperationIsRefusedNamingTheContainerItsTypeTheOperationAndWhatItStates()
    {
        var container = Container("UserPermissionGrant", "View", "Query");

        var refused = DataOperationRule.Check(container.Object, ["Insert", "Update"], NullLogger.Instance);

        refused.IsFailure.ShouldBeTrue();
        refused.Messages.Select(message => message.Code).ShouldContain("DATA-61148");
        var text = refused.CurrentMessage.ShouldNotBeNull();
        text.ShouldContain("UserPermissionGrant");
        text.ShouldContain("View");
        text.ShouldContain("Insert");
        text.ShouldContain("Query");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AContainerThatStatesNoOperationAtAllIsRefusedNeverAssumedToStateThem()
    {
        var container = Container("Orders", "Table");

        DataOperationRule.Check(container.Object, ["Query"], NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    private sealed class StatingTranslator(params string[] operations) : IDataCommandTranslator<ConnectionBaseTests.TestNativeCommand>, IStatesOperations
    {
        public int Translated { get; private set; }

        public string Name => "Insert";

        public string DomainName => "Test";

        public IReadOnlyList<string> Operations => operations;

        public int Id => 1;

        public string Category => "Test";

        public Task<IGenericResult<ConnectionBaseTests.TestNativeCommand>> Translate(IDataCommand command, IStorageContainer container, CancellationToken cancellationToken = default)
        {
            Translated++;
            return Task.FromResult<IGenericResult<ConnectionBaseTests.TestNativeCommand>>(
                GenericResult<ConnectionBaseTests.TestNativeCommand>.Success(new ConnectionBaseTests.TestNativeCommand { Sql = "SELECT 1" }));
        }
    }

    private sealed class RuleConnection : ConnectionBase<ConnectionBaseTests.TestNativeCommand, ConnectionBaseTests.TestConnectionConfiguration, RuleConnection>
    {
        private readonly IDataCommandTranslator<ConnectionBaseTests.TestNativeCommand> _translator;

        public RuleConnection(IDataCommandTranslator<ConnectionBaseTests.TestNativeCommand> translator)
            : base(NullLogger<RuleConnection>.Instance, new ConnectionBaseTests.TestConnectionConfiguration { Name = "Rule" })
        {
            _translator = translator;
        }

        public int Ran { get; private set; }

        protected override IDataCommandTranslator<ConnectionBaseTests.TestNativeCommand> GetTranslator(string commandType) => _translator;

        protected override IGenericResult ApplyCommandTimeout(ConnectionBaseTests.TestNativeCommand native, TimeSpan timeout, string owner) => GenericResult.Success();

        protected override Task<IGenericResult<T>> Execute<T>(ConnectionBaseTests.TestNativeCommand command, IStorageContainer container, CancellationToken cancellationToken)
        {
            Ran++;
            return Task.FromResult(GenericResult<T>.Success((T)(object)"ran"));
        }

        protected override Task<IGenericResult> Execute(ConnectionBaseTests.TestNativeCommand command, IStorageContainer container, CancellationToken cancellationToken)
        {
            Ran++;
            return Task.FromResult(GenericResult.Success());
        }
    }

    private static IDataCommand Command()
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Insert");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
        return command.Object;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task AnOperationTheContainerTypeDoesNotStateIsRefusedBeforeAnythingIsTranslatedOrSent()
    {
        var translator = new StatingTranslator("Insert");
        var connection = new RuleConnection(translator);

        var result = await connection.Execute<string>(Command(), Container("UserPermissionGrant", "View", "Query").Object, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(message => message.Code).ShouldContain("DATA-61148");
        translator.Translated.ShouldBe(0);
        connection.Ran.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task AnOperationTheContainerTypeStatesIsTranslatedAndRun()
    {
        var translator = new StatingTranslator("Insert");
        var connection = new RuleConnection(translator);

        var result = await connection.Execute<string>(Command(), Container("Orders", "Table", "Query", "Insert").Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        translator.Translated.ShouldBe(1);
        connection.Ran.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task ATranslatorThatStatesNoOperationsIsNotAskedTheRule()
    {
        var translator = new Mock<IDataCommandTranslator<ConnectionBaseTests.TestNativeCommand>>();
        translator.Setup(t => t.Name).Returns("Query");
        translator
            .Setup(t => t.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConnectionBaseTests.TestNativeCommand>.Success(new ConnectionBaseTests.TestNativeCommand { Sql = "SELECT 1" }));
        var connection = new RuleConnection(translator.Object);

        var result = await connection.Execute<string>(Command(), Container("Feed", "Endpoint").Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        connection.Ran.ShouldBe(1);
    }
}
