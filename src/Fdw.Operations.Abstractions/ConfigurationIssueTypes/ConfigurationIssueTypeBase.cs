using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Base class for configuration issue types.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class ConfigurationIssueTypeBase : TypeOptionBase<int, ConfigurationIssueTypeBase>, IConfigurationIssueType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationIssueTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this configuration issue type.</param>
    /// <param name="name">The name of this configuration issue type.</param>
    /// <param name="isHealable">Whether this issue can be automatically healed.</param>
    protected ConfigurationIssueTypeBase(int id, string name, bool isHealable)
        : base(id, name)
    {
        IsHealable = isHealable;
    }

    /// <inheritdoc />
    public bool IsHealable { get; }
}
