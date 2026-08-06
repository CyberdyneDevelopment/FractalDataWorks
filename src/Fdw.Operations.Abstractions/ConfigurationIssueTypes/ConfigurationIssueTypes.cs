using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// TypeCollection for configuration issue types.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for configuration issue types.
/// Source generator creates static properties for each registered configuration issue type.
/// </remarks>
[TypeCollection(typeof(ConfigurationIssueTypeBase), typeof(IConfigurationIssueType), typeof(ConfigurationIssueTypes))]
public sealed partial class ConfigurationIssueTypes : TypeCollectionBase<ConfigurationIssueTypeBase, IConfigurationIssueType>
{
}
