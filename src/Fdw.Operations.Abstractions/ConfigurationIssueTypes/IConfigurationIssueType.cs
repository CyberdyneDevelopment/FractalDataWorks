using Fdw.Collections;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Interface for configuration issue types.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
public interface IConfigurationIssueType : ITypeOption<int, ConfigurationIssueTypeBase>
{
    /// <summary>
    /// Gets a value indicating whether this issue can be automatically healed.
    /// </summary>
    bool IsHealable { get; }
}
