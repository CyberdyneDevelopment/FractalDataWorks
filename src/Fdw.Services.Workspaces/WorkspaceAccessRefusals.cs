using System.Linq;
using Fdw.Results;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces;

/// <summary>Reads what an <see cref="IWorkspaceAccessPolicy"/> refusal means for the answer an endpoint gives.</summary>
public static class WorkspaceAccessRefusals
{
    /// <summary>
    /// Whether <paramref name="refusal"/> says the caller cannot see the workspace at all, so the
    /// endpoint must answer exactly as it does for a workspace that does not exist.
    /// </summary>
    /// <param name="refusal">A failure returned by the access policy.</param>
    /// <returns><see langword="true"/> when the refusal is <see cref="WorkspaceNotVisibleCode"/>.</returns>
    /// <remarks>
    /// Answering "absent" in the endpoint's own words rather than with the refusal's code keeps a
    /// hidden workspace and a missing one indistinguishable, so a caller cannot find Private names by
    /// probing. The policy has already logged the real reason (FDW-936).
    /// </remarks>
    public static bool Hides(IGenericResult refusal)
        => refusal is not null
           && (refusal.Code is WorkspaceNotVisibleCode || refusal.CodeChain.Any(code => code is WorkspaceNotVisibleCode));

    /// <summary>
    /// The answer for a workspace that does not exist: the same <see cref="WorkspaceNotVisibleCode"/>
    /// the policy gives a caller who may not view one, so the two cannot be told apart.
    /// </summary>
    /// <typeparam name="T">The value type of the endpoint's result.</typeparam>
    /// <param name="workspaceName">The name that was asked for.</param>
    /// <param name="logger">The logger that records the refusal.</param>
    /// <returns>A 404-category failure.</returns>
    /// <remarks>
    /// An endpoint that answered a missing workspace any other way -- a 500 for "the load returned no
    /// value", a bare 404 while a hidden one got a refusal body -- would let a caller find Private
    /// names by comparing the two (FDW-937).
    /// </remarks>
    public static IGenericResult<T> Absent<T>(string workspaceName, ILogger logger)
        => GenericResult<T>.Failure(
            WorkspacesResultCodes.ByName("WorkspaceNotVisible"), logger,
            ResultDetails.Create("name", workspaceName, "reason", "no such workspace"));

    /// <summary>
    /// Whether <paramref name="failure"/> is not a refusal at all but an access decision that could not
    /// be made, because something it needs could not be read.
    /// </summary>
    /// <param name="failure">A failure returned by the access policy.</param>
    /// <returns><see langword="true"/> when the failure is <see cref="WorkspaceAccessUndeterminedCode"/>.</returns>
    /// <remarks>
    /// Anything that treats a refusal as "leave it out" -- a filtered list, a lookup -- must pass this
    /// one on instead: leaving a workspace out because the role store was down would report an outage
    /// as an empty result (FDW-937).
    /// </remarks>
    public static bool IsUndetermined(IGenericResult failure)
        => failure is not null
           && (failure.Code is WorkspaceAccessUndeterminedCode || failure.CodeChain.Any(code => code is WorkspaceAccessUndeterminedCode));
}
