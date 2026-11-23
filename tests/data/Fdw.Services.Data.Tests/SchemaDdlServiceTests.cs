using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Abstractions;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Results;
using Fdw.Services.Connections.TestDouble;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-3617: DDL runs on a connection through the connection's own type, as discovery does. The service resolves the connection by name
/// and its type, and a type that does not offer DDL execution is refused before the connection is asked for; nothing stands in for it.
/// </summary>
public sealed class SchemaDdlServiceTests
{
    private readonly Mock<IConnectionConfigurationProvider> _configurations = new(MockBehavior.Strict);
    private readonly Mock<IConnectionProvider> _connections = new(MockBehavior.Strict);
    private readonly Mock<IGenericConnection> _live = new();
    private readonly IReadOnlyList<IDdlCommand> _commands = [new CreateSchemaCommand { Name = "cfg" }];

    private SchemaDdlService Service() => new(_connections.Object, _configurations.Object, NullLogger<SchemaDdlService>.Instance);

    private void Configured(string name, string implementation)
    {
        var configuration = new Mock<IConnectionImplementationConfiguration>();
        configuration.Setup(c => c.Name).Returns(name);
        configuration.Setup(c => c.Implementation).Returns(implementation);
        _configurations
            .Setup(p => p.Get(name, ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IConnectionImplementationConfiguration>.Success(configuration.Object));
    }

    private static DdlProbeConnectionType Probe()
    {
        var probe = (DdlProbeConnectionType)ConnectionTypes.ByName(DdlProbeConnectionType.OptionName);
        probe.Reset();
        return probe;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AConnectionThatIsNotConfiguredIsRefusedAndNothingRuns()
    {
        _configurations
            .Setup(p => p.Get("Missing", ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IConnectionImplementationConfiguration>.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired")));

        var result = await Service().Execute("Missing", _commands, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _connections.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AConnectionTypeThatIsNotRegisteredIsRefusedAndNothingRuns()
    {
        Configured("Main", "NoSuchEngine");

        var result = await Service().Execute("Main", _commands, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _connections.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ATypeThatOffersNoDdlExecutionIsRefusedWithTheCodeBeforeTheConnectionIsAskedFor()
    {
        Configured("Main", MockConnectionType.OptionName);

        var result = await Service().Execute("Main", _commands, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.FailureReason().Code.ShouldBe(ConnectionsResultCodes.ByName("DdlExecutionNotOffered").Code);
        _connections.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ATypeThatExecutesIsHandedTheLiveConnectionTheCommandsAndTheToken()
    {
        var probe = Probe();
        probe.Answer = GenericResult.Success();
        Configured("Main", DdlProbeConnectionType.OptionName);
        _connections
            .Setup(p => p.Get("Main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IGenericConnection>.Success(_live.Object));
        using var cancellation = new CancellationTokenSource();

        var result = await Service().Execute("Main", _commands, cancellation.Token);

        result.IsSuccess.ShouldBeTrue();
        probe.Calls.ShouldBe(1);
        probe.ReceivedConnection.ShouldBeSameAs(_live.Object);
        probe.ReceivedCommands.ShouldBeSameAs(_commands);
        probe.ReceivedToken.ShouldBe(cancellation.Token);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheFailureOfTheExecutionIsTheFailureAnswered()
    {
        var probe = Probe();
        probe.Answer = GenericResult.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired"));
        Configured("Main", DdlProbeConnectionType.OptionName);
        _connections
            .Setup(p => p.Get("Main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IGenericConnection>.Success(_live.Object));

        var result = await Service().Execute("Main", _commands, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.FailureReason().Code.ShouldBe(DataServiceResultCodes.ByName("DataStoreNameRequired").Code);
    }
}
