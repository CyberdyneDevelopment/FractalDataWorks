using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// FDW-6221: a domain's provider says whether a member with a durable id is current, retired or not there, from the domain row alone. A record that
/// cites another domain's member (a threshold, a tag) asks it before it writes, now that no foreign key does, and refuses a retired member for its own
/// reason, not as a member that was never there.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataIntegrity")]
public sealed class DomainConfigurationExistsTests
{
    private static Mock<IConfigurationGateway> GatewayAnswering(params IEnumerable<DataSetDomainConfiguration>[] answers)
    {
        var gateway = new Mock<IConfigurationGateway>();
        var sequence = gateway.SetupSequence(g => g.Execute<IEnumerable<DataSetDomainConfiguration>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()));
        foreach (var answer in answers)
            sequence = sequence.ReturnsAsync(GenericResult<IEnumerable<DataSetDomainConfiguration>>.Success(answer));
        return gateway;
    }

    private static DataSetConfigurationProvider Provider(Mock<IConfigurationGateway> gateway)
    {
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        return new DataSetConfigurationProvider(
            NullLogger<DataSetConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration", "data", "DataSet",
            new Fdw.Services.Configuration.UnavailablePlacement(), new Mock<Fdw.Services.Descriptions.IDescriptionWriter>(MockBehavior.Strict).Object,
            new Mock<Fdw.Services.Descriptions.Abstractions.IDescriptionReader>(MockBehavior.Strict).Object);
    }

    private static DataSetDomainConfiguration Row(Guid id) => new() { Id = id, Name = "Orders", Domain = "DataSet", Implementation = "Simple" };

    [Fact]
    public async Task AMemberTheDomainHoldsIsCurrentAndTakesOneQuery()
    {
        var id = Guid.NewGuid();
        var gateway = GatewayAnswering([Row(id)]);

        var state = await Provider(gateway).StateOf(id, TestContext.Current.CancellationToken);

        state.IsSuccess.ShouldBeTrue();
        state.Value.ShouldBe(CitedStates.Current);
        gateway.Verify(g => g.Execute<IEnumerable<DataSetDomainConfiguration>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AMemberOnlyAnOlderVersionOfWhichIsHeldIsRetired()
    {
        var id = Guid.NewGuid();

        var state = await Provider(GatewayAnswering([], [Row(id)])).StateOf(id, TestContext.Current.CancellationToken);

        state.IsSuccess.ShouldBeTrue();
        state.Value.ShouldBe(CitedStates.Retired);
    }

    [Fact]
    public async Task AMemberNoVersionOfWhichIsHeldIsAbsentAndThatIsNotAFailure()
    {
        var state = await Provider(GatewayAnswering([], [])).StateOf(Guid.NewGuid(), TestContext.Current.CancellationToken);

        state.IsSuccess.ShouldBeTrue();
        state.Value.ShouldBe(CitedStates.Absent);
    }

    [Fact]
    public async Task StateOfReadsTheDomainTableAndNeverTheImplementation()
    {
        var id = Guid.NewGuid();
        var store = ConfigurationStore.DataSets(new SimpleDataSetConfiguration { Id = id, Name = "Orders" });

        await ((IDomainConfigurationProvider)store.Provider).StateOf(id, TestContext.Current.CancellationToken);

        store.Implementations.Verify(i => i.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        store.Implementations.Verify(i => i.Get(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AReadThatFailsIsAFailureNotAnAnswerThatTheMemberIsAbsent()
    {
        var store = ConfigurationStore.DataSets();
        store.CannotBeRead(new GenericMessage("gateway exploded"));

        var state = await ((IDomainConfigurationProvider)store.Provider).StateOf(Guid.NewGuid(), TestContext.Current.CancellationToken);

        state.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void TheProviderNamesItsContainerByPathAndTable()
    {
        var store = ConfigurationStore.DataSets();

        ((IDomainConfigurationProvider)store.Provider).Container.ShouldBe("data.DataSet");
    }
}
