using System;
using Fdw.Aegis.Abstractions;
using Fdw.Aegis.Configuration;
using Fdw.Aegis.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Aegis;

/// <summary>
/// Phase 1's <see cref="IApprovalPolicyEvaluator"/>: fail-closed, deterministic, no human/agent in
/// the loop. Approves ONLY when the requested command's declared policy
/// <see cref="AegisCommandConfiguration.ServiceOptionType"/> is <c>"PreApproved"</c> — every other
/// case (undeclared command, <c>"AdHoc"</c>, or any future policy kind) is denied. Phases 2-4 add
/// human/agent evaluators against this same interface.
/// </summary>
public sealed class PreApprovedPolicyEvaluator : IApprovalPolicyEvaluator
{
    private readonly IOptions<AegisCommandsOptions> _commands;
    private readonly ILogger<PreApprovedPolicyEvaluator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreApprovedPolicyEvaluator"/> class.
    /// </summary>
    public PreApprovedPolicyEvaluator(IOptions<AegisCommandsOptions> commands, ILogger<PreApprovedPolicyEvaluator>? logger = null)
    {
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        _logger = logger ?? NullLogger<PreApprovedPolicyEvaluator>.Instance;
    }

    /// <inheritdoc />
    public IGenericResult<Verdict> Evaluate(ApprovalRequest request)
    {
        var declared = _commands.Value.Commands;
        AegisCommandConfiguration? command = null;
        for (var i = 0; i < declared.Count; i++)
        {
            if (string.Equals(declared[i].ConnectionName, request.ConnectionName, StringComparison.Ordinal)
                && string.Equals(declared[i].Name, request.CommandName, StringComparison.Ordinal))
            {
                command = declared[i];
                break;
            }
        }

        // Why fail-closed: only a declared command whose policy is the PreApproved option is approved;
        // an undeclared command or any other policy kind is denied — there is no "default = allow".
        if (command is not null)
            AegisLog.PolicyEvaluated(_logger, command.ServiceOptionType, request.CommandName);

        if (command is not null
            && string.Equals(command.ServiceOptionType, ApprovalPolicyTypes.PreApproved.Name, StringComparison.Ordinal))
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
            : $"policy '{command.ServiceOptionType}' is not PreApproved";

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
