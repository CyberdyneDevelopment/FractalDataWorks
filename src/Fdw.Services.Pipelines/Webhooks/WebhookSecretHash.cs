using System;

namespace Fdw.Services.Pipelines.Webhooks;

/// <summary>What is stored of a webhook trigger's secret (FDW-1884): its hash, the first characters, and when it was made.</summary>
/// <param name="Hash">The SHA-256 of the secret as lowercase hexadecimal.</param>
/// <param name="Prefix">The first characters of the secret.</param>
/// <param name="CreatedAt">When the secret was made.</param>
public sealed record WebhookSecretHash(string Hash, string Prefix, DateTimeOffset CreatedAt);
