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
/// The data set provider keeps the descriptions table in step with the <c>Description</c> of a data set and of each of its fields, in the unit of the write
/// (FDW-6121). The writer is a stand-in, so what is asserted is what the provider asks it to write; the unit is only passed through.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class DataSetDescriptionsTests
{
    private const string Language = "en-US";
    private static readonly Guid DataSetId = Guid.Parse("00000000-0000-0000-0000-0000000000d1");
    private static readonly Guid FieldA = Guid.Parse("00000000-0000-0000-0000-0000000000f1");
    private static readonly Guid FieldB = Guid.Parse("00000000-0000-0000-0000-0000000000f2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private DataSetConfigurationProvider Provider()
    {
        var gateways = new Mock<IConfigurationGatewayProvider>();
        return new DataSetConfigurationProvider(
            NullLogger<DataSetConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration", "data", "DataSet", new UnavailablePlacement(), _writer.Object, _reader.Object);
    }

    private static SimpleDataSetConfiguration DataSet(string description, params (Guid Id, string Name, string? Description)[] fields)
        => new()
        {
            Id = DataSetId,
            Name = "Orders",
            Description = description,
            Fields = fields.Select(f => new DataSetFieldConfiguration { Id = f.Id, Name = f.Name, Description = f.Description }).ToList(),
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

    // The unit is passed through to the writer and never touched here, so a stand-in needs none.
    private Task<IGenericResult> Written(SimpleDataSetConfiguration dataSet)
        => Provider().Written(dataSet, null!, TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASavedDataSetWritesItsDescriptionAndThoseOfItsFieldsWithTheirOwner()
    {
        LanguageIs(Language);
        WritesSucceed();

        var written = await Written(DataSet("Orders placed", (FieldA, "Total", "The order total"), (FieldB, "Placed", "When it was placed")));

        written.IsSuccess.ShouldBeTrue();
        _writer.Verify(w => w.Save(
            It.Is<DescriptionKey>(k => k.Type == "DataSet" && k.SubType == "Text" && k.ThingId == DataSetId && k.Language == Language),
            "Orders", null, "Orders placed", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Once);
        // The fields are handed over together, with the data set as their owner (FDW-6589): the writer writes only what changed.
        _writer.Verify(w => w.DescribeOwned(
            "DataSetField", "Text", It.Is<DescriptionOwner>(o => o.Id == DataSetId && o.Name == "Orders"),
            It.Is<IReadOnlyList<OwnedDescription>>(things => things.SequenceEqual(new[]
            {
                new OwnedDescription(FieldA, "Total", "The order total"),
                new OwnedDescription(FieldB, "Placed", "When it was placed"),
            })),
            It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheFieldsTheSavedListNoLongerHoldLoseTheirDescriptions()
    {
        LanguageIs(Language);
        WritesSucceed();

        await Written(DataSet("Orders placed", (FieldA, "Total", "The order total"), (FieldB, "Placed", null)));

        // Every field the saved list holds is handed over, the one with no text too: the writer removes its description, and those of fields the list no longer
        // holds, in the language this save keeps in step.
        _writer.Verify(w => w.DescribeOwned(
            "DataSetField", "Text", It.Is<DescriptionOwner>(o => o.Id == DataSetId),
            It.Is<IReadOnlyList<OwnedDescription>>(things => things.SequenceEqual(new[]
            {
                new OwnedDescription(FieldA, "Total", "The order total"),
                new OwnedDescription(FieldB, "Placed", null),
            })),
            It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AClearedDescriptionWritesNothingAndRemovesTheRowTheDataSetHad(string cleared)
    {
        LanguageIs(Language);
        WritesSucceed();

        var written = await Written(DataSet(cleared));

        written.IsSuccess.ShouldBeTrue();
        _writer.Verify(w => w.Delete(It.Is<DescriptionKey>(k => k.Type == "DataSet" && k.ThingId == DataSetId), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Once);
        _writer.Verify(w => w.Save(
            It.IsAny<DescriptionKey>(), It.IsAny<string>(), It.IsAny<DescriptionOwner?>(), It.IsAny<string>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASaveFailsWhenNobodyStatedTheLanguageAndWritesNothing()
    {
        LanguageIs(null);

        var written = await Written(DataSet("Orders placed"));

        written.IsFailure.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailingDescriptionWriteFailsTheSaveSoItsUnitRollsBack()
    {
        LanguageIs(Language);
        _writer
            .Setup(w => w.Save(It.IsAny<DescriptionKey>(), It.IsAny<string>(), It.IsAny<DescriptionOwner?>(), It.IsAny<string>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("description refused")));

        var written = await Written(DataSet("Orders placed"));

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("description refused");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingADataSetRemovesItsDescriptionsAndThoseOfItsFieldsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("DataSet", DataSetId, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        // The aggregates, joins and samples a data set owns go with it too (FDW-6208); DataSetChildDescriptionsTests holds their own cases.
        foreach (var child in new[] { "DataSetField", "DataSetAggregate", "DataSetJoin", "DataSetSample" })
        {
            _writer.Setup(w => w.DeleteOwnedBy(child, DataSetId, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        }

        var removed = await Provider().RemoveReferences(DataSetId, null!, TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheChildFillSoTheFieldListsChildReadIsFilled()
    {
        // FDW-6777: GET /datasets/{name}/fields reads the shared field table as a child read, which reaches no FillSet.
        Provider().ShouldBeAssignableTo<IConfigurationChildReadObserver<IDataSetImplementationConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AChildReadOfFieldsFillsEachFieldsDescriptionByItsIdFromTheDescriptionsTheDataSetOwns()
    {
        var owner = new DescriptionOwner(DataSetId, "Orders");
        IReadOnlyDictionary<Guid, DescriptionEntry> held = new Dictionary<Guid, DescriptionEntry>
        {
            [FieldA] = new(new DescriptionKey("DataSetField", "Text", FieldA, Language), "OrderId", owner, "The order's number"),
        };
        _reader
            .Setup(r => r.ReadOwnedBy("DataSetField", "Text", It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { DataSetId })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(held));
        var fields = new List<DataSetFieldConfiguration> { new() { Id = FieldA, Name = "OrderId" }, new() { Id = FieldB, Name = "Placed" } };

        var filled = await Provider().ReadChildren(DataSetId, fields, TestContext.Current.CancellationToken);

        filled.IsSuccess.ShouldBeTrue(filled.CurrentMessage);
        fields[0].Description.ShouldBe("The order's number");
        fields[1].Description.ShouldBeNull();
        _reader.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedReadOfTheFieldDescriptionsFailsTheChildReadAndChildrenOfAnotherTypeAreNotRead()
    {
        _reader
            .Setup(r => r.ReadOwnedBy("DataSetField", "Text", It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var failed = await Provider().ReadChildren(DataSetId, new List<DataSetFieldConfiguration> { new() { Id = FieldA, Name = "OrderId" } }, TestContext.Current.CancellationToken);
        var other = await Provider().ReadChildren(DataSetId, new List<DataSetSourceConfiguration> { new() }, TestContext.Current.CancellationToken);

        failed.IsFailure.ShouldBeTrue();
        failed.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
        other.IsSuccess.ShouldBeTrue(other.CurrentMessage);
        _reader.Verify(r => r.ReadOwnedBy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
