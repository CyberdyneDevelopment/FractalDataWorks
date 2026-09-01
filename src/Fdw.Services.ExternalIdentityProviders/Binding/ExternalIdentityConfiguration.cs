using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>
/// One row of <c>auth.ExternalIdentity</c> — a subject at an outside authority, bound to a user here.
/// </summary>
/// <remarks>
/// The pair (<see cref="Provider"/>, <see cref="ExternalSubject"/>) is unique, because a subject
/// identifier means nothing without the authority that minted it. There is deliberately no email
/// column: an address is often unverified, changes, and can be asserted for different people by
/// different providers, so matching on one is the standard account-takeover path in federated login.
/// </remarks>
/// <remarks>
/// Lives in the ExternalIdentityProviders domain, not Authentication: both
/// <c>Authentication.Binding.ExternalIdentityBinding</c> (reads it to resolve a principal) and
/// <c>ExternalIdentityProviders.ClaimMapped.ClaimMappedProvisioner</c> (writes it after JIT
/// provisioning) need this record, and Authentication already references ExternalIdentityProviders
/// (to reach the provisioner chain on an unbound subject) — the record has to live on the side that
/// doesn't create a cycle.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ExternalIdentity")]
public partial class ExternalIdentityConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    /// <remarks>The provider name, which is what this row is addressed by.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SectionName => "ExternalIdentities";

    /// <inheritdoc />
    public string ServiceType => "ExternalIdentity";

    /// <inheritdoc />
    public string? ServiceOptionType => null;

    /// <summary>Gets or sets the configured name of the authority that asserted the subject.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the subject identifier as that authority states it.</summary>
    public string ExternalSubject { get; set; } = string.Empty;

    /// <summary>Gets or sets the local user this identity belongs to.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets whether this binding may still be used.</summary>
    /// <remarks>
    /// A disabled binding is not a missing one. Deleting the row would let the same external subject
    /// be provisioned again as a new user, which is not what disabling an identity means.
    /// </remarks>
    public bool IsActive { get; set; }
}
