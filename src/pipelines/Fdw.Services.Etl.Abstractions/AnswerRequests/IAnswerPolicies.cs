using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>
/// Judges a call under an answer policy (FDW-6142): which calls go on by themselves, which are refused, which need the person and which a stored grant covers. The policy is configuration (pipe.AnswerPolicy and
/// pipe.AnswerPolicyCall); nothing here assumes a policy, a call or a requirement.
/// </summary>
public interface IAnswerPolicies
{
    /// <summary>Judges a call.</summary>
    /// <param name="policyName">The policy's name.</param>
    /// <param name="callName">The call's name, as the policy lists it.</param>
    /// <param name="grantId">The stored grant to judge a grant-or-person call against; null when the run has none, which leaves such a call to the person.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>What the call is judged to be and how long a request lasts; or the refusal when the policy is not there, does not list the call, states a requirement that is none, or the grant stated cannot be read.</returns>
    Task<IGenericResult<AnswerPolicyJudgment>> Judge(string policyName, string callName, Guid? grantId, CancellationToken cancellationToken = default);
}
