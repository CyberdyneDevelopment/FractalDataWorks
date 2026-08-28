using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Aegis;
using Fdw.Aegis.Abstractions;
using Fdw.Aegis.Configuration;
using Fdw.Aegis.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Fdw.Aegis.McpServer;

/// <summary>
/// Exposes the Aegis Gateway's Phase 1 approval + injection pipeline as three MCP tools:
/// <c>list_connections</c>, <c>describe_action</c>, <c>request_action</c>.
/// </summary>
/// <remarks>
/// <para>
/// Non-exposure by construction: no tool method here ever holds, logs, or returns a resolved
/// secret. <see cref="AegisInjector"/> keeps the plaintext <c>SecretValue</c> inside its own
/// <see langword="using"/> block; the only thing that crosses back out to this service (and from
/// here, to Claude) is the sanitized <see cref="Fdw.Aegis.AegisInjectionOutcome"/>.
/// </para>
/// <para>
/// <see cref="ModelContextProtocol"/>'s <c>WithTools&lt;T&gt;()</c> activates a fresh instance of
/// this type per tool call from the current request's DI scope (via
/// <c>ActivatorUtilities.CreateInstance</c>) — this class holds no mutable instance state itself.
/// </para>
/// </remarks>
[McpServerToolType]
public sealed class AegisToolService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IOptions<AegisCommandsOptions> _commands;
    private readonly IApprovalPolicyEvaluator _evaluator;
    private readonly AegisInjector _injector;
    private readonly ILogger<AegisToolService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AegisToolService"/> class.
    /// </summary>
    public AegisToolService(
        IOptions<AegisCommandsOptions> commands,
        IApprovalPolicyEvaluator evaluator,
        AegisInjector injector,
        ILogger<AegisToolService>? logger = null)
    {
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _injector = injector ?? throw new ArgumentNullException(nameof(injector));
        _logger = logger ?? NullLogger<AegisToolService>.Instance;
    }

    [McpServerTool(Name = "list_connections")]
    [Description("List the Aegis commands declared for this host: name, target connection, and approval policy kind. Never includes secret references.")]
    public Task<string> ListConnections(CancellationToken cancellationToken = default)
    {
        AegisLog.ToolInvoked(_logger, "list_connections");

        var payload = _commands.Value.Commands.Select(c => new
        {
            name = c.Name,
            connectionName = c.ConnectionName,
            policy = c.ServiceOptionType,
        }).ToArray();

        AegisLog.ConnectionsListed(_logger, payload.Length);

        return Task.FromResult(JsonSerializer.Serialize(payload, JsonOptions));
    }

    [McpServerTool(Name = "describe_action")]
    [Description("Describe the declared parameter allow-list for one Aegis command. Never emits the secret manager or key name.")]
    public Task<string> DescribeAction(
        [Description("The declared command name (see list_connections).")] string commandName,
        CancellationToken cancellationToken = default)
    {
        AegisLog.ToolInvoked(_logger, "describe_action");

        var command = FindCommand(commandName);
        if (command is null)
            return Task.FromResult(Error($"Command '{commandName}' is not declared. Use list_connections to discover available commands."));

        var (allowList, _, _) = ExtractPolicyDetails(command.Configuration);

        AegisLog.ActionDescribed(_logger, command.Name, allowList.Count);

        var payload = new
        {
            name = command.Name,
            connectionName = command.ConnectionName,
            policy = command.ServiceOptionType,
            secretRequired = true,
            parameters = allowList.Select(p => new
            {
                name = p.ParameterName,
                permittedValues = p.PermittedValues,
                required = p.Required,
            }).ToArray(),
        };

        return Task.FromResult(JsonSerializer.Serialize(payload, JsonOptions));
    }

    [McpServerTool(Name = "request_action")]
    [Description("Request approval and brokered execution of a declared Aegis command. Never returns the resolved secret.")]
    public async Task<string> RequestAction(
        [Description("The declared connection name (see list_connections).")] string connectionName,
        [Description("The declared command name (see list_connections).")] string commandName,
        [Description("JSON object of submitted parameters. Pass '{}' for commands with no parameters.")] string parametersJson,
        CancellationToken cancellationToken = default)
    {
        AegisLog.ToolInvoked(_logger, "request_action");

        var (submitted, parseError) = ParseParameters(parametersJson);
        if (submitted is null)
            return Error(parseError);

        var command = _commands.Value.Commands.FirstOrDefault(c =>
            string.Equals(c.ConnectionName, connectionName, StringComparison.Ordinal)
            && string.Equals(c.Name, commandName, StringComparison.Ordinal));
        if (command is null)
        {
            var message = AegisLog.ConnectionNotDeclared(_logger, connectionName, commandName);
            return Error(message.Message);
        }

        var (allowList, secretManagerName, secretKeyName) = ExtractPolicyDetails(command.Configuration);
        var invalidParameter = FindInvalidParameter(submitted, allowList);
        if (invalidParameter is not null)
        {
            var message = AegisLog.ParameterNotInAllowList(_logger, invalidParameter, commandName);
            return Error(message.Message);
        }

        AegisLog.ParametersValidated(_logger, commandName, submitted.Count);

        var request = new ApprovalRequest
        {
            CorrelationId = Guid.NewGuid(),
            ConnectionName = connectionName,
            CommandName = commandName,
            Parameters = submitted,
            SecretManagerName = secretManagerName,
            SecretKeyName = secretKeyName,
            RequestedAt = DateTimeOffset.UtcNow,
        };

        AegisLog.ActionRequested(_logger, connectionName, commandName, request.CorrelationId);

        var verdictResult = _evaluator.Evaluate(request);
        if (!verdictResult.IsSuccess || verdictResult.Value is null)
        {
            var reason = verdictResult.CurrentMessage;
            return reason is not null
                ? Error(reason)
                : Error(AegisLog.ActionDenied(_logger, commandName, "approval evaluation failed without a message").Message);
        }

        var verdict = verdictResult.Value;

        AegisLog.VerdictReached(_logger, verdict.Disposition.Name, commandName, request.CorrelationId);

        if (!verdict.Disposition.AllowsInjection)
        {
            var denyPayload = new
            {
                success = false,
                disposition = verdict.Disposition.Name,
                reason = verdict.Reason,
            };
            return JsonSerializer.Serialize(denyPayload, JsonOptions);
        }

        AegisLog.InjectionStarting(_logger, commandName, request.CorrelationId);

        var injectionResult = await _injector.Execute(request, cancellationToken).ConfigureAwait(false);
        if (!injectionResult.IsSuccess || injectionResult.Value is null)
        {
            var reason = injectionResult.CurrentMessage;
            return reason is not null
                ? Error(reason)
                : Error(AegisLog.InjectionFailed(_logger, commandName, "injection reported failure without a message").Message);
        }

        AegisLog.InjectionSucceeded(_logger, commandName, injectionResult.Value.CorrelationId);

        var successPayload = new
        {
            success = true,
            correlationId = injectionResult.Value.CorrelationId,
            @ref = injectionResult.Value.Reference,
        };

        return JsonSerializer.Serialize(successPayload, JsonOptions);
    }

    /// <summary>
    /// Parses the raw parametersJson from the MCP boundary into a parameter dictionary.
    /// </summary>
    /// <returns>
    /// The parsed parameters, or null with a caller-returnable reason when the input is not a usable
    /// JSON object.
    /// </returns>
    private static (Dictionary<string, object?>? Parameters, string Error) ParseParameters(string parametersJson)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                string.IsNullOrWhiteSpace(parametersJson) ? "{}" : parametersJson, JsonOptions);
            return parsed is null
                ? (null, "parametersJson must be a JSON object.")
                : (parsed, string.Empty);
        }
        catch (JsonException ex)
        {
            return (null, $"Invalid parametersJson: {ex.Message}");
        }
    }

    private AegisCommandConfiguration? FindCommand(string commandName) =>
        _commands.Value.Commands.FirstOrDefault(c => string.Equals(c.Name, commandName, StringComparison.Ordinal));

    private static (IReadOnlyList<ParameterAllowEntry> AllowList, string? SecretManagerName, string? SecretKeyName) ExtractPolicyDetails(
        IApprovalPolicyConfiguration? configuration) => configuration switch
        {
            PreApprovedCommandConfiguration pre => (pre.ParameterAllowList.ToList(), pre.SecretManagerName, pre.SecretKeyName),
            AdHocCommandConfiguration adHoc => (Array.Empty<ParameterAllowEntry>(), adHoc.SecretManagerName, adHoc.SecretKeyName),
            _ => (Array.Empty<ParameterAllowEntry>(), null, null),
        };

    private static string? FindInvalidParameter(
        Dictionary<string, object?> submitted,
        IReadOnlyList<ParameterAllowEntry> allowList)
    {
        foreach (var kvp in submitted)
        {
            var entry = allowList.FirstOrDefault(e => string.Equals(e.ParameterName, kvp.Key, StringComparison.Ordinal));

            if (entry is null || kvp.Value is null)
                return kvp.Key;

            if (kvp.Value.ToString() is not { } value || !entry.PermittedValues.Contains(value, StringComparer.Ordinal))
                return kvp.Key;
        }

        foreach (var entry in allowList)
        {
            if (entry.Required && !submitted.ContainsKey(entry.ParameterName))
                return entry.ParameterName;
        }

        return null;
    }

    private static string Error(string message) =>
        JsonSerializer.Serialize(new { success = false, error = message }, JsonOptions);
}
