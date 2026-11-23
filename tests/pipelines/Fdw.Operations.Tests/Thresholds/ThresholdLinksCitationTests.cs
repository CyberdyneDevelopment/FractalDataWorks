using System.Collections.Generic;
using System.Threading;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Operations;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Operations.Configuration;
using Fdw.Operations.Thresholds;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Tests.Thresholds;

/// <summary>
/// FDW-6221: a threshold is attached only to a subject that exists. The data set's key to its subject is gone from the database (a filtered
/// unique index cannot be referenced), so the link asks the citation check, by the container the subject kind declares, before it writes.
/// </summary>
public sealed class ThresholdLinksCitationTests
{
    private const string Store = "PlatformConfiguration";
    private static readonly Guid Subject = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
    private static readonly Guid ThresholdId = Guid.Parse("00000000-0000-0000-0000-0000000000c1");

    private readonly Mock<ICitedConfigurations> _cited = new();
    private readonly Mock<IConfigurationGateway> _gateway = new();
    private readonly List<IDataCommand> _written = [];

    private ThresholdLinks Links()
    {
        var thresholds = new Mock<IThresholdConfigurationProvider>();
        var slow = new ThresholdImplementationConfiguration { Id = ThresholdId, Name = "Slow", SubjectKind = "DataSet" };
        thresholds
            .Setup(t => t.Get("Slow", ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IThresholdImplementationConfiguration>.Success(slow));
        thresholds
            .Setup(t => t.Get(ThresholdId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IThresholdImplementationConfiguration>.Success(slow));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get(Store)).Returns(GenericResult<IConfigurationGateway>.Success(_gateway.Object));
        _gateway
            .Setup(g => g.Execute<IEnumerable<ThresholdLinkConfiguration>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ThresholdLinkConfiguration>>.Success([]));
        _gateway
            .Setup(g => g.Execute<ConfigurationWriteResult>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((command, _, _) => _written.Add(command))
            .ReturnsAsync(GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], null)));
        _cited
            .Setup(c => c.StateOf(It.IsAny<Citation>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICitedState>.Success(CitedStates.Current));
        return new ThresholdLinks(gateways.Object, thresholds.Object, _cited.Object, Store, NullLogger<ThresholdLinks>.Instance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AttachWritesTheLinkWhenTheSubjectExists()
    {
        var attached = await Links().Attach("DataSet", Subject, "Slow", TestContext.Current.CancellationToken);

        attached.IsSuccess.ShouldBeTrue();
        _written.ShouldHaveSingleItem();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AttachRefusesASubjectThatDoesNotExistAndWritesNothing()
    {
        var links = Links();
        _cited
            .Setup(c => c.StateOf(It.IsAny<Citation>(), Subject, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICitedState>.Success(CitedStates.Absent));

        var attached = await links.Attach("DataSet", Subject, "Slow", TestContext.Current.CancellationToken);

        attached.IsFailure.ShouldBeTrue();
        attached.Code!.Name.ShouldBe("ThresholdSubjectNotFound");
        _written.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AttachRefusesASubjectThatWasRetiredForItsOwnReasonAndWritesNothing()
    {
        var links = Links();
        _cited
            .Setup(c => c.StateOf(It.IsAny<Citation>(), Subject, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICitedState>.Success(CitedStates.Retired));

        var attached = await links.Attach("DataSet", Subject, "Slow", TestContext.Current.CancellationToken);

        attached.IsFailure.ShouldBeTrue();
        attached.Code!.Name.ShouldBe("ThresholdSubjectRetired");
        _written.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ThresholdsAlreadyOnARetiredSubjectStillReadAndComeOffWithoutAskingWhetherItExists()
    {
        var links = Links();
        _cited
            .Setup(c => c.StateOf(It.IsAny<Citation>(), Subject, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICitedState>.Success(CitedStates.Retired));
        _gateway
            .Setup(g => g.Execute<IEnumerable<ThresholdLinkConfiguration>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ThresholdLinkConfiguration>>.Success(
                [new ThresholdLinkConfiguration { Id = Guid.NewGuid(), SubjectId = Subject, ThresholdId = ThresholdId }]));

        var read = await links.ThresholdsOf("DataSet", Subject, TestContext.Current.CancellationToken);
        var detached = await links.Detach("DataSet", Subject, "Slow", TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        detached.IsSuccess.ShouldBeTrue();
        _cited.Verify(c => c.StateOf(It.IsAny<Citation>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AttachAsksForTheSubjectTheKindDeclaresItCites()
    {
        await Links().Attach("DataSet", Subject, "Slow", TestContext.Current.CancellationToken);

        _cited.Verify(c => c.StateOf(It.Is<Citation>(citation => citation.Container == "data.DataSet"), Subject, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AttachFailsWhenTheCitationCannotBeChecked()
    {
        var links = Links();
        _cited
            .Setup(c => c.StateOf(It.IsAny<Citation>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICitedState>.Failure(new GenericMessage("gateway exploded")));

        var attached = await links.Attach("DataSet", Subject, "Slow", TestContext.Current.CancellationToken);

        attached.IsFailure.ShouldBeTrue();
        _written.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EveryKindStatesWhatItCitesAndOnlyTheDataSetIsCheckedHere()
    {
        ThresholdSubjectKinds.All().ShouldAllBe(kind => kind.Cites != null);
        ThresholdSubjectKinds.All().Where(kind => kind.Cites.Container is not null).Select(kind => kind.Name).ToList().ShouldBe(["DataSet"]);
        ThresholdSubjectKinds.ByName("DataSet").Cites.Container.ShouldBe("data.DataSet");
    }
}
