using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Abstractions;

/// <summary>
/// Just-in-time provisioning mechanism consulted by the external-identity issuance path when an
/// externally-validated identity has no existing <c>auth.ExternalIdentity</c> link to a FDW user.
/// A <see cref="IExternalIdentityProvisioner"/> is a <see cref="IServiceOption"/> resolved through
/// <c>ExternalIdentityProvisionerTypes</c>' <c>IFdwServiceProvider&lt;IExternalIdentityProvisioner,
/// ExternalIdentityProvisionerConfiguration&gt;</c> — selected per (tenant, external provider) by
/// <c>ExternalIdentityProvisionerBindingConfigurationProvider.ResolveProvisionerName</c>. Default is
/// OFF: with no matching binding row, the issuance path behaves exactly as it did before this
/// mechanism existed (a lookup miss fails loud as "external identity not found").
/// </summary>
/// <remarks>
/// <para>
/// <b>NOT-FOUND CONTRACT (load-bearing — leaf implementations MUST follow this exactly):</b> a
/// provisioner that determines the externally-validated subject is not one it provisions (e.g. one
/// step of a composite chain trying the next sibling) MUST return a <c>Failure</c> whose
/// <c>IGenericResult.Code</c> is the canonical NotFound <see cref="Fdw.Results.Abstractions.IResultCode"/>
/// — the categorized number <c>30000</c> reserved in the ResultCode catalog for "not found" (every
/// package's own NotFound code reuses this number under its own prefix; e.g.
/// <c>GenericResult&lt;Guid&gt;.Failure(MyDomainResultCodes.NotFound)</c>). Callers that walk multiple
/// provisioners (e.g. <c>ChainedExternalIdentityProvisioner</c>) check
/// <c>result.Code?.Id == 30000</c> to decide "this subject isn't mine — fall through to the next
/// candidate" versus "this is a hard error — propagate immediately." ANY OTHER failure (a message-only
/// <c>GenericResult.Failure(IGenericMessage)</c>, or a <c>Failure</c> carrying a different
/// <see cref="Fdw.Results.Abstractions.IResultCode"/>) is treated as a hard error and propagated without
/// falling through. Implementations that merely log-and-fail with a plain message for "not mine" will
/// be treated as a hard failure, not a fall-through — they MUST use the ResultCode-based Failure
/// overload for that specific case.
/// </para>
/// </remarks>
public interface IExternalIdentityProvisioner : IServiceOption
{
    /// <summary>
    /// Provisions a FDW user for an externally-validated identity that has no existing link row, and
    /// returns the newly created user's durable Id. Implementations are responsible for creating the
    /// <c>auth.ExternalIdentity</c> link row themselves (so subsequent logins resolve via the normal
    /// lookup path, not this provisioner). See the NOT-FOUND CONTRACT on this interface's remarks for
    /// the required failure shape when this provisioner does not recognize the subject.
    /// </summary>
    /// <param name="provider">The external identity provider name (e.g. the configured Oidc provider's <c>Name</c>).</param>
    /// <param name="externalSubject">The external subject identifier (the IdP's <c>sub</c> claim).</param>
    /// <param name="externalPrincipal">The already-validated external principal, for reading claims (email, display name, ...) needed to provision the user.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<Guid>> Provision(
        string provider,
        string externalSubject,
        ClaimsPrincipal externalPrincipal,
        CancellationToken cancellationToken = default);
}
