using System;
using System.Globalization;
using Fdw.Messages;
using Fdw.Services.RateLimiting.Abstractions;
using Fdw.Services.RateLimiting.Logging;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.RateLimiting;

/// <summary>
/// Reads the settings of one policy's allowance one after another, keeping the first one that cannot be used.
/// </summary>
/// <remarks>
/// Why it keeps the refusal instead of returning at the first one: the allowance is built from nine settings in one expression,
/// and the first setting that fails is the one named. Once one has failed no later setting is read, so the log carries one line
/// that names the one to fix.
/// </remarks>
internal sealed class RateLimitAllowanceReader
{
    private readonly IEffectiveSettingsProvider _settings;
    private readonly IRateLimitPolicy _policy;
    private readonly Guid? _tenantId;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitAllowanceReader"/> class.
    /// </summary>
    /// <param name="settings">The settings the allowance is resolved from.</param>
    /// <param name="policy">The policy whose allowance is read.</param>
    /// <param name="tenantId">The company the request belongs to, or null for a request that belongs to none.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    internal RateLimitAllowanceReader(IEffectiveSettingsProvider settings, IRateLimitPolicy policy, Guid? tenantId, ILogger logger)
    {
        _settings = settings;
        _policy = policy;
        _tenantId = tenantId;
        _logger = logger;
    }

    /// <summary>Gets the first setting that could not be used, or null while every one read so far could.</summary>
    internal IGenericMessage? Refusal { get; private set; }

    /// <summary>Reads a whole number above zero.</summary>
    /// <param name="field">The field.</param>
    /// <returns>The number, or zero when the setting could not be used.</returns>
    internal int Positive(RateLimitAllowanceField field) => Number(field, 1, "a whole number above zero");

    /// <summary>Reads a whole number, zero or more.</summary>
    /// <param name="field">The field.</param>
    /// <returns>The number, or zero when the setting could not be used.</returns>
    internal int NotNegative(RateLimitAllowanceField field) => Number(field, 0, "a whole number, zero or more");

    /// <summary>Reads a number of seconds above zero.</summary>
    /// <param name="field">The field.</param>
    /// <returns>The time, or zero when the setting could not be used.</returns>
    internal TimeSpan Seconds(RateLimitAllowanceField field) => TimeSpan.FromSeconds(Positive(field));

    /// <summary>Reads a true or false.</summary>
    /// <param name="field">The field.</param>
    /// <returns>The value, or false when the setting could not be used.</returns>
    internal bool Flag(RateLimitAllowanceField field) => Read<bool>(field);

    /// <summary>Reads the name of an option of a collection and resolves it.</summary>
    /// <typeparam name="T">The collection's option type.</typeparam>
    /// <param name="field">The field.</param>
    /// <param name="lookup">The collection's lookup by name, which answers its not-found option for a name it does not hold.</param>
    /// <param name="notFound">The collection's not-found option.</param>
    /// <returns>The option, or the not-found option when the setting could not be used.</returns>
    internal T Named<T>(RateLimitAllowanceField field, Func<string, T> lookup, T notFound)
        where T : class
    {
        var name = Read<string>(field);
        if (Refusal is not null)
        {
            return notFound;
        }

        var found = lookup(name);
        if (ReferenceEquals(found, notFound))
        {
            Refuse(field, name, "the name of one of its options");
        }

        return found;
    }

    private int Number(RateLimitAllowanceField field, int lowest, string rule)
    {
        var value = Read<int>(field);
        if (Refusal is null && value < lowest)
        {
            Refuse(field, value.ToString(CultureInfo.InvariantCulture), rule);
        }

        return value;
    }

    private T Read<T>(RateLimitAllowanceField field)
    {
        if (Refusal is not null)
        {
            return default!;
        }

        if (_settings.TryGetEffectiveValue<T>(RateLimitAllowanceFields.SettingName(_policy.Name, field), out var value, _tenantId))
        {
            return value;
        }

        Refusal = RateLimitLog.AllowanceSettingUnreadable(_logger, _policy.Name, RateLimitAllowanceFields.SettingName(_policy.Name, field));
        return default!;
    }

    private void Refuse(RateLimitAllowanceField field, string value, string rule)
        => Refusal = RateLimitLog.AllowanceSettingRefused(
            _logger, _policy.Name, RateLimitAllowanceFields.SettingName(_policy.Name, field), value, rule);
}
