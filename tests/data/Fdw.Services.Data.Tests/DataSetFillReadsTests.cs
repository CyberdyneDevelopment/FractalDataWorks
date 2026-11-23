using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Messages;
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
/// The fill of a data set read reads the texts of the fields only for the data sets whose records hold fields (FDW-6124): a record-only read, as a list page makes,
/// carries none, and the second read would fetch rows that nothing receives. The reader is strict, so a read the test did not expect fails it.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class DataSetFillReadsTests
{
    private const string Language = "en-US";
    private static readonly Guid WithFields = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid RecordOnly = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
    private static readonly Guid Field = Guid.Parse("00000000-0000-0000-0000-0000000000f1");

    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private DataSetConfigurationProvider Provider()
        => new(
            NullLogger<DataSetConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>().Object, "PlatformConfiguration", "data", "DataSet",
            new UnavailablePlacement(), new Mock<IDescriptionWriter>(MockBehavior.Strict).Object, _reader.Object);

    private static SimpleDataSetConfiguration DataSet(Guid id, params Guid[] fields)
        => new()
        {
            Id = id,
            Name = "Orders",
            Implementation = SimpleDataSetConfiguration.ImplementationName,
            Fields = fields.Select(fieldId => new DataSetFieldConfiguration { Id = fieldId, Name = "Total" }).ToList(),
        };

    private static DescriptionEntry Entry(string type, Guid id, string text)
        => new(new DescriptionKey(type, DescriptionTextSubType.OptionName, id, Language), "Name", null, text);

    private void OwnTexts(params (Guid Id, string Text)[] texts)
        => _reader
            .Setup(r => r.ReadMany("DataSet", DescriptionTextSubType.OptionName, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(texts.ToDictionary(text => text.Id, text => Entry("DataSet", text.Id, text.Text))));

    private void FieldTexts(params (Guid Id, string Text)[] texts)
        => _reader
            .Setup(r => r.ReadOwnedBy("DataSetField", DescriptionTextSubType.OptionName, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(texts.ToDictionary(text => text.Id, text => Entry("DataSetField", text.Id, text.Text))));

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARecordOnlyReadFillsTheOwnTextAndReadsNoFieldTexts()
    {
        OwnTexts((RecordOnly, "Orders placed"));
        var records = new List<IDataSetImplementationConfiguration> { DataSet(RecordOnly) };

        var filled = await Provider().Read(records, null, TestContext.Current.CancellationToken);

        filled.IsSuccess.ShouldBeTrue(filled.CurrentMessage);
        records[0].Description.ShouldBe("Orders placed");
        _reader.Verify(r => r.ReadOwnedBy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _reader.Verify(r => r.ReadMany("DataSet", DescriptionTextSubType.OptionName, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AReadOfRecordsThatHoldFieldsReadsTheirOwnTextsAndTheirFieldTextsOnceEach()
    {
        OwnTexts((WithFields, "Orders placed"));
        FieldTexts((Field, "The order total"));
        var records = new List<IDataSetImplementationConfiguration> { DataSet(WithFields, Field) };

        var filled = await Provider().Read(records, null, TestContext.Current.CancellationToken);

        filled.IsSuccess.ShouldBeTrue(filled.CurrentMessage);
        records[0].Description.ShouldBe("Orders placed");
        records[0].Fields.Single().Description.ShouldBe("The order total");
        _reader.Verify(r => r.ReadMany("DataSet", DescriptionTextSubType.OptionName, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
        _reader.Verify(r => r.ReadOwnedBy("DataSetField", DescriptionTextSubType.OptionName, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AMixedPageReadsFieldTextsForTheDataSetsThatHoldFieldsOnly()
    {
        OwnTexts((WithFields, "Orders placed"), (RecordOnly, "Customers"));
        FieldTexts((Field, "The order total"));
        var records = new List<IDataSetImplementationConfiguration> { DataSet(WithFields, Field), DataSet(RecordOnly) };

        var filled = await Provider().Read(records, null, TestContext.Current.CancellationToken);

        filled.IsSuccess.ShouldBeTrue(filled.CurrentMessage);
        records.Select(record => record.Description).ShouldBe(["Orders placed", "Customers"]);
        _reader.Verify(
            r => r.ReadOwnedBy("DataSetField", DescriptionTextSubType.OptionName, It.Is<IReadOnlyCollection<Guid>>(owners => owners.SequenceEqual(new[] { WithFields })), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedReadOfTheOwnTextsIsTheAnswerAndNoFieldTextIsRead()
    {
        _reader
            .Setup(r => r.ReadMany(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Failure(new GenericMessage(MessageSeverity.Error, "no language stated", "TST-61401")));

        var filled = await Provider().Read([DataSet(WithFields, Field)], null, TestContext.Current.CancellationToken);

        filled.IsFailure.ShouldBeTrue();
        _reader.Verify(r => r.ReadOwnedBy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
