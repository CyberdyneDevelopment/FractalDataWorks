using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.AnswerRequests;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution.Answering;

/// <summary>
/// Judges a call under an answer policy (FDW-6142): the policy is <c>pipe.AnswerPolicy</c> and its calls <c>pipe.AnswerPolicyCall</c>, read through the platform's configuration gateway in the caller's tenant (the
/// row-level security of the tenant filters what is read). What a call requires is the requirement its row states, and the requirement judges it: no name is switched on here.
/// </summary>
/// <remarks>
/// <para>
/// Nothing is assumed. A policy that is not there, a call it does not list, a requirement that is none of <see cref="AnswerPolicyRequirements"/>, a requirement that needs a permission and states none (or states one it
/// does not need) and a policy that does not state a positive time are each a refusal of their own. A stored grant that was stated and cannot be read is a refusal too; a grant that was read and has ended, was revoked
/// or does not allow the permission leaves the call to the person.
/// </para>
/// </remarks>
public sealed class AnswerPolicies : IAnswerPolicies
{
    private const string PathName = "pipe";
    private const string PolicyContainerName = "AnswerPolicy";
    private const string CallContainerName = "AnswerPolicyCall";

    private readonly IConfigurationGatewayProvider _gateways;
    private readonly IRunGrants _grants;
    private readonly TimeProvider _time;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="AnswerPolicies"/> class.</summary>
    /// <param name="gateways">Gives the platform's configuration gateway.</param>
    /// <param name="grants">Reads the stored grant a call may be judged against.</param>
    /// <param name="time">The clock a grant's end is judged against.</param>
    /// <param name="logger">The logger.</param>
    public AnswerPolicies(IConfigurationGatewayProvider gateways, IRunGrants grants, TimeProvider time, ILogger<AnswerPolicies>? logger = null)
    {
        _gateways = gateways;
        _grants = grants;
        _time = time;
        _logger = logger ?? NullLogger<AnswerPolicies>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AnswerPolicyJudgment>> Judge(string policyName, string callName, Guid? grantId, CancellationToken cancellationToken = default)
    {
        var policy = await ReadPolicy(policyName, cancellationToken).ConfigureAwait(false);
        if (policy.IsFailure || policy.Value is not { } found)
            return policy.ToNewResult<AnswerPolicyJudgment>();

        var call = await ReadCall(found, callName, cancellationToken).ConfigureAwait(false);
        if (call.IsFailure || call.Value is not { } listed)
            return call.ToNewResult<AnswerPolicyJudgment>();

        var requirement = RequirementOf(found.Name, listed);
        if (requirement.IsFailure || requirement.Value is not { } stated)
            return requirement.ToNewResult<AnswerPolicyJudgment>();

        var covers = await GrantCovers(found.Name, stated, listed, grantId, cancellationToken).ConfigureAwait(false);
        if (covers.IsFailure)
            return covers.ToNewResult<AnswerPolicyJudgment>();

        var outcome = stated.Judge(covers.Value);
        _ = AnswerPolicyLog.Judged(_logger, found.Name, listed.CallName, outcome.Name);
        return GenericResult<AnswerPolicyJudgment>.Success(new AnswerPolicyJudgment(found.Name, listed.CallName, outcome, found.ExpiresAfterSeconds));
    }

    private async Task<IGenericResult<AnswerPolicyRecord>> ReadPolicy(string policyName, CancellationToken cancellationToken)
    {
        var rows = await Gateway().Execute<IEnumerable<AnswerPolicyRecord>>(
            Query.From<AnswerPolicyRecord>(PlatformConnection.Name, PathName, PolicyContainerName).Where("Name", policyName).Where("IsCurrent", true).Where("IsDeleted", false).Build(),
            cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is null)
            return GenericResult<AnswerPolicyRecord>.Failure(AnswerPolicyLog.NotRead(_logger, policyName, rows.CurrentMessage));

        if (rows.Value.FirstOrDefault() is not { } policy)
            return GenericResult<AnswerPolicyRecord>.Failure(AnswerPolicyLog.PolicyNotFound(_logger, policyName));

        return policy.ExpiresAfterSeconds > 0
            ? GenericResult<AnswerPolicyRecord>.Success(policy)
            : GenericResult<AnswerPolicyRecord>.Failure(AnswerPolicyLog.PolicyInvalid(_logger, policyName, "it states no positive time a request stays open"));
    }

    private async Task<IGenericResult<AnswerPolicyCallRecord>> ReadCall(AnswerPolicyRecord policy, string callName, CancellationToken cancellationToken)
    {
        var rows = await Gateway().Execute<IEnumerable<AnswerPolicyCallRecord>>(
            Query.From<AnswerPolicyCallRecord>(PlatformConnection.Name, PathName, CallContainerName).Where("PolicyId", policy.Id).Where("IsCurrent", true).Where("IsDeleted", false).Build(),
            cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is null)
            return GenericResult<AnswerPolicyCallRecord>.Failure(AnswerPolicyLog.NotRead(_logger, policy.Name, rows.CurrentMessage));

        return rows.Value.FirstOrDefault(row => string.Equals(row.CallName, callName, StringComparison.OrdinalIgnoreCase)) is { } listed
            ? GenericResult<AnswerPolicyCallRecord>.Success(listed)
            : GenericResult<AnswerPolicyCallRecord>.Failure(AnswerPolicyLog.CallNotListed(_logger, policy.Name, callName));
    }

    // The requirement a call row states: one of the requirements, which states a permission exactly when it needs one.
    private IGenericResult<IAnswerPolicyRequirement> RequirementOf(string policyName, AnswerPolicyCallRecord call)
    {
        var requirement = AnswerPolicyRequirements.ByName(call.RequirementName);
        if (ReferenceEquals(requirement, AnswerPolicyRequirements.NotFound))
            return GenericResult<IAnswerPolicyRequirement>.Failure(AnswerPolicyLog.PolicyInvalid(_logger, policyName, $"call '{call.CallName}' states requirement '{call.RequirementName}', which is none of the requirements"));

        return !string.IsNullOrWhiteSpace(call.Permission) == requirement.NeedsPermission
            ? GenericResult<IAnswerPolicyRequirement>.Success(requirement)
            : GenericResult<IAnswerPolicyRequirement>.Failure(AnswerPolicyLog.PolicyInvalid(
                _logger, policyName, $"call '{call.CallName}' under requirement '{requirement.Name}' {(requirement.NeedsPermission ? "states no permission, which it needs" : "states a permission, which it does not take")}"));
    }

    // A grant covers a call when it was read, has not ended or been revoked, and allows the call's permission. A requirement that takes no permission, or a run with no grant, is covered by none.
    private async Task<IGenericResult<bool>> GrantCovers(
        string policyName, IAnswerPolicyRequirement requirement, AnswerPolicyCallRecord call, Guid? grantId, CancellationToken cancellationToken)
    {
        if (!requirement.NeedsPermission || grantId is not { } id || call.Permission is not { } permission)
            return GenericResult<bool>.Success(false);

        var read = await _grants.Read(id, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } grant)
            return GenericResult<bool>.Failure(AnswerPolicyLog.NotRead(_logger, policyName, read.CurrentMessage));

        return GenericResult<bool>.Success(!grant.HasEnded(_time.GetUtcNow()) && grant.NotAllowed([permission]).Count == 0);
    }

    private IDataGateway Gateway()
    {
        var found = _gateways.Get(PlatformConnection.Name);
        return found.IsSuccess && found.Value is { } gateway
            ? gateway
            : throw new InvalidOperationException($"No configuration gateway is available for '{PlatformConnection.Name}'; it must be declared in the host's configurationSchema.json, with the tables {PathName}.{PolicyContainerName} and {PathName}.{CallContainerName}.");
    }
}
