using System.Collections.Generic;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>Components with the links and dropped outputs between them, placed together in a graph by the graph tests.</summary>
/// <param name="Components">The components.</param>
/// <param name="Links">The links between their nodes.</param>
/// <param name="Dropped">The outputs that are deliberately linked to nothing.</param>
internal sealed record GraphPart(
    IReadOnlyList<PipelineComponentConfiguration> Components,
    IReadOnlyList<PipelineLinkConfiguration> Links,
    IReadOnlyList<PipelineDroppedOutputConfiguration> Dropped);
