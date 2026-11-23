using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// The aggregates, joins and samples of a data set keep their description in the descriptions table, owned by the data set (FDW-6208): the provider writes them in the
/// unit of the data set's save, fills them on every read that composes them, and removes them with the data set. The writer and reader are strict stand-ins.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class DataSetChildDescriptionsTests
{
    private const string Language = "en-US";
    private static readonly Guid DataSetId = Guid.Parse("00000000-0000-0000-0000-0000000000d1");
    private static readonly Guid AggregateA = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid AggregateB = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
    private static readonly Guid JoinA = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
    private static readonly Guid SampleNew = Guid.Parse("00000000-0000-0000-0000-0000000000c1");
    private static readonly Guid SampleOld = Guid.Parse("00000000-0000-0000-0000-0000000000c2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private DataSetConfigurationProvider Provider()
    {
        var gateways = new Mock<IConfigurationGatewayProvider>();
        return new DataSetConfigurationProvider(
            NullLogger<DataSetConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration", "data", "DataSet", new UnavailablePlacement(), _writer.Object, _reader.Object);
    }

    private static SimpleDataSetConfiguration Simple(params (Guid Id, string Column, string? Description)[] aggregates)
        => new()
        {
            Id = DataSetId,
            Name = "Orders",
            Description = "Orders placed",
            Aggregates = aggregates.Select(a => new DataSetAggregateConfiguration { Id = a.Id, AggregateColumnName = a.Column, Description = a.Description }).ToList(),
        };

    private static CompoundDataSetConfiguration Compound(string? joinDescription)
        => new()
        {
            Id = DataSetId,
            Name = "Orders",
            Description = "Orders placed",
            Joins = [new DataSetJoinConfiguration { Id = JoinA, LeftSourceName = "Orders", LeftFieldName = "CustomerId", RightSourceName = "Customers", RightFieldName = "Id", JoinType = "Inner", Description = joinDescription }],
        };

    private void LanguageIs(string? language)
        => _writer.Setup(w => w.Language()).Returns(language is null
            ? GenericResult<string>.Failure(new Fdw.Messages.GenericMessage("language not stated"))
            : GenericResult<string>.Success(language));

    private void WritesSucceed()
    {
        _writer
            .Setup(w => w.Save(It.IsAny<DescriptionKey>(), It.IsAny<string>(), It.IsAny<DescriptionOwner?>(), It.IsAny<string>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        _writer
            .Setup(w => w.Delete(It.IsAny<DescriptionKey>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        _writer
            .Setup(w => w.DescribeOwned(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DescriptionOwner>(), It.IsAny<IReadOnlyList<OwnedDescription>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
    }

    private void ReaderHolds(string type, params (Guid Id, string Text)[] texts)
    {
        IReadOnlyDictionary<Guid, DescriptionEntry> held = texts.ToDictionary(
            t => t.Id, t => new DescriptionEntry(new DescriptionKey(type, "Text", t.Id, Language), "thing", new DescriptionOwner(DataSetId, "Orders"), t.Text));
        _reader
            .Setup(r => r.ReadOwnedBy(type, "Text", It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { DataSetId })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(held));
    }

    // The unit is passed through to the writer and never touched here, so a stand-in needs none.
    private Task<IGenericResult> Written(IDataSetImplementationConfiguration dataSet)
        => Provider().Written(dataSet, null!, TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASavedDataSetHandsItsAggregatesOverWithTheDataSetAsTheirOwner()
    {
        LanguageIs(Language);
        WritesSucceed();

        var written = await Written(Simple((AggregateA, "TotalSales", "Sales summed by state"), (AggregateB, "Orders", null)));

        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
        _writer.Verify(w => w.DescribeOwned(
            "DataSetAggregate", "Text", It.Is<DescriptionOwner>(o => o.Id == DataSetId && o.Name == "Orders"),
            It.Is<IReadOnlyList<OwnedDescription>>(things => things.SequenceEqual(new[]
            {
                new OwnedDescription(AggregateA, "TotalSales", "Sales summed by state"),
                new OwnedDescription(AggregateB, "Orders", null),
            })),
            It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASavedDataSetHandsItsJoinsOverNamedByWhatTheyMatchOn()
    {
        LanguageIs(Language);
        WritesSucceed();

        var written = await Written(Compound("Customers of an order"));

        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
        _writer.Verify(w => w.DescribeOwned(
            "DataSetJoin", "Text", It.Is<DescriptionOwner>(o => o.Id == DataSetId),
            It.Is<IReadOnlyList<OwnedDescription>>(things => things.SequenceEqual(new[]
            {
                new OwnedDescription(JoinA, "Orders.CustomerId Inner Customers.Id", "Customers of an order"),
            })),
            It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADataSetWithNoAggregatesOrJoinsHandsOverNoneSoWhatAnEarlierKindLeftIsRemoved()
    {
        LanguageIs(Language);
        WritesSucceed();

        await Written(Simple());

        _writer.Verify(w => w.DescribeOwned(
            "DataSetAggregate", "Text", It.IsAny<DescriptionOwner>(), It.Is<IReadOnlyList<OwnedDescription>>(things => things.Count == 0),
            It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Once);
        _writer.Verify(w => w.DescribeOwned(
            "DataSetJoin", "Text", It.IsAny<DescriptionOwner>(), It.Is<IReadOnlyList<OwnedDescription>>(things => things.Count == 0),
            It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedAggregateDescriptionFailsTheSaveAndTheJoinsAreNotWritten()
    {
        LanguageIs(Language);
        WritesSucceed();
        _writer
            .Setup(w => w.DescribeOwned(
                "DataSetAggregate", It.IsAny<string>(), It.IsAny<DescriptionOwner>(), It.IsAny<IReadOnlyList<OwnedDescription>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("aggregate description refused")));

        var written = await Written(Simple((AggregateA, "TotalSales", "Sales summed by state")));

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("aggregate description refused");
        _writer.Verify(w => w.DescribeOwned(
            "DataSetJoin", It.IsAny<string>(), It.IsAny<DescriptionOwner>(), It.IsAny<IReadOnlyList<OwnedDescription>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingADataSetRemovesTheDescriptionsOfItsAggregatesJoinsAndSamplesToo()
    {
        _writer.Setup(w => w.DeleteAbout("DataSet", DataSetId, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        foreach (var type in new[] { "DataSetField", "DataSetAggregate", "DataSetJoin", "DataSetSample" })
        {
            _writer.Setup(w => w.DeleteOwnedBy(type, DataSetId, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        }

        var removed = await Provider().RemoveReferences(DataSetId, null!, TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue(removed.CurrentMessage);
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AReadOfTheSetFillsEachAggregateFromOneReadOfTheDescriptionsTheDataSetsOwn()
    {
        IReadOnlyDictionary<Guid, DescriptionEntry> none = new Dictionary<Guid, DescriptionEntry>();
        _reader
            .Setup(r => r.ReadMany("DataSet", "Text", It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(none));
        ReaderHolds("DataSetAggregate", (AggregateA, "Sales summed by state"));
        var dataSet = Simple((AggregateA, "TotalSales", null), (AggregateB, "Orders", null));

        var filled = await Provider().Read([dataSet], null, TestContext.Current.CancellationToken);

        filled.IsSuccess.ShouldBeTrue(filled.CurrentMessage);
        dataSet.Aggregates[0].Description.ShouldBe("Sales summed by state");
        dataSet.Aggregates[1].Description.ShouldBeNull();
        // A data set that holds no field and no join costs no read of either.
        _reader.Verify(r => r.ReadOwnedBy("DataSetField", It.IsAny<string>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _reader.Verify(r => r.ReadOwnedBy("DataSetJoin", It.IsAny<string>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AChildReadOfJoinsFillsEachJoinsDescriptionByItsId()
    {
        ReaderHolds("DataSetJoin", (JoinA, "Customers of an order"));
        var joins = new List<DataSetJoinConfiguration> { new() { Id = JoinA }, new() { Id = Guid.NewGuid() } };

        var filled = await Provider().ReadChildren(DataSetId, joins, TestContext.Current.CancellationToken);

        filled.IsSuccess.ShouldBeTrue(filled.CurrentMessage);
        joins[0].Description.ShouldBe("Customers of an order");
        joins[1].Description.ShouldBeNull();
        _reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedReadOfTheAggregateDescriptionsFailsTheSetRead()
    {
        IReadOnlyDictionary<Guid, DescriptionEntry> none = new Dictionary<Guid, DescriptionEntry>();
        _reader
            .Setup(r => r.ReadMany("DataSet", "Text", It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(none));
        _reader
            .Setup(r => r.ReadOwnedBy("DataSetAggregate", "Text", It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var failed = await Provider().Read([Simple((AggregateA, "TotalSales", null))], null, TestContext.Current.CancellationToken);

        failed.IsFailure.ShouldBeTrue();
        failed.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ANewSampleReplacingAnotherRemovesTheOldOnesDescriptionAndSavesTheNewWithTheDataSetAsOwner()
    {
        WritesSucceed();

        var described = await Provider().DescribeSampleIn(DataSetId, "Orders", SampleNew, "happy-path", "The rows people mean", SampleOld, Language, null!, TestContext.Current.CancellationToken);

        described.IsSuccess.ShouldBeTrue(described.CurrentMessage);
        _writer.Verify(w => w.Delete(
            It.Is<DescriptionKey>(k => k.Type == "DataSetSample" && k.SubType == "Text" && k.ThingId == SampleOld && k.Language == Language), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Once);
        _writer.Verify(w => w.Save(
            It.Is<DescriptionKey>(k => k.Type == "DataSetSample" && k.ThingId == SampleNew && k.Language == Language),
            "happy-path", It.Is<DescriptionOwner?>(o => o != null && o.Id == DataSetId && o.Name == "Orders"), "The rows people mean", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFirstSampleReplacesNoneAndASampleWithNoTextSavesNoneAndRemovesTheRowItHad()
    {
        WritesSucceed();

        var described = await Provider().DescribeSampleIn(DataSetId, "Orders", SampleNew, "happy-path", "   ", null, Language, null!, TestContext.Current.CancellationToken);

        described.IsSuccess.ShouldBeTrue(described.CurrentMessage);
        _writer.Verify(w => w.Delete(It.Is<DescriptionKey>(k => k.ThingId == SampleOld), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Never);
        _writer.Verify(w => w.Delete(It.Is<DescriptionKey>(k => k.ThingId == SampleNew), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Once);
        _writer.Verify(w => w.Save(
            It.IsAny<DescriptionKey>(), It.IsAny<string>(), It.IsAny<DescriptionOwner?>(), It.IsAny<string>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASampleWhoseOldDescriptionCannotBeRemovedFailsAndSavesNothing()
    {
        _writer
            .Setup(w => w.Delete(It.IsAny<DescriptionKey>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("removal refused")));

        var described = await Provider().DescribeSampleIn(DataSetId, "Orders", SampleNew, "happy-path", "The rows people mean", SampleOld, Language, null!, TestContext.Current.CancellationToken);

        described.IsFailure.ShouldBeTrue();
        described.CurrentMessage.ShouldNotBeNull().ShouldContain("removal refused");
        _writer.Verify(w => w.Save(
            It.IsAny<DescriptionKey>(), It.IsAny<string>(), It.IsAny<DescriptionOwner?>(), It.IsAny<string>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASampleIsNotDescribedWhenNobodyStatedTheLanguageAndNoUnitIsOpened()
    {
        LanguageIs(null);

        var described = await Provider().DescribeSample(DataSetId, "Orders", SampleNew, "happy-path", "The rows people mean", null, TestContext.Current.CancellationToken);

        described.IsFailure.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheSampleDescriptionsOfADataSetComeBackByTheSampleId()
    {
        ReaderHolds("DataSetSample", (SampleNew, "The rows people mean"));

        var read = await Provider().SampleDescriptions(DataSetId, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        var texts = read.Value.ShouldNotBeNull();
        texts.Count.ShouldBe(1);
        texts[SampleNew].ShouldBe("The rows people mean");
        _reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedReadOfTheSampleDescriptionsIsAFailureAndNotAnEmptyAnswer()
    {
        _reader
            .Setup(r => r.ReadOwnedBy("DataSetSample", "Text", It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var read = await Provider().SampleDescriptions(DataSetId, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }
}
