using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Results;
using Fdw.Services.Messaging.Logging;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging;

/// <summary>
/// Says which permission answers the questions left on a screen, from the setting <see cref="SettingDefinitions.UiFeedbackQuestionAnswerers"/>: the longest
/// route prefix that matches the screen's route wins, and a route no prefix matches has no answerers.
/// </summary>
/// <remarks>
/// Who can answer is configuration, never code: the setting is read for the comment's tenant, so the server's value applies and a tenant may state its own.
/// A deployment with no setting has no answerers. A host that registers no settings, or a value that is not a list of route prefixes and permissions, is a
/// failure the caller reports; it is never read as "no answerers". Each tenant's value is read once per instance (the service is scoped).
/// </remarks>
internal sealed class UiFeedbackAnswerers
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private readonly ILogger _logger;
    private readonly Lazy<IEffectiveSettingsProvider>? _settings;
    private readonly Dictionary<Guid, IGenericResult<IReadOnlyList<UiFeedbackAnswererRoute>>> _read = [];

    /// <summary>Initializes a new instance of the <see cref="UiFeedbackAnswerers"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="settings">The host's settings; <see langword="null"/> in a host that registers none.</param>
    public UiFeedbackAnswerers(ILogger logger, Lazy<IEffectiveSettingsProvider>? settings)
    {
        _logger = logger;
        _settings = settings;
    }

    /// <summary>Finds the permission that answers for a route.</summary>
    /// <param name="route">The route pattern of the screen the question was asked on.</param>
    /// <param name="tenantId">The tenant of the question, whose value of the setting applies.</param>
    /// <returns>The permission, <see langword="null"/> when no prefix matches or there is no setting, or the failure to read the setting.</returns>
    public IGenericResult<string?> PermissionFor(string route, Guid tenantId)
    {
        var routes = Routes(route, tenantId);
        if (!routes.IsSuccess)
            return routes.ToNewResult<string?>();

        var match = routes.Value!
            .Where(entry => Matches(entry.RoutePrefix!, route))
            .OrderByDescending(entry => Trimmed(entry.RoutePrefix!).Length)
            .FirstOrDefault();
        return GenericResult<string?>.Success(match?.Permission);
    }

    // Why whole segments: "/data" answers for "/data" and "/data/sources", not for "/datasets", which is another screen with its own entry.
    private static bool Matches(string prefix, string route)
    {
        var trimmed = Trimmed(prefix);
        return string.Equals(trimmed, "/", StringComparison.Ordinal)
               || string.Equals(route, trimmed, StringComparison.Ordinal)
               || route.StartsWith(trimmed + "/", StringComparison.Ordinal);
    }

    // Why: "/documents/" and "/documents" are the same prefix; "/" alone stays "/" and answers for every screen.
    private static string Trimmed(string prefix) => prefix.Length > 1 ? prefix.TrimEnd('/') : prefix;

    private IGenericResult<IReadOnlyList<UiFeedbackAnswererRoute>> Routes(string route, Guid tenantId)
    {
        if (_read.TryGetValue(tenantId, out var known))
            return known;

        var read = Read(route, tenantId);
        _read[tenantId] = read;
        return read;
    }

    private IGenericResult<IReadOnlyList<UiFeedbackAnswererRoute>> Read(string route, Guid tenantId)
    {
        const string setting = SettingDefinitions.UiFeedbackQuestionAnswerers;
        if (_settings is null)
            return GenericResult<IReadOnlyList<UiFeedbackAnswererRoute>>.Failure(
                MessagingLog.UiFeedbackAnswerersUnreadable(_logger, route, setting, "cannot be read: the host registers no settings"));

        string? value;
        try
        {
            if (!_settings.Value.TryGetEffectiveValue(setting, out value, tenantId) || string.IsNullOrWhiteSpace(value))
                return GenericResult<IReadOnlyList<UiFeedbackAnswererRoute>>.Success([]);
        }
        catch (InvalidOperationException ex)
        {
            return GenericResult<IReadOnlyList<UiFeedbackAnswererRoute>>.Failure(MessagingLog.UiFeedbackAnswerersUnreadable(_logger, route, setting, "could not be read: " + ex.Message));
        }

        List<UiFeedbackAnswererRoute>? entries;
        try
        {
            entries = JsonSerializer.Deserialize<List<UiFeedbackAnswererRoute>>(value, Json);
        }
        catch (JsonException ex)
        {
            return GenericResult<IReadOnlyList<UiFeedbackAnswererRoute>>.Failure(MessagingLog.UiFeedbackAnswerersSettingInvalid(_logger, setting, ex.Message));
        }

        if (entries is null)
            return GenericResult<IReadOnlyList<UiFeedbackAnswererRoute>>.Failure(MessagingLog.UiFeedbackAnswerersSettingInvalid(_logger, setting, "it is null"));

        return Problem(entries) is { } problem
            ? GenericResult<IReadOnlyList<UiFeedbackAnswererRoute>>.Failure(MessagingLog.UiFeedbackAnswerersSettingInvalid(_logger, setting, problem))
            : GenericResult<IReadOnlyList<UiFeedbackAnswererRoute>>.Success(entries);
    }

    private static string? Problem(List<UiFeedbackAnswererRoute> entries)
    {
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            if (entry is null)
                return $"entry {index} is null";

            if (string.IsNullOrWhiteSpace(entry.RoutePrefix) || !entry.RoutePrefix.StartsWith('/'))
                return $"entry {index} has no routePrefix that starts with '/'";

            if (string.IsNullOrWhiteSpace(entry.Permission))
                return $"entry {index} ({entry.RoutePrefix}) has no permission";
        }

        var repeated = entries
            .GroupBy(entry => Trimmed(entry.RoutePrefix!), StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        return repeated is null ? null : $"the routePrefix {repeated.Key} is stated more than once";
    }
}
