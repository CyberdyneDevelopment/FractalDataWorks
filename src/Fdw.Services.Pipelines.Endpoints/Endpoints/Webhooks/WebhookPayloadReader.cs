using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Webhooks;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Webhooks;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Turns a webhook trigger in a request into the stored form, after checking it.</summary>
public static class WebhookPayloadReader
{
    /// <summary>The domain every webhook trigger belongs to.</summary>
    public const string DomainName = "WebhookTrigger";

    /// <summary>The one implementation a webhook trigger has.</summary>
    public const string ImplementationName = "Webhook";

    /// <summary>Reads the payload as the stored trigger of the identity given.</summary>
    /// <param name="payload">The trigger in the request.</param>
    /// <param name="id">The identity the stored trigger takes: a new one, or the one a save replaces.</param>
    /// <param name="pipeline">The pipeline the trigger starts, as stored.</param>
    /// <param name="roles">The run roles, already judged against what the caller holds.</param>
    /// <param name="secret">What is stored of the secret: the new one, or the one a save keeps.</param>
    /// <returns>The stored trigger, or the refusal that names each problem.</returns>
    public static IGenericResult<WebhookTriggerConfiguration> Read(
        WebhookPayload payload, Guid id, IPipelineImplementationConfiguration pipeline, IReadOnlyList<string> roles, WebhookSecretHash secret)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(secret);
        var problems = WebhookChecker.Problems(payload, DeclaredNames(pipeline));
        if (problems.Count > 0 || payload.Enabled is not { } enabled)
        {
            return WebhookRefusal.Of<WebhookTriggerConfiguration>(string.Join("; ", problems));
        }

        var trigger = new WebhookTriggerConfiguration
        {
            Id = id,
            Name = payload.Name,
            Domain = DomainName,
            Implementation = ImplementationName,
            PipelineId = pipeline.Id,
            PipelineName = pipeline.Name,
            IsEnabled = enabled,
            Description = payload.Description,
            SecretHash = secret.Hash,
            SecretPrefix = secret.Prefix,
            SecretCreatedAt = secret.CreatedAt,
        };
        trigger.ReplaceRunRoles(roles);
        var ordinal = 0;
        foreach (var map in payload.Parameters)
        {
            trigger.ParameterMaps.Add(new WebhookTriggerParameterMapConfiguration
            {
                Ordinal = ordinal++, BodyPath = map.BodyPath, ParameterName = map.Parameter,
            });
        }

        return GenericResult<WebhookTriggerConfiguration>.Success(trigger);
    }

    private static string[] DeclaredNames(IPipelineImplementationConfiguration pipeline) =>
        pipeline is IDeclaresRunParameters declaring ? [.. declaring.DeclaredRunParameters.Select(parameter => parameter.Name)] : [];
}
