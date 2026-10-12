using Fdw.Services.Etl;
using Fdw.Services.Etl.Pipelines;

namespace Fdw.Services.Etl.Abstractions.Tests;

/// <summary>
/// Locks the <see cref="IEtlPipelineImplementationConfiguration"/> linkage-bearing contract: both
/// engines must be assignable to the domain's contract and round-trip its linkage members, so the
/// lineage graph reads any engine polymorphically (no <c>is BatchCopy...</c> branch).
/// </summary>
[ExcludeFromCodeCoverage]
public class IEtlPipelineImplementationConfigurationLinkageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void BatchCopyPipelineConfigurationIsAssignableToLinkageInterface()
    {
        var engine = new BatchCopyPipelineConfiguration();

        engine.ShouldBeAssignableTo<IEtlPipelineImplementationConfiguration>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void StreamingPipelineConfigurationIsAssignableToLinkageInterface()
    {
        var engine = new StreamingPipelineConfiguration();

        engine.ShouldBeAssignableTo<IEtlPipelineImplementationConfiguration>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void BatchCopyPipelineConfigurationRoundTripsLinkageThroughInterface()
    {
        var concrete = new BatchCopyPipelineConfiguration
        {
            IsEnabled = true,
            SourceConnectionName = "SourceConn",
            SourceDataSet = "SourceDs",
            DestinationConnectionName = "DestConn",
            DestinationDataSet = "DestDs",
            SourceDataSetId = Guid.NewGuid(),
            SinkDataSetId = Guid.NewGuid()
        };

#pragma warning disable CA1859 // interface-contract test, narrowing defeats the point
        IEtlPipelineImplementationConfiguration engine = concrete;
#pragma warning restore CA1859

        engine.IsEnabled.ShouldBeTrue();
        engine.SourceConnectionName.ShouldBe(concrete.SourceConnectionName);
        engine.SourceDataSet.ShouldBe(concrete.SourceDataSet);
        engine.DestinationConnectionName.ShouldBe(concrete.DestinationConnectionName);
        engine.DestinationDataSet.ShouldBe(concrete.DestinationDataSet);
        engine.SourceDataSetId.ShouldBe(concrete.SourceDataSetId);
        engine.SinkDataSetId.ShouldBe(concrete.SinkDataSetId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void StreamingPipelineConfigurationRoundTripsLinkageThroughInterface()
    {
        var concrete = new StreamingPipelineConfiguration
        {
            IsEnabled = false,
            SourceConnectionName = "StreamSourceConn",
            SourceDataSet = "StreamSourceDs",
            DestinationConnectionName = "StreamDestConn",
            DestinationDataSet = "StreamDestDs",
            SourceDataSetId = Guid.NewGuid(),
            SinkDataSetId = Guid.NewGuid()
        };

#pragma warning disable CA1859 // interface-contract test, narrowing defeats the point
        IEtlPipelineImplementationConfiguration engine = concrete;
#pragma warning restore CA1859

        engine.IsEnabled.ShouldBeFalse();
        engine.SourceConnectionName.ShouldBe(concrete.SourceConnectionName);
        engine.SourceDataSet.ShouldBe(concrete.SourceDataSet);
        engine.DestinationConnectionName.ShouldBe(concrete.DestinationConnectionName);
        engine.DestinationDataSet.ShouldBe(concrete.DestinationDataSet);
        engine.SourceDataSetId.ShouldBe(concrete.SourceDataSetId);
        engine.SinkDataSetId.ShouldBe(concrete.SinkDataSetId);
    }
}
