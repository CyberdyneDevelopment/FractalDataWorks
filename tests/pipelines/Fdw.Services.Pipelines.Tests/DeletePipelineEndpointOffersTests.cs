using Fdw.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Thresholds;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// FDW-6135: deleting a pipeline retires its offers, after its thresholds are released and before the pipeline itself goes; an offer that cannot be retired stops the delete, so a pipeline is never left
/// gone with offers still standing. The fakes state their answers: the configuration gateway holds no wait threshold level, and every call the delete is not expected to make is a strict mock's failure.
/// </summary>
public sealed class DeletePipelineEndpointOffersTests
{
    private static readonly Guid PipelineId = Guid.Parse("44444444-0000-0000-0000-000000000004");

    private sealed class Probe(
        IPipelineConfigurationProvider provider, IConfigurationGatewayProvider gateways, IPipelineOfferService offers)
        : DeletePipelineEndpointBase<IPipelineImplementationConfiguration>(NullLogger<DeletePipelineEndpointBase<IPipelineImplementationConfiguration>>.Instance, provider, gateways, offers)
    {
        public Task<IGenericResult> Run(string name, CancellationToken ct) => Delete(new PipelineNameRequest { Name = name }, ct);
    }

    private readonly Mock<IPipelineConfigurationProvider> _provider = new(MockBehavior.Strict);
    private readonly Mock<IPipelineOfferService> _offers = new(MockBehavior.Strict);
    private readonly List<string> _order = [];

    private Probe Sut()
    {
        var existing = new Mock<IPipelineImplementationConfiguration>();
        existing.SetupGet(e => e.Id).Returns(PipelineId);
        _provider.Setup(p => p.Get("Consolidate",ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Success(existing.Object));
        _provider.Setup(p => p.Delete(PipelineId, It.IsAny<CancellationToken>())).Returns(() =>
        {
            _order.Add("delete");
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        });
        var gateway = new Mock<IConfigurationGateway>(MockBehavior.Strict);
        gateway.Setup(g => g.Execute<IEnumerable<WaitThresholdLevelRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<WaitThresholdLevelRecord>>.Success([]));
        var gateways = new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict);
        gateways.Setup(g => g.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        return new Probe(_provider.Object, gateways.Object, _offers.Object);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task DeletingAPipelineRetiresItsOffersBeforeThePipelineGoes()
    {
        _offers.Setup(o => o.RemoveAll(PipelineId, It.IsAny<CancellationToken>())).Returns(() =>
        {
            _order.Add("offers");
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        });

        var deleted = await Sut().Run("Consolidate", TestContext.Current.CancellationToken);

        deleted.IsSuccess.ShouldBeTrue(deleted.CurrentMessage);
        _order.ShouldBe(["offers", "delete"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task OffersThatCannotBeRetiredStopTheDeleteAndThePipelineStays()
    {
        _offers.Setup(o => o.RemoveAll(PipelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new GenericMessage("the store is down")));

        var deleted = await Sut().Run("Consolidate", TestContext.Current.CancellationToken);

        deleted.IsFailure.ShouldBeTrue();
        _order.ShouldBeEmpty();
    }
}
