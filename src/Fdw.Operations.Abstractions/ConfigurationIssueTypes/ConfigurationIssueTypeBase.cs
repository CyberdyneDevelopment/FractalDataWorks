using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Base class for configuration issue types.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class ConfigurationIssueTypeBase : TypeOptionBase<ConfigurationIssueTypeBase>, IConfigurationIssueType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationIssueTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of this configuration issue type.</param>
    /// <param name="isHealable">Whether this issue can be automatically healed.</param>
    protected ConfigurationIssueTypeBase(string name, bool isHealable)
        : base(name)
    {
        IsHealable = isHealable;
    }

    /// <inheritdoc />
    public bool IsHealable { get; }
}
