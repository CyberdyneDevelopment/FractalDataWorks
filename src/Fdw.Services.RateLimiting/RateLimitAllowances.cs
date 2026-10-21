using System;
using Fdw.Results;
using Fdw.Services.RateLimiting.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.RateLimiting;

/// <summary>
/// Reads what a rate limit policy holds its callers to from the platform's settings (FDW-1945), for the company the request
/// belongs to (FDW-1946).
/// </summary>
/// <remarks>
/// <para>
/// One reader for every request. A setting that is not set, or holds a value the limiter cannot use, is a refusal that names
/// it and never a made-up number: an absent setting must not read as no limit, nor as some other limit. Which values the
/// settings hold is each host's baseline runtime script; which an operator changed is the settings page.
/// </para>
/// <para>
/// A company's own value for a setting wins over the server's, unless the server's value is locked. That layering is the
/// platform's own (<see cref="IEffectiveSettingsProvider"/>), so a company that set nothing is held to the server's number and
/// nothing here chooses between the two. A request that belongs to no company is held to the server's.
/// </para>
/// </remarks>
public static class RateLimitAllowances
{
    /// <summary>
    /// Reads a policy's allowance.
    /// </summary>
    /// <param name="settings">The settings the allowance is resolved from.</param>
    /// <param name="policy">The policy whose allowance is read.</param>
    /// <param name="tenantId">The company the request belongs to, or null for a request that belongs to none.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The allowance, or the refusal naming the first setting that cannot be used.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the settings, the policy or the logger is null.</exception>
    public static IGenericResult<RateLimitAllowance> Read(IEffectiveSettingsProvider settings, IRateLimitPolicy policy, Guid? tenantId, ILogger logger)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        var reader = new RateLimitAllowanceReader(settings, policy, tenantId, logger);
        var allowance = new RateLimitAllowance(
            reader.Positive(RateLimitAllowanceFields.RequestsPerWindow),
            reader.Seconds(RateLimitAllowanceFields.WindowSeconds),
            reader.Flag(RateLimitAllowanceFields.AllowBurst),
            reader.Positive(RateLimitAllowanceFields.BurstLimit),
            reader.Named(RateLimitAllowanceFields.Algorithm, RateLimitAlgorithms.ByName, RateLimitAlgorithms.NotFound),
            reader.Positive(RateLimitAllowanceFields.SegmentsPerWindow),
            reader.Flag(RateLimitAllowanceFields.QueueExceededRequests),
            reader.NotNegative(RateLimitAllowanceFields.QueueLimit),
            reader.Named(RateLimitAllowanceFields.Partitioning, RateLimitPartitionings.ByName, RateLimitPartitionings.NotFound));

        return reader.Refusal is { } refusal
            ? GenericResult<RateLimitAllowance>.Failure(refusal)
            : GenericResult<RateLimitAllowance>.Success(allowance);
    }
}
