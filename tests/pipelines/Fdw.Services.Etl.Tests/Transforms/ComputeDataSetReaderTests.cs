using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Tests.Pipelines;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for the Compute stage's data set read (FDW-4911): it reads through the gateway's stable read of the data set, with no page size
/// of its own, and a refusal names the stage, the data set and why.
/// </summary>
public sealed class ComputeDataSetReaderTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task EveryRowOfTheDataSetIsReadThroughOneStableReadAndReleased()
    {
        var events = new List<string>();
        IDataCommand? asked = null;
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand command, DataSetTarget target, CancellationToken _) =>
            {
                asked = command;
                target.DataSet.ShouldBe("Devices");
                return Task.FromResult<IGenericResult<IRecordSource<DataRecord>>>(
                    GenericResult<IRecordSource<DataRecord>>.Success(new CountingRecordSource([.. PagedSourceGateway.Rows(2500)], events)));
            });

        var read = await ComputeDataSetReader.ReadAll(gateway.Object, "compute", "devices", "Devices", NullLogger.Instance, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value!.Count.ShouldBe(2500);
        read.Value[2499]["Id"].ShouldBe(2499);
        asked.ShouldBeAssignableTo<IQueryCommand>().ShouldNotBeNull().Paging.ShouldBeNull();
        events.ShouldBe(["closed after 2500"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADataSetTheGatewayRefusesIsUnreadableNamingTheStageTheDataSetAndTheReason()
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRecordSource<DataRecord>>.Failure(new GenericMessage("declares no key")));

        var read = await ComputeDataSetReader.ReadAll(gateway.Object, "compute", "devices", "Devices", NullLogger.Instance, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        var message = read.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("compute");
        message.ShouldContain("Devices");
        message.ShouldContain("declares no key");
    }
}
