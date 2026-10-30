using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fdw.Operations.Endpoints.ConfigurationMetadata;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Reads the settings a caller sends for a secret manager type into that type's own configuration (FDW-2890): the one reader create and update share.
/// </summary>
/// <remarks>
/// A setting the type does not take is refused, naming it and listing those it takes: a member that is accepted and ignored tells a person a change was
/// saved when it was not. An update lays the sent settings over the stored ones, so a member left out keeps its value.
/// </remarks>
public static class SecretManagerSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Reads the sent settings as the named type's configuration.</summary>
    /// <param name="typeName">The secret manager type the settings are for.</param>
    /// <param name="sent">The settings the caller sent.</param>
    /// <param name="stored">The settings already stored, laid under the sent ones; none for a new secret manager.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The configuration, or a coded refusal that names the type, the setting or the reason.</returns>
    public static IGenericResult<ISecretManagerImplementationConfiguration> Read(
        string typeName, JsonElement? sent, ISecretManagerImplementationConfiguration? stored, ILogger logger)
    {
        var option = SecretManagerTypes.ByName(typeName);
        if (ReferenceEquals(option, SecretManagerTypes.NotFound))
        {
            return GenericResult<ISecretManagerImplementationConfiguration>.Failure(SecretManagerEndpointLog.SecretManagerTypeUnknown(
                logger, typeName, string.Join(", ", SecretManagerTypes.All().Select(known => known.Value.Name))));
        }

        if (sent is not { ValueKind: JsonValueKind.Object } settings)
        {
            return Unreadable(typeName, "send the settings as a JSON object", logger);
        }

        var merged = Merge(option.ConfigurationType, typeName, settings, stored, logger);
        return merged.IsFailure || merged.Value is null
            ? merged.ToNewResult<ISecretManagerImplementationConfiguration>()
            : Deserialize(option.ConfigurationType, typeName, merged.Value, logger);
    }

    // The stored settings, if any, with each sent member laid over them. A sent member the type does not take is refused.
    private static IGenericResult<JsonObject> Merge(
        Type configuration, string typeName, JsonElement settings, ISecretManagerImplementationConfiguration? stored, ILogger logger)
    {
        var body = stored is null ? new JsonObject() : JsonSerializer.SerializeToNode(stored, configuration, JsonOptions) as JsonObject;
        if (body is null)
        {
            return GenericResult<JsonObject>.Failure(SecretManagerEndpointLog.SecretManagerSettingsUnreadable(
                logger, typeName, "the stored settings could not be read"));
        }

        var takes = Takes(configuration);
        foreach (var member in settings.EnumerateObject())
        {
            var taken = takes.FirstOrDefault(name => string.Equals(name, member.Name, StringComparison.OrdinalIgnoreCase));
            if (taken is null)
            {
                return GenericResult<JsonObject>.Failure(SecretManagerEndpointLog.SecretManagerSettingUnknown(
                    logger, typeName, member.Name, string.Join(", ", takes)));
            }

            // Why: a client that read the settings back holds the mask in place of each secret, and sends it again to mean "keep what is stored"
            // (as for a connection), so it never replaces a stored secret.
            if (IsMask(member.Value))
            {
                continue;
            }

            if (CannotBeCleared(configuration, taken, member.Value))
            {
                return GenericResult<JsonObject>.Failure(SecretManagerEndpointLog.SecretManagerSettingNotClearable(logger, typeName, taken));
            }

            body[taken] = JsonNode.Parse(member.Value.GetRawText());
        }

        return GenericResult<JsonObject>.Success(body);
    }

    private static bool IsMask(JsonElement value)
        => value.ValueKind == JsonValueKind.String && string.Equals(value.GetString(), ConnectionSecretMask.Value, StringComparison.Ordinal);

    // A null for a setting that always has a value cannot be stored as null: the stored value would come back unchanged while the caller was told
    // it was cleared. The setting's description (canBeEmpty) is built by the same rule.
    private static bool CannotBeCleared(Type configuration, string setting, JsonElement value)
        => value.ValueKind == JsonValueKind.Null
           && !ConfigurationPropertyExtractor.CanBeEmpty(configuration.GetProperty(setting, BindingFlags.Public | BindingFlags.Instance)!);

    private static IGenericResult<ISecretManagerImplementationConfiguration> Deserialize(Type configuration, string typeName, JsonObject body, ILogger logger)
    {
        try
        {
            return body.Deserialize(configuration, JsonOptions) is ISecretManagerImplementationConfiguration read
                ? GenericResult<ISecretManagerImplementationConfiguration>.Success(read)
                : Unreadable(typeName, "no settings were read", logger);
        }
        catch (JsonException unreadable)
        {
            return Unreadable(typeName, Reason(unreadable), logger);
        }
    }

    private static IGenericResult<ISecretManagerImplementationConfiguration> Unreadable(string typeName, string reason, ILogger logger)
        => GenericResult<ISecretManagerImplementationConfiguration>.Failure(SecretManagerEndpointLog.SecretManagerSettingsUnreadable(logger, typeName, reason));

    // The setting the reader stopped at, when it names one, then what it said.
    private static string Reason(JsonException unreadable)
        => string.IsNullOrEmpty(unreadable.Path) ? unreadable.Message : $"{unreadable.Path}: {unreadable.Message}";

    /// <summary>
    /// The settings a manager is set to, as a client reads them back: each setting the kind takes, with every secret one shown as the mask when
    /// it holds a value (and null when it does not), never as its value.
    /// </summary>
    /// <param name="option">The manager's kind.</param>
    /// <param name="stored">The manager's stored settings.</param>
    /// <param name="logger">Logger for the refusal.</param>
    /// <returns>The settings, or the failure when they cannot be read.</returns>
    public static IGenericResult<JsonObject> Shown(ISecretManagerType option, ISecretManagerImplementationConfiguration stored, ILogger logger)
    {
        if (JsonSerializer.SerializeToNode(stored, option.ConfigurationType, JsonOptions) is not JsonObject body)
        {
            return GenericResult<JsonObject>.Failure(SecretManagerEndpointLog.SecretManagerSettingsUnreadable(
                logger, option.Name, "the stored settings could not be read"));
        }

        var secrets = ConfigurationPropertyExtractor.ExtractProperties(option.ConfigurationType)
            .Where(property => property.IsSecret)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        var shown = new JsonObject();
        foreach (var name in Takes(option.ConfigurationType).Where(taken => body.ContainsKey(taken)))
        {
            shown[name] = secrets.Contains(name) && body[name] is not null
                ? JsonValue.Create(ConnectionSecretMask.Value)
                : body[name]?.DeepClone();
        }

        return GenericResult<JsonObject>.Success(shown);
    }

    /// <summary>
    /// The settings a kind takes: the members of its configuration class that can be set, other than the ones every implementation carries and the
    /// system fills. POST and PATCH accept exactly these, and the types route describes exactly these.
    /// </summary>
    /// <param name="configuration">The kind's configuration class.</param>
    /// <returns>The member names, ordered.</returns>
    public static IReadOnlyList<string> Takes(Type configuration)
    {
        var system = typeof(ISecretManagerImplementationConfiguration).GetProperties()
            .Concat(typeof(ISecretManagerImplementationConfiguration).GetInterfaces().SelectMany(contract => contract.GetProperties()))
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        return [.. configuration.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanWrite && !system.Contains(property.Name))
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)];
    }
}
