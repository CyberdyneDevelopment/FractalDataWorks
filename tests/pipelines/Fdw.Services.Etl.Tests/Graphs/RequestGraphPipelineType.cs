using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// A second closing of <see cref="GraphPipelineTypeBase{TIn, TOut}"/> (FDW-6244), beside <see cref="GraphPipelineType"/>: a graph pipeline type that takes a
/// question and answers with text. It declares no <c>[Implementation]</c>, so the test builds a host with it by hand and it never joins the collection the
/// other tests read.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RequestGraphPipelineType : GraphPipelineTypeBase<string, string>
{
    /// <summary>The name a pipeline's domain row would give this implementation.</summary>
    public const string OptionName = "Request";

    /// <summary>Initializes a new instance of the <see cref="RequestGraphPipelineType"/> class.</summary>
    public RequestGraphPipelineType()
        : base(
            name: OptionName,
            displayName: OptionName,
            description: "A test graph pipeline type that takes a question and answers with text",
            runModeName: "Queued")
    {
        Registration((builder, loggerFactory) => GraphRegistration(this, builder, loggerFactory));
        Initialization((host, loggerFactory) => GraphInitialization(this, host, loggerFactory));
    }
}
