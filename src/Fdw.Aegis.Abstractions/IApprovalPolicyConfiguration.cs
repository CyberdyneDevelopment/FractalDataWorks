using System;
using Fdw.Configuration;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// Marker interface for typed Aegis command approval-policy bodies (<c>PreApprovedCommandConfiguration</c>,
/// <c>AdHocCommandConfiguration</c>). Each typed body implements this interface directly without
/// inheriting from <c>AegisCommandConfiguration</c>.
/// </summary>
/// <remarks>
/// Mirrors <c>IConnectionConfiguration</c>. Approval-policy bodies are (Phase 2) persisted in their
/// own tables and linked to the parent <c>AegisCommandConfiguration</c> row via an
/// <see cref="AegisCommandId"/> foreign key property. The parent carries an
/// <c>IApprovalPolicyConfiguration? Configuration</c> property populated on the read path.
/// </remarks>
public interface IApprovalPolicyConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the logical FK to the parent <c>AegisCommandConfiguration.Id</c>.</summary>
    Guid AegisCommandId { get; set; }
}
