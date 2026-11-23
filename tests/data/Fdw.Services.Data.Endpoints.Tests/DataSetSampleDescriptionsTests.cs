using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
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

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6208: a sample's description is no longer a column of <c>data.DataSetSample</c>. <c>GET /datasets/{name}/samples</c> still answers it, read from the descriptions table by the
/// sample's Id, and <c>POST /datasets/{name}/samples</c> still answers the text it was given and writes it to the descriptions table through the data set provider.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "Api")]
public sealed class DataSetSampleDescriptionsTests
{
    private const string Store = "ConfigStore";
    private const string Path = "data";
    private static readonly Guid DataSetId = Guid.Parse("00000000-0000-0000-0000-0000000000d1");
    private static readonly Guid Described = Guid.Parse("00000000-0000-0000-0000-0000000000c1");
    private static readonly Guid Undescribed = Guid.Parse("00000000-0000-0000-0000-0000000000c2");

    private static Mock<IDataGateway> Gateway(params DataSetSampleConfiguration[] samples)
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<DataSetSampleConfiguration>>(It.IsAny<IDataCommand>(), It.Is<DataStoreTarget>(t => t.Container == "DataSetSample"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DataSetSampleConfiguration>>.Success(samples));
        gateway
            .Setup(g => g.Execute<IEnumerable<DataSetSampleRowConfiguration>>(It.IsAny<IDataCommand>(), It.Is<DataStoreTarget>(t => t.Container == "DataSetSampleRow"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DataSetSampleRowConfiguration>>.Success([]));
        return gateway;
    }

    [Fact]
    public async Task TheListAnswersEachSamplesDescriptionFromTheDescriptionsTableByItsId()
    {
        // The row's own member holds what a column used to: it is never the answer.
        var gateway = Gateway(
            new DataSetSampleConfiguration { Id = Described, Name = "happy-path", DataSetId = DataSetId, Description = "left on the row" },
            new DataSetSampleConfiguration { Id = Undescribed, Name = "edges", DataSetId = DataSetId, Description = "left on the row" });
        IReadOnlyDictionary<Guid, string> texts = new Dictionary<Guid, string> { [Described] = "The rows people mean" };

        var listed = await DataSetSampleReader.ListForDataSet(
            gateway.Object, Store, Path, DataSetId, new Dictionary<Guid, string>(), texts, TestContext.Current.CancellationToken);

        listed.IsSuccess.ShouldBeTrue(listed.CurrentMessage);
        var samples = listed.Value.ShouldNotBeNull();
        samples.Single(s => s.Name == "happy-path").Description.ShouldBe("The rows people mean");
        samples.Single(s => s.Name == "edges").Description.ShouldBeNull();
    }

    [Fact]
    public async Task ASampleWrittenWhenNobodyStatedTheDescriptionLanguageFailsTheRouteAndNamesTheLanguage()
    {
        var gateway = Gateway();
        gateway
            .Setup(g => g.Execute<ConfigurationWriteResult>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], new Dictionary<string, object?>())));
        var writer = new Mock<IDescriptionWriter>(MockBehavior.Strict);
        writer.Setup(w => w.Language()).Returns(GenericResult<string>.Failure(new GenericMessage("language not stated")));
        var provider = new DataSetConfigurationProvider(
            NullLogger<DataSetConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>().Object, Store, Path, "DataSet", new UnavailablePlacement(),
            writer.Object, new Mock<IDescriptionReader>(MockBehavior.Strict).Object);
        var request = new DataSetSampleRequest { Name = "Orders", SampleName = "happy-path", Description = "The rows people mean" };

        var written = await DataSetSampleWriter.Upsert(
            gateway.Object, Store, Path, provider, DataSetId, new Dictionary<string, Guid>(), request, NullLogger.Instance, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    public void TheDataSetDetailsAggregateAnswersTheDescriptionTheProviderFilled()
    {
        // The provider fills an aggregate's Description from the descriptions table on every read that composes it; the detail route maps the member as it always did.
        var aggregate = new DataSetAggregateConfiguration { Id = Guid.NewGuid(), AggregateColumnName = "TotalSales", Description = "Sales summed by state" };

        DataSetQueryHelper.MapToAggregateDto(aggregate).Description.ShouldBe("Sales summed by state");
    }
}
