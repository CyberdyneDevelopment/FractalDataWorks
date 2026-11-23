using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>FDW-6208: a variation's description is the one of its data set, read from the descriptions table for the whole page; the view does not carry it.</summary>
public sealed class DataSetVariationListDescriptionsTests
{
    private static readonly Guid First = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid Second = Guid.Parse("00000000-0000-0000-0000-0000000000a2");

    private static DataSetVariationListProvider Observer(Mock<IDescriptionReader> reader)
        => new DataSetVariationListProvider(
            NullLogger<DataSetVariationListProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "data", "DataSetVariationList", reader.Object);

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APageIsFilledFromOneReadOfTheDescriptionsOfItsDataSetsAndAVariationWithoutOneKeepsNone()
    {
        var reader = new Mock<IDescriptionReader>(MockBehavior.Strict);
        reader
            .Setup(r => r.ReadMany("DataSet", DescriptionTextSubType.OptionName, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(new Dictionary<Guid, DescriptionEntry>
            {
                [First] = new(new DescriptionKey("DataSet", DescriptionTextSubType.OptionName, First, "en-US"), "Orders", null, "Orders by month"),
            }));
        var records = new List<DataSetVariationDomainConfiguration> { new() { Id = First }, new() { Id = Second } };

        var filled = await Observer(reader).Read(records, TestContext.Current.CancellationToken);

        filled.IsSuccess.ShouldBeTrue(filled.CurrentMessage);
        records.Select(record => record.Description).ToList().ShouldBe(["Orders by month", null]);
        reader.Verify(r => r.ReadMany("DataSet", DescriptionTextSubType.OptionName, It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedReadOfTheDescriptionsIsTheAnswerOfTheList()
    {
        var reader = new Mock<IDescriptionReader>(MockBehavior.Strict);
        reader
            .Setup(r => r.ReadMany(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Failure(new GenericMessage(MessageSeverity.Error, "no language stated", "TST-61400")));

        var filled = await Observer(reader).Read([new DataSetVariationDomainConfiguration { Id = First }], TestContext.Current.CancellationToken);

        filled.IsFailure.ShouldBeTrue();
    }
}
