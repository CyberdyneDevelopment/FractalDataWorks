using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Runs an authentication flow's steps in order and issues a token if they establish enough to warrant one.
/// </summary>
/// <remarks>
/// What a transport (an HTTP login, an OIDC return, a SQL Server login) holds instead of the runner itself,
/// so none of them depends on the concrete class (FDW-4618 rule 4).
/// </remarks>
public interface IAuthenticationRunner
{
    /// <summary>Runs <paramref name="flow"/> from its first step.</summary>
    /// <param name="flow">The flow to run.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The flow's result, or a structured failure.</returns>
    Task<IGenericResult<FlowResult>> Run(AuthenticationFlow flow, CancellationToken cancellationToken = default);

    /// <summary>Resumes a suspended flow by the token handed out when it suspended.</summary>
    /// <param name="resumeToken">The token handed out when the flow suspended.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The flow's result, or a structured failure.</returns>
    Task<IGenericResult<FlowResult>> Resume(string resumeToken, CancellationToken cancellationToken = default);
}
