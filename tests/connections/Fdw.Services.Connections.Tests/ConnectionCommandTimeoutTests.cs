using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Abstractions.Timeouts;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// A data command that carries a command timeout reaches the connection kind's own hook before it runs, and a kind that cannot apply one
/// refuses the command (FDW-3324).
/// </summary>
public sealed class ConnectionCommandTimeoutTests
{
    private sealed class TimedConnection : ConnectionBase<ConnectionBaseTests.TestNativeCommand, ConnectionBaseTests.TestConnectionConfiguration, TimedConnection>
    {
        private readonly IDataCommandTranslator<ConnectionBaseTests.TestNativeCommand> _translator;

        public TimedConnection(IDataCommandTranslator<ConnectionBaseTests.TestNativeCommand> translator)
            : base(NullLogger<TimedConnection>.Instance, new ConnectionBaseTests.TestConnectionConfiguration { Name = "Timed" })
        {
            _translator = translator;
        }

        public List<(TimeSpan Timeout, string Owner)> Applied { get; } = [];

        public int Ran { get; private set; }

        protected override IDataCommandTranslator<ConnectionBaseTests.TestNativeCommand> GetTranslator(string commandType) => _translator;

        protected override IGenericResult ApplyCommandTimeout(ConnectionBaseTests.TestNativeCommand native, TimeSpan timeout, string owner)
        {
            Applied.Add((timeout, owner));
            return GenericResult.Success();
        }

        protected override Task<IGenericResult<T>> Execute<T>(
            ConnectionBaseTests.TestNativeCommand command, IStorageContainer container, CancellationToken cancellationToken)
        {
            Ran++;
            return Task.FromResult(GenericResult<T>.Success((T)(object)"ran"));
        }

        protected override Task<IGenericResult> Execute(
            ConnectionBaseTests.TestNativeCommand command, IStorageContainer container, CancellationToken cancellationToken)
        {
            Ran++;
            return Task.FromResult(GenericResult.Success());
        }
    }

    private static Mock<IDataCommandTranslator<ConnectionBaseTests.TestNativeCommand>> Translator()
    {
        var translator = new Mock<IDataCommandTranslator<ConnectionBaseTests.TestNativeCommand>>();
        translator.Setup(t => t.Name).Returns("Query");
        translator
            .Setup(t => t.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConnectionBaseTests.TestNativeCommand>.Success(new ConnectionBaseTests.TestNativeCommand { Sql = "SELECT 1" }));
        return translator;
    }

    private static IDataCommand CommandCarrying(TimeSpan? timeout)
    {
        var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (timeout is { } limit)
        {
            CommandTimeoutPolicy.Set(metadata, limit, "secret manager 'MsSqlSecrets'");
        }

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");
        command.Setup(c => c.Metadata).Returns(metadata);
        return command.Object;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ACommandCarryingATimeoutIsGivenItBeforeItRuns()
    {
        var connection = new TimedConnection(Translator().Object);

        var result = await connection.Execute<string>(CommandCarrying(TimeSpan.FromSeconds(2)), new Mock<IDataContainer>().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        connection.Applied.ShouldHaveSingleItem().ShouldBe((TimeSpan.FromSeconds(2), "secret manager 'MsSqlSecrets'"));
        connection.Ran.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ACommandCarryingNoTimeoutIsNotTouched()
    {
        var connection = new TimedConnection(Translator().Object);

        var result = await connection.Execute<string>(CommandCarrying(null), new Mock<IDataContainer>().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        connection.Applied.ShouldBeEmpty();
        connection.Ran.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AKindThatCannotApplyATimeoutRefusesTheCommandAndDoesNotRunIt()
    {
        var connection = new ConnectionBaseTests.TestConnection(
            NullLogger<ConnectionBaseTests.TestConnection>.Instance,
            new ConnectionBaseTests.TestConnectionConfiguration { Name = "Plain" },
            Translator().Object);

        var result = await connection.Execute<string>(CommandCarrying(TimeSpan.FromSeconds(2)), new Mock<IDataContainer>().Object, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldEndWith("-62034");
        result.Messages[^1].Message.ShouldContain("secret manager 'MsSqlSecrets'");
    }
}
