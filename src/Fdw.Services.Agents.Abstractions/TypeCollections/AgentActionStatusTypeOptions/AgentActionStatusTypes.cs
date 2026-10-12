using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Agents.Abstractions.TypeCollections.AgentActionStatusTypeOptions;

/// <summary>
/// TypeCollection for agent action review status types.
/// Source generator will populate with all discovered TypeOptions.
/// </summary>
[TypeCollection(typeof(AgentActionStatusTypeBase), typeof(IAgentActionStatusType), typeof(AgentActionStatusTypes))]
public sealed partial class AgentActionStatusTypes : TypeCollectionBase<AgentActionStatusTypeBase, IAgentActionStatusType>
{
}
