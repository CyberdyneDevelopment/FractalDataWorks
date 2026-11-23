using System;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Pipelines.Endpoints;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests.Endpoints;

/// <summary>
/// FDW-6519: the execution and history routes return the person, the run mode, the host and the slot a run's row states, through the one mapping all three use.
/// FDW-6672: the record states who acted as one fact in one field (ActingPersonId); the always-null ExecutedBy is not a member of the row's three record types.
/// </summary>
[Trait("Category", "RunRecord")]
public sealed class PipelineExecutionRecordMappingTests
{
    [Fact]
    [Trait("Priority", "P0")]
    public void TheFourFactsOfTheRowAreTheFactsOfTheRecord()
    {
        var person = Guid.NewGuid();

        var record = PipelineExecutionRecordMapping.From(new PipelineExecutionDbRecord
        {
            Id = Guid.NewGuid(), PipelineName = "Load", Status = "Succeeded",
            ActingPersonId = person, RunMode = "Queued", ExecutingHost = "Reference.Etl.Server", ExecutingSlot = "b",
        });

        record.ActingPersonId.ShouldBe(person);
        record.RunMode.ShouldBe("Queued");
        record.ExecutingHost.ShouldBe("Reference.Etl.Server");
        record.ExecutingSlot.ShouldBe("b");
        record.Success.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AnAbsentFactIsAbsentInTheRecord()
    {
        var record = PipelineExecutionRecordMapping.From(new PipelineExecutionDbRecord { Id = Guid.NewGuid(), PipelineName = "Load", Status = "Queued" });

        record.ActingPersonId.ShouldBeNull();
        record.RunMode.ShouldBeNull();
        record.ExecutingHost.ShouldBeNull();
        record.ExecutingSlot.ShouldBeNull();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData(typeof(PipelineExecutionRecord))]
    [InlineData(typeof(PipelineExecutionDbRecord))]
    [InlineData(typeof(EtlPipelineExecutionRecord))]
    public void TheRecordHasNoExecutedByMemberBecauseThePersonIsActingPersonId(Type record)
    {
        record.GetProperty("ExecutedBy").ShouldBeNull();
        record.GetProperty("ActingPersonId").ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ARecordPropertyWhoseColumnTheResultLacksIsNullAndTheReadSucceeds()
    {
        // FDW-6672, the order of the two migrations: the field row of etl.PipelineExecution.ExecutedBy is retired first, so a copy of a host that still has the property
        // reads a result with no such column; the generated mapper answers the property null and the read succeeds. The column is dropped second.
        // Why the codebase's own reader and not a stand-in: its GetOrdinal answers a column the result lacks with the IndexOutOfRangeException a database reader throws, which the generated mapper reads
        // as "no such column".
        var id = Guid.NewGuid();
        var reader = new RecordDictionaryReader([new System.Collections.Generic.Dictionary<string, object?>(StringComparer.Ordinal) { ["Id"] = id, ["PipelineName"] = "Load", ["Status"] = "Succeeded" }]);
        reader.Read().ShouldBeTrue();

        var mapped = PocoMapperCollection.ByName(nameof(PipelineExecutionDbRecord)).MapFromReader(reader, new Mock<IStorageContainer>().Object);

        mapped.IsSuccess.ShouldBeTrue(mapped.CurrentMessage);
        var record = mapped.Value.ShouldBeOfType<PipelineExecutionDbRecord>();
        record.Id.ShouldBe(id);
        record.PipelineName.ShouldBe("Load");
        record.ErrorMessage.ShouldBeNull();
        record.ActingPersonId.ShouldBeNull();
    }
}
