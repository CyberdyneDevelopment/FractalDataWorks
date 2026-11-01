using Fdw.Services.RateLimiting.Abstractions;

namespace Fdw.Services.RateLimiting.Tests;

/// <summary>
/// The numbers and names each tier was held to when they were constants in the policy classes (before FDW-1945), which is what
/// each host's baseline runtime script seeds as settings: <c>databases-seed/ConfigurationDb/seed/08-seed-baseline-runtime.sql</c>
/// and <c>databases-seed/TechCom/08-techcom-baseline-runtime.sql</c>. A change here is a change to those scripts.
/// </summary>
internal static class TodaysRateLimitSettings
{
    /// <summary>One tier's settings, as text.</summary>
    internal sealed class Tier
    {
        internal Tier(string policy, int requests, int seconds, bool burst, int burstLimit, string algorithm, int segments, string partitioning)
        {
            Policy = policy;
            Requests = requests;
            Seconds = seconds;
            Burst = burst;
            BurstLimit = burstLimit;
            Algorithm = algorithm;
            Segments = segments;
            Partitioning = partitioning;
        }

        internal string Policy { get; }

        internal int Requests { get; }

        internal int Seconds { get; }

        internal bool Burst { get; }

        internal int BurstLimit { get; }

        internal string Algorithm { get; }

        internal int Segments { get; }

        internal string Partitioning { get; }
    }

    /// <summary>The four tiers.</summary>
    internal static readonly Tier[] Tiers =
    {
        new Tier("Authenticated", 500, 60, true, 750, "SlidingWindow", 10, "PerCaller"),
        new Tier("Premium", 2000, 60, true, 3000, "TokenBucket", 1, "PerCaller"),
        new Tier("Admin", 10000, 60, true, 15000, "TokenBucket", 1, "PerCaller"),
        new Tier("Standard", 100, 60, false, 100, "SlidingWindow", 10, "PerAddress"),
    };

    /// <summary>Makes the settings with every tier's numbers set, none queued.</summary>
    /// <returns>The settings.</returns>
    internal static FakeEffectiveSettings All()
    {
        var settings = new FakeEffectiveSettings();
        foreach (var tier in Tiers)
        {
            Set(settings, tier.Policy, RateLimitAllowanceFields.RequestsPerWindow, tier.Requests.ToString());
            Set(settings, tier.Policy, RateLimitAllowanceFields.WindowSeconds, tier.Seconds.ToString());
            Set(settings, tier.Policy, RateLimitAllowanceFields.AllowBurst, tier.Burst.ToString());
            Set(settings, tier.Policy, RateLimitAllowanceFields.BurstLimit, tier.BurstLimit.ToString());
            Set(settings, tier.Policy, RateLimitAllowanceFields.Algorithm, tier.Algorithm);
            Set(settings, tier.Policy, RateLimitAllowanceFields.SegmentsPerWindow, tier.Segments.ToString());
            Set(settings, tier.Policy, RateLimitAllowanceFields.QueueExceededRequests, bool.FalseString);
            Set(settings, tier.Policy, RateLimitAllowanceFields.QueueLimit, "0");
            Set(settings, tier.Policy, RateLimitAllowanceFields.Partitioning, tier.Partitioning);
        }

        return settings;
    }

    /// <summary>Sets one of a tier's settings.</summary>
    /// <param name="settings">The settings.</param>
    /// <param name="policy">The tier's name.</param>
    /// <param name="field">The field.</param>
    /// <param name="value">The value, as text.</param>
    internal static void Set(FakeEffectiveSettings settings, string policy, RateLimitAllowanceField field, string value)
        => settings.Set(RateLimitAllowanceFields.SettingName(policy, field), value);
}
