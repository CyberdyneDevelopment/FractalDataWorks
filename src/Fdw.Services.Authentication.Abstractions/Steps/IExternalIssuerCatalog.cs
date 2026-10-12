using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// States the issuers this deployment can resolve a proved subject from.
/// </summary>
/// <remarks>
/// An external-identity binding is matched against the issuer a proved subject carries and nothing
/// else, so a binding naming anything outside this set writes, reads back, and silently never
/// resolves a sign-in — the first real sign-in ignores it and provisions a second account instead.
/// Whoever writes a binding needs to be able to ask what is resolvable, and asking is the whole
/// point of this being a service rather than a list a caller assembles for itself.
/// </remarks>
public interface IExternalIssuerCatalog
{
    /// <summary>
    /// Reads every issuer a federating step in this deployment hands callers to, and says whether
    /// that is all of them.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the reads.</param>
    /// <remarks>
    /// An authority that cannot state its issuer leaves the set incomplete rather than failing the
    /// read: one misconfigured authority must not stop a binding naming a different, readable one.
    /// What it does stop is a REFUSAL, which needs the whole set to be honest.
    /// </remarks>
    Task<IGenericResult<ExternalIssuerSet>> Read(CancellationToken cancellationToken = default);
}
