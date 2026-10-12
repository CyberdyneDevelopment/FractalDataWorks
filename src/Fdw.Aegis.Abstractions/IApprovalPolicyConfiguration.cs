using System;
using Fdw.Configuration;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// The contract every Aegis command implementation carries (<c>PreApprovedCommandConfiguration</c>,
/// <c>AdHocCommandConfiguration</c>).
/// </summary>
/// <remarks>
/// The AegisCommand domain row is Id, Name, Domain and Implementation only. Everything a reader needs
/// about a command -- the connection it runs against and the secret it may inject -- is here, on the
/// implementation, whose row hangs from the domain row by <see cref="AegisCommandId"/>.
/// </remarks>
public interface IApprovalPolicyConfiguration : IGenericConfiguration, IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record's durable id.</summary>
    Guid AegisCommandId { get; set; }

    /// <summary>Gets or sets the declared connection this command runs against.</summary>
    string ConnectionName { get; set; }

    /// <summary>
    /// Gets or sets the name of the secret manager that owns the secret this command may inject. A
    /// reference only -- never the secret value.
    /// </summary>
    string SecretManagerName { get; set; }

    /// <summary>Gets or sets the key name of the secret within <see cref="SecretManagerName"/>.</summary>
    string SecretKeyName { get; set; }
}
