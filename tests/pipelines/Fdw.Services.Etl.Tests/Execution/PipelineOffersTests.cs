using Fdw.Services.Abstractions;
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
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// The offers of pipelines (FDW-6135): they are the pipeline's own configuration in <c>pipe.PipelineOffer</c>, read through the configuration gateway and written and retired through the
/// configuration save and retire path. A pipeline is offered when an offer of the whole tenant, of the person or of one of the person's roles stands; none stands, none is assumed.
/// </summary>
public sealed class PipelineOffersTests
{
    private static readonly Guid Pipeline = Guid.Parse("44444444-0000-0000-0000-000000000004");
    private static readonly Guid Person = Guid.Parse("77777777-0000-0000-0000-000000000007");
    private static readonly Guid Someone = Guid.Parse("88888888-0000-0000-0000-000000000008");

    private readonly List<PipelineOfferRecord> _rows = [];
    private readonly List<IDataCommand> _writes = [];
    private readonly Mock<IPipelineConfigurationProvider> _pipelines = new();
    private bool _readFails;

    private PipelineOffers Sut(bool pipelineExists = true)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<PipelineOfferRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _readFails
                ? GenericResult<IEnumerable<PipelineOfferRecord>>.Failure(Mock.Of<IGenericMessage>(m => m.Message == "the store is down"))
                : GenericResult<IEnumerable<PipelineOfferRecord>>.Success(_rows.Where(row => row.IsCurrent && !row.IsDeleted).ToList()));
        gateway
            .Setup(g => g.Execute<ConfigurationWriteResult>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget _, CancellationToken __) =>
            {
                _writes.Add(command);
                if (command is ConfigurationSaveCommand<PipelineOfferRecord> save && save.Data is { } row)
                {
                    _rows.Add(row);
                }
                else if (command is ConfigurationRetireCommand retire)
                {
                    foreach (var offer in _rows.Where(offer => offer.Id.Equals(retire.KeyValues[0])))
                    {
                        offer.IsDeleted = true;
                    }
                }

                return GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], new Dictionary<string, object?>()));
            });
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(p => p.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        _pipelines.Setup(p => p.Get(It.IsAny<Guid>(),ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>())).ReturnsAsync(
            pipelineExists
                ? GenericResult<IPipelineImplementationConfiguration>.Success(new GraphPipelineConfiguration { Id = Pipeline, Name = "Consolidate" })
                : GenericResult<IPipelineImplementationConfiguration>.Failure(Mock.Of<IGenericMessage>(m => m.Message == "no such pipeline")));
        return new PipelineOffers(gateways.Object, _pipelines.Object, NullLogger<PipelineOffers>.Instance);
    }

    private static PipelineOfferRecord Offer(Guid? person = null, string? role = null) => new() { PipelineId = Pipeline, PersonId = person, RoleName = role };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task WithNoOfferNothingIsOffered()
    {
        var offered = await Sut().IsOffered(Pipeline, Person, ["etl-runner"], TestContext.Current.CancellationToken);

        offered.IsSuccess.ShouldBeTrue(offered.CurrentMessage);
        offered.Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnOfferToTheWholeTenantOffersItToEveryone()
    {
        _rows.Add(Offer());

        var offered = await Sut().IsOffered(Pipeline, Someone, [], TestContext.Current.CancellationToken);

        offered.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnOfferToAPersonOffersItToThatPersonOnly()
    {
        _rows.Add(Offer(person: Person));
        var sut = Sut();

        (await sut.IsOffered(Pipeline, Person, [], TestContext.Current.CancellationToken)).Value.ShouldBeTrue();
        (await sut.IsOffered(Pipeline, Someone, [], TestContext.Current.CancellationToken)).Value.ShouldBeFalse();
        (await sut.IsOffered(Pipeline, null, [], TestContext.Current.CancellationToken)).Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnOfferToARoleOffersItToItsHoldersWhateverTheCase()
    {
        _rows.Add(Offer(role: "Etl-Runner"));
        var sut = Sut();

        (await sut.IsOffered(Pipeline, Person, ["etl-runner"], TestContext.Current.CancellationToken)).Value.ShouldBeTrue();
        (await sut.IsOffered(Pipeline, Person, ["viewer"], TestContext.Current.CancellationToken)).Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnOfferThatCannotBeReadIsAFailureAndNeverAnAnswer()
    {
        _readFails = true;

        var offered = await Sut().IsOffered(Pipeline, Person, [], TestContext.Current.CancellationToken);

        offered.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Behavior")]
    public async Task AddingStoresOneOfferThroughTheConfigurationSavePath()
    {
        var sut = Sut();

        var added = await sut.Add(Pipeline, Person, null, TestContext.Current.CancellationToken);

        added.IsSuccess.ShouldBeTrue(added.CurrentMessage);
        added.Value.ShouldNotBeNull().PersonId.ShouldBe(Person);
        _writes.ShouldHaveSingleItem().ShouldBeOfType<ConfigurationSaveCommand<PipelineOfferRecord>>();
        (await sut.List(Pipeline, TestContext.Current.CancellationToken)).Value.ShouldNotBeNull().ShouldHaveSingleItem().Id.ShouldBe(added.Value.Id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Behavior")]
    public async Task AnOfferThatNamesAPersonAndARoleIsRefusedAndNothingIsWritten()
    {
        var added = await Sut().Add(Pipeline, Person, "etl-runner", TestContext.Current.CancellationToken);

        added.IsFailure.ShouldBeTrue();
        _writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Behavior")]
    public async Task AnOfferThatAlreadyStandsIsRefusedAndNothingIsWrittenTwice()
    {
        _rows.Add(Offer(role: "etl-runner"));

        var added = await Sut().Add(Pipeline, null, "ETL-RUNNER", TestContext.Current.CancellationToken);

        added.IsFailure.ShouldBeTrue();
        _writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Behavior")]
    public async Task AnOfferForAPipelineThatDoesNotExistIsRefused()
    {
        var added = await Sut(pipelineExists: false).Add(Pipeline, Person, null, TestContext.Current.CancellationToken);

        added.IsFailure.ShouldBeTrue();
        _writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Behavior")]
    public async Task RemovingRetiresTheOfferAndAnOfferThatIsNotThereIsNotFound()
    {
        var offer = Offer(person: Person);
        _rows.Add(offer);
        var sut = Sut();

        (await sut.Remove(Pipeline, Guid.NewGuid(), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        _writes.ShouldBeEmpty();

        (await sut.Remove(Pipeline, offer.Id, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        _writes.ShouldHaveSingleItem().ShouldBeOfType<ConfigurationRetireCommand>();
        (await sut.IsOffered(Pipeline, Person, [], TestContext.Current.CancellationToken)).Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Behavior")]
    public async Task RemovingAllRetiresEveryOfferOfThePipeline()
    {
        _rows.Add(Offer(person: Person));
        _rows.Add(Offer(role: "etl-runner"));
        _rows.Add(Offer());
        var sut = Sut();

        (await sut.RemoveAll(Pipeline, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writes.Count.ShouldBe(3);
        (await sut.List(Pipeline, TestContext.Current.CancellationToken)).Value.ShouldNotBeNull().ShouldBeEmpty();
    }
}
