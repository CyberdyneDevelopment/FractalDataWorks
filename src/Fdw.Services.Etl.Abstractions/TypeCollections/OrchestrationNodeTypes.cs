using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

/// <summary>
/// TypeCollection for orchestration node types.
/// Extensible: external assemblies may register additional node types via module initializers.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(OrchestrationNodeTypeBase), typeof(IOrchestrationNodeType), typeof(OrchestrationNodeTypes))]
public abstract partial class OrchestrationNodeTypes : TypeCollectionBase<OrchestrationNodeTypeBase, IOrchestrationNodeType>
{
}
