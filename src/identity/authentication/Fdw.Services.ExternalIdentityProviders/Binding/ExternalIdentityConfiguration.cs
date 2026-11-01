using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>
/// One row of <c>auth.ExternalIdentity</c> — a subject at an outside authority, bound to a user here.
/// </summary>
/// <remarks>
/// The pair (<see cref="Issuer"/>, <see cref="ExternalSubject"/>) is unique, because a subject
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
public partial class ExternalIdentityConfiguration : IExternalIdentityImplementationConfiguration
{


    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    /// <remarks>The issuer and the subject together, which is what this row is addressed by.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string Issuer { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? ExternalSubject { get; set; }

    /// <inheritdoc/>
    public string? InvitedEmail { get; set; }

    /// <summary>Gets or sets the local user this identity belongs to.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets whether this binding may still be used.</summary>
    /// <remarks>
    /// A disabled binding is not a missing one. Deleting the row would let the same external subject
    /// be provisioned again as a new user, which is not what disabling an identity means.
    /// </remarks>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the moment this binding first resolved a sign-in.
    /// </summary>
    /// <remarks>
    /// Null means an administrator wrote the binding and the provider has not yet asserted the
    /// subject through it: the binding is real but unproven, and a console shows it as pending.
    /// </remarks>
    public DateTimeOffset? BoundAt { get; set; }

    /// <summary>
    /// The name a binding row carries: the issuer and the subject together.
    /// </summary>
    /// <remarks>
    /// A binding is one per (Issuer, ExternalSubject) — the table's unique index says so — and a
    /// configuration row is addressed by name, so the name has to carry both. Naming rows after the
    /// issuer alone lets the second subject from an issuer overwrite the first.
    /// </remarks>
    /// <param name="issuer">The issuer that asserts the subject.</param>
    /// <param name="externalSubject">That issuer's own identifier for the person.</param>
    public static string BindingName(string issuer, string externalSubject)
        => issuer + ":" + externalSubject;

    /// <summary>
    /// The name an invitation row carries: the issuer and the invited address together.
    /// </summary>
    /// <remarks>
    /// One open invitation per (Issuer, InvitedEmail), which the table's unique index enforces. The row
    /// keeps this name once redeemed; the subject is then in <see cref="ExternalSubject"/>.
    /// </remarks>
    /// <param name="issuer">The issuer the invited person will sign in through.</param>
    /// <param name="invitedEmail">The address the invitation is for.</param>
    public static string InvitationName(string issuer, string invitedEmail)
        => issuer + ":invited:" + invitedEmail.ToUpperInvariant();
}
