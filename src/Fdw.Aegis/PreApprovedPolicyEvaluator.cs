using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Aegis.Abstractions;
using Fdw.Aegis.Configuration;
using Fdw.Aegis.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Aegis;

/// <summary>
/// Phase 1's <see cref="IApprovalPolicyEvaluator"/>: fail-closed, deterministic, no human/agent in
/// the loop. Approves ONLY when the requested command's declared implementation is
/// <c>"PreApproved"</c> — every other case (undeclared command, <c>"AdHoc"</c>, or any future policy
/// kind) is denied. Phases 2-4 add human/agent evaluators against this same interface.
/// </summary>
public sealed class PreApprovedPolicyEvaluator : IApprovalPolicyEvaluator
{
    private readonly IAegisCommandConfigurationProvider _commands;
    private readonly ILogger<PreApprovedPolicyEvaluator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreApprovedPolicyEvaluator"/> class.
    /// </summary>
    public PreApprovedPolicyEvaluator(IAegisCommandConfigurationProvider commands, ILogger<PreApprovedPolicyEvaluator>? logger = null)
    {
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        _logger = logger ?? NullLogger<PreApprovedPolicyEvaluator>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Verdict>> Evaluate(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        var declared = await _commands.Get(cancellationToken).ConfigureAwait(false);
        if (!declared.IsSuccess)
            return declared.ToNewResult<Verdict>();

        IApprovalPolicyConfiguration? command = null;
        foreach (var candidate in declared.Value!)
        {
            if (string.Equals(candidate.ConnectionName, request.ConnectionName, StringComparison.Ordinal)
                && string.Equals(candidate.Name, request.CommandName, StringComparison.Ordinal))
            {
                command = candidate;
                break;
            }
        }

        if (command is not null)
            AegisLog.PolicyEvaluated(_logger, command.Implementation, request.CommandName);

        if (command is not null
            && string.Equals(command.Implementation, ApprovalPolicyTypes.PreApproved.Name, StringComparison.Ordinal))
        {
            return GenericResult<Verdict>.Success(new Verdict
            {
                Disposition = VerdictDispositions.Approve,
                CorrelationId = request.CorrelationId,
                DecidedAt = DateTimeOffset.UtcNow,
                Actor = nameof(PreApprovedPolicyEvaluator),
                Reason = null,
            });
        }

        var reason = command is null
            ? $"command '{request.CommandName}' is not declared for connection '{request.ConnectionName}'"
            : $"policy '{command.Implementation}' is not PreApproved";

        AegisLog.ActionDenied(_logger, request.CommandName, reason);

        return GenericResult<Verdict>.Success(new Verdict
        {
            Disposition = VerdictDispositions.Deny,
            CorrelationId = request.CorrelationId,
            DecidedAt = DateTimeOffset.UtcNow,
            Actor = nameof(PreApprovedPolicyEvaluator),
            Reason = reason,
        });
    }
}
