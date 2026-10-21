using System.Collections.Generic;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The chain of Transformations one mapping on a component input states (FDW-2000), as the chain lookup needs it: where the mapping sits,
/// and the steps it names.
/// </summary>
/// <param name="Component">The name of the component whose input the mapping is on, which places a problem.</param>
/// <param name="Index">The mapping's position among the component's mappings.</param>
/// <param name="Target">The output field the mapping writes, which names the mapping in a problem.</param>
/// <param name="Chain">The steps the mapping states, each naming its Transformation by name or by id.</param>
public sealed record MapChain(string Component, int Index, string Target, IList<PipelineFieldTransformationReference> Chain);
