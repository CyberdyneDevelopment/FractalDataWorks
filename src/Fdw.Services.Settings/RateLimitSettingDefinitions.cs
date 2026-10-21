using System;
using System.Collections.Generic;
using Fdw.Services.RateLimiting.Abstractions;

namespace Fdw.Services.Settings;

/// <summary>
/// The settings every rate limit policy's allowance is read from, as definitions the registry knows (FDW-1945).
/// </summary>
/// <remarks>
/// They are made from <see cref="RateLimitPolicies"/> and <see cref="RateLimitAllowanceFields"/> and not listed by name in
/// <see cref="SettingDefinitions"/>, so a policy added to the collection has its settings here with no other change. The
/// definitions are only what lets the settings page create a row for a setting that has none yet: the values themselves are
/// rows in each host's baseline runtime script, and a request under a policy whose setting is absent is refused rather than
/// given a number.
/// </remarks>
internal static class RateLimitSettingDefinitions
{
    private static readonly Dictionary<string, SettingDefinition> ByName = Build();

    /// <summary>
    /// Attempts to resolve the definition of a rate limit setting by name.
    /// </summary>
    /// <param name="name">The setting's name: <c>RateLimit.{policy}.{field}</c>.</param>
    /// <param name="definition">The definition, when the name is one.</param>
    /// <returns><see langword="true"/> when the name is a rate limit setting of a policy in <see cref="RateLimitPolicies"/>.</returns>
    internal static bool TryGet(string name, out SettingDefinition definition)
        => ByName.TryGetValue(name, out definition);

    private static Dictionary<string, SettingDefinition> Build()
    {
        var all = new Dictionary<string, SettingDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var policy in RateLimitPolicies.All())
        {
            foreach (var field in RateLimitAllowanceFields.All)
            {
                all[RateLimitAllowanceFields.SettingName(policy.Name, field)] = new SettingDefinition(field.DataType, field.Description);
            }
        }

        return all;
    }
}
