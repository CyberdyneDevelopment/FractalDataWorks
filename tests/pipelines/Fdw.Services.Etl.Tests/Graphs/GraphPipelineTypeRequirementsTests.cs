using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Descriptions;
using Fdw.Services.Etl.Graphs.Logging;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-3865, FDW-4488, FDW-4588, FDW-6244, FDW-7323: the graph pipeline type declares the services a run needs (the threshold evaluator, the data set writer runs, and, as every
/// pipeline type does, the pipe.Pipeline configuration provider and the descriptions writer, which the pipeline provider writes each description with), each with its own coded message, so a host that attached the option without them is refused at
/// Initialize and a host that never attached it is not asked.
/// </summary>
public sealed class GraphPipelineTypeRequirementsTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheGraphTypeRequiresTheThresholdEvaluatorTheDataSetWriterRunsThePipelineConfigurationProviderAndTheDescriptionWriter()
    {
        var requirements = new GraphPipelineType().Requirements;

        requirements.Select(r => r.Service).ToArray().ShouldBe([typeof(IThresholdEvaluator), typeof(IDataSetWriterRuns), typeof(IPipelineConfigurationProvider), typeof(IDescriptionWriter)], ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void EachRequirementCarriesItsOwnCodedMessage()
    {
        var requirements = new GraphPipelineType().Requirements;
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        var codes = requirements.ToDictionary(r => r.Service, r => r.Message(logger).Code);

        codes[typeof(IThresholdEvaluator)].ShouldBe("RUNTHRESHOLD-23004");
        codes[typeof(IDataSetWriterRuns)].ShouldBe("FLOW-60350");
        codes[typeof(IPipelineConfigurationProvider)].ShouldBe("PIPELINES-60833");
        codes[typeof(IDescriptionWriter)].ShouldBe("PIPELINES-60860");
    }
}
