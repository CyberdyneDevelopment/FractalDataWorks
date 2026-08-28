using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Aegis.Abstractions;
using Fdw.Aegis.Logging;
using Fdw.Results;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Aegis;

/// <summary>
/// The source-agnostic resolve-below-boundary core of the Aegis Gateway. Generalizes
/// <c>ConfiguredMcpClientDelegate</c> (mcp-hub — env-injection into a spawned MCP process) and
/// mirrors <c>OutboundCredentialAccessTokenProvider</c> (reference-scheduler — by-provider secret
/// resolution) into a single reusable resolve-inject-sanitize sequence.
/// </summary>
/// <remarks>
/// <para>
/// Resolution goes through <see cref="ISecretManagerProvider"/> — the SecretManager domain's own
/// provider — by NAME. The provider owns the whole resolution chain (configuration lookup, typed
/// body, factory dispatch); this injector supplies only the logical
/// <see cref="ApprovalRequest.SecretManagerName"/> the request names. It therefore never holds a
/// specific secret manager, never holds a directory of declared
/// <c>SecretManagerConfiguration</c>s, and never switches on secret SOURCE kind
/// (MsSql/AzureKeyVault/EnvironmentVariable/...) — swapping the declared backend requires zero
/// changes here. Each backend's <c>[ServiceTypeOption]</c> self-wires via its own
/// <c>Configuration</c>/<c>Registration</c> bodies, so the set of reachable managers is decided by
/// which option packages the host references, not by anything in Aegis.
/// </para>
/// <para>
/// Plaintext lifetime: the resolved <see cref="SecretValue"/> lives only inside the
/// <see langword="using"/> block below, handed to <see cref="IAegisInjectionTarget.Inject"/> and
/// disposed immediately after. It is never logged and never appears in the returned
/// <see cref="AegisInjectionOutcome"/>.
/// </para>
/// </remarks>
public sealed class AegisInjector
{
    private readonly ISecretManagerProvider _secretManagerProvider;
    private readonly IAegisInjectionTarget _target;
    private readonly ILogger<AegisInjector> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AegisInjector"/> class.
    /// </summary>
    public AegisInjector(
        ISecretManagerProvider secretManagerProvider,
        IAegisInjectionTarget target,
        ILogger<AegisInjector>? logger = null)
    {
        _secretManagerProvider = secretManagerProvider ?? throw new ArgumentNullException(nameof(secretManagerProvider));
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _logger = logger ?? NullLogger<AegisInjector>.Instance;
    }

    /// <summary>
    /// Resolves the secret <paramref name="request"/> references, below the boundary, and hands it
    /// to the configured <see cref="IAegisInjectionTarget"/>.
    /// </summary>
    /// <param name="request">The approved request. Must already carry a non-empty
    /// <see cref="ApprovalRequest.SecretManagerName"/>/<see cref="ApprovalRequest.SecretKeyName"/> —
    /// this method never resolves without a reference, and never falls back to a default.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A sanitized <see cref="AegisInjectionOutcome"/> on success, or a fail-loud
    /// SecretResolutionFailed/InjectionFailed <c>AegisResultCodes</c> failure. The secret value
    /// itself is never part of either outcome.</returns>
    public async Task<IGenericResult<AegisInjectionOutcome>> Execute(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SecretManagerName) || string.IsNullOrWhiteSpace(request.SecretKeyName))
            return GenericResult<AegisInjectionOutcome>.Failure(AegisLog.RequiredValueMissing(_logger, nameof(request.SecretManagerName)));

        var managerResult = await _secretManagerProvider.Get(request.SecretManagerName, cancellationToken).ConfigureAwait(false);
        if (!managerResult.IsSuccess || managerResult.Value is null)
            return GenericResult<AegisInjectionOutcome>.Failure(
                AegisLog.SecretResolutionFailed(_logger, request.SecretManagerName, request.SecretKeyName));

        AegisLog.SecretManagerResolved(_logger, request.SecretManagerName);

        var secretResult = await managerResult.Value
            .Execute(GetSecretManagerCommand.Latest(container: null, secretKey: request.SecretKeyName), cancellationToken)
            .ConfigureAwait(false);
        if (!secretResult.IsSuccess || secretResult.Value is null)
            return GenericResult<AegisInjectionOutcome>.Failure(
                AegisLog.SecretResolutionFailed(_logger, request.SecretManagerName, request.SecretKeyName));

        using (secretResult.Value)
        {
            return await _target.Inject(request, secretResult.Value, cancellationToken).ConfigureAwait(false);
        }
    }
}
