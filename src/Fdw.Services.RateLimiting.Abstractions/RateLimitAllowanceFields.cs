using System;
using System.Collections.Generic;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// The settings every rate limit policy's allowance is read from, and the one place their names are made.
/// </summary>
/// <remarks>
/// A policy owns one setting for each field here, named <c>RateLimit.{policy}.{field}</c>. The platform's settings registry and
/// the limiter that reads them both take the names from here, so a policy added to <see cref="RateLimitPolicies"/> has its
/// settings with no other change (FDW-1945).
/// </remarks>
public static class RateLimitAllowanceFields
{
    /// <summary>The prefix of every rate limit setting's name.</summary>
    public const string Prefix = "RateLimit";

    /// <summary>The most requests allowed in a window.</summary>
    public static readonly RateLimitAllowanceField RequestsPerWindow = new RateLimitAllowanceField(
        "RequestsPerWindow", "Int32", "The most requests one caller may make in a window under this policy; must be positive.");

    /// <summary>How long a window lasts, in seconds.</summary>
    public static readonly RateLimitAllowanceField WindowSeconds = new RateLimitAllowanceField(
        "WindowSeconds", "Int32", "How long a window lasts under this policy, in seconds; must be positive.");

    /// <summary>Whether a burst above the limit is allowed.</summary>
    public static readonly RateLimitAllowanceField AllowBurst = new RateLimitAllowanceField(
        "AllowBurst", "Boolean", "Whether a caller may burst above the limit under this policy, up to the burst limit.");

    /// <summary>The most requests a burst allows.</summary>
    public static readonly RateLimitAllowanceField BurstLimit = new RateLimitAllowanceField(
        "BurstLimit", "Int32", "The most requests a burst allows under this policy; not below the limit.");

    /// <summary>The algorithm that counts requests.</summary>
    public static readonly RateLimitAllowanceField Algorithm = new RateLimitAllowanceField(
        "Algorithm", "String", "How requests are counted under this policy: FixedWindow, SlidingWindow, TokenBucket or Concurrency.");

    /// <summary>The segments a sliding window is cut into.</summary>
    public static readonly RateLimitAllowanceField SegmentsPerWindow = new RateLimitAllowanceField(
        "SegmentsPerWindow", "Int32", "The segments a sliding window is cut into under this policy; used by SlidingWindow only, must be positive.");

    /// <summary>Whether requests over the limit wait.</summary>
    public static readonly RateLimitAllowanceField QueueExceededRequests = new RateLimitAllowanceField(
        "QueueExceededRequests", "Boolean", "Whether a request over the limit waits for room under this policy instead of being refused.");

    /// <summary>The most requests that wait.</summary>
    public static readonly RateLimitAllowanceField QueueLimit = new RateLimitAllowanceField(
        "QueueLimit", "Int32", "The most requests that wait under this policy when they do; not below zero.");

    /// <summary>How requests are divided into buckets.</summary>
    public static readonly RateLimitAllowanceField Partitioning = new RateLimitAllowanceField(
        "Partitioning", "String", "How callers are counted under this policy: PerCaller (each caller has its own allowance), PerAddress (each address) or Shared (everyone one allowance).");

    /// <summary>Gets every field, in the order they are read.</summary>
    public static IReadOnlyList<RateLimitAllowanceField> All { get; } = new[]
    {
        RequestsPerWindow,
        WindowSeconds,
        AllowBurst,
        BurstLimit,
        Algorithm,
        SegmentsPerWindow,
        QueueExceededRequests,
        QueueLimit,
        Partitioning,
    };

    /// <summary>
    /// Makes the name of the setting a policy's field is read from.
    /// </summary>
    /// <param name="policyName">The policy's name.</param>
    /// <param name="field">The field.</param>
    /// <returns>The setting's name: <c>RateLimit.{policy}.{field}</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the policy's name or the field is null.</exception>
    public static string SettingName(string policyName, RateLimitAllowanceField field)
    {
        if (policyName is null)
        {
            throw new ArgumentNullException(nameof(policyName));
        }

        if (field is null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        return Prefix + "." + policyName + "." + field.Name;
    }
}
