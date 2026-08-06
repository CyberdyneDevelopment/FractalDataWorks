using Fdw.Services.Etl.Pipelines;

namespace Fdw.Services.Etl.Abstractions.Tests;

/// <summary>
/// Locks the promoted <see cref="IEtlPipelineTypedConfiguration"/> linkage-bearing contract: both
/// engine typed bodies must be assignable to the shared interface and round-trip its linkage members,
/// so the lineage graph can dot-walk any engine polymorphically (no <c>is BatchCopy...</c> branch).
/// </summary>
[ExcludeFromCodeCoverage]
public class IEtlPipelineTypedConfigurationLinkageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void BatchCopyPipelineConfigurationIsAssignableToLinkageInterface()
    {
        var engine = new BatchCopyPipelineConfiguration();

        engine.ShouldBeAssignableTo<IEtlPipelineTypedConfiguration>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void StreamingPipelineConfigurationIsAssignableToLinkageInterface()
    {
        var engine = new StreamingPipelineConfiguration();

        engine.ShouldBeAssignableTo<IEtlPipelineTypedConfiguration>();
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

        IEtlPipelineTypedConfiguration engine = concrete;

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

        IEtlPipelineTypedConfiguration engine = concrete;

        engine.IsEnabled.ShouldBeFalse();
        engine.SourceConnectionName.ShouldBe(concrete.SourceConnectionName);
        engine.SourceDataSet.ShouldBe(concrete.SourceDataSet);
        engine.DestinationConnectionName.ShouldBe(concrete.DestinationConnectionName);
        engine.DestinationDataSet.ShouldBe(concrete.DestinationDataSet);
        engine.SourceDataSetId.ShouldBe(concrete.SourceDataSetId);
        engine.SinkDataSetId.ShouldBe(concrete.SinkDataSetId);
    }
}
