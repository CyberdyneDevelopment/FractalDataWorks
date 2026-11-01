using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// A step that proves nothing itself and hands the caller to another authority to be proven.
/// </summary>
/// <remarks>
/// <para>
/// This is what makes a flow a "sign in with somebody else" rather than a login this platform
/// performs. It is declared rather than inferred for the same reason a step names itself: matching
/// on a step's name would work for the one federating step that ships today and quietly exclude the
/// next one, which is precisely the open set <see cref="IAuthenticationStep"/> exists to keep open.
/// </para>
/// <para>
/// Separate from <see cref="IAuthenticationStep"/> because federating is not something every step
/// does. A step implements both: the flow runs it through the step contract, and a caller choosing
/// where to sign in reads it through this one.
/// </para>
/// </remarks>
public interface IFederatingAuthenticationStep
{
    /// <summary>Names the authority this step hands the caller to.</summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <remarks>
    /// Asynchronous because which authority a deployment trusts is a configuration row, not a
    /// compiled-in constant — the same read the step performs to build its challenge. It fails
    /// rather than returning a placeholder: a provider button whose label nobody chose is a button
    /// nobody can be held to, and the failure a caller needs to see is that the authority is
    /// unreadable, not that it is called "Unknown".
    /// </remarks>
    Task<IGenericResult<string>> GetAuthorityName(CancellationToken cancellationToken = default);

    /// <summary>States the issuer a subject proved by this authority carries.</summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <remarks>
    /// This is the value an external-identity binding must hold, because a binding is matched
    /// against the proved subject's own issuer and nothing else. It is NOT the authority's display
    /// name and NOT the flow's name — a binding written with either of those writes, reads back,
    /// and silently never resolves a sign-in.
    ///
    /// Asynchronous and failing for the same reason as <see cref="GetAuthorityName"/>: which
    /// authority a deployment trusts is a configuration row, and a caller needs to see that it is
    /// unreadable rather than receive a placeholder that would be written into a binding.
    /// </remarks>
    Task<IGenericResult<string>> GetIssuer(CancellationToken cancellationToken = default);
}
