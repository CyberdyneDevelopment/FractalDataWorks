using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Issue type for orphaned role permission configuration issues.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationIssueTypes), "OrphanedRolePermission", RestrictToCurrentCompilation = true)]
public sealed class OrphanedRolePermissionIssueType : ConfigurationIssueTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrphanedRolePermissionIssueType"/> class.
    /// </summary>
    public OrphanedRolePermissionIssueType() : base(4, "OrphanedRolePermission", isHealable: true) { }
}
