using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections;

/// <summary>
/// The default <see cref="IConnectionEndpointBehavior"/>: binds, merges and shows a connection type's
/// configuration straight from its configuration class, with no per-type mapping code.
/// </summary>
/// <remarks>
/// <para>
/// Same approach as the secret manager and calculation endpoints: resolve the option, then read the JSON
/// into the option's configuration type. A connection type adds only what is genuinely its own, through
/// <see cref="Alias"/>, <see cref="MaskEntries"/> and <see cref="Validate"/>, called from its constructor
/// (see <see cref="ConnectionTypeBase{TService,TFactory,TConfiguration}"/>).
/// </para>
/// <para>
/// Merge works on JSON: the stored configuration is written out, the change is laid over it (nested
/// objects and dictionaries merge member by member, anything else is replaced, a masked value keeps what is
/// stored), and the result is read back. So a type needs no merge code for a dictionary it wants merged
/// rather than replaced.
/// </para>
/// </remarks>
public sealed class ConnectionEndpointBehavior : IConnectionEndpointBehavior<IConnectionImplementationConfiguration>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new CaseInsensitiveStringDictionaryConverter() },
    };

    // Why: every connection has these. They are the endpoint's own top-level fields (name, health checks,
    // discovery), not a connection type's settings, so they are left out of what a type shows.
    private static readonly HashSet<string> SharedMembers = new(
        typeof(IConnectionImplementationConfiguration).GetInterfaces()
            .Append(typeof(IConnectionImplementationConfiguration))
            .SelectMany(contract => contract.GetProperties())
            .Select(property => JsonNamingPolicy.CamelCase.ConvertName(property.Name)),
        StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Func<IConnectionImplementationConfiguration, IReadOnlyDictionary<string, string?>, IDictionary<string, string?>>> _entryMasks =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Func<IConnectionImplementationConfiguration, ILogger, IGenericResult>> _rules = [];

    /// <summary>Initializes a new instance of the <see cref="ConnectionEndpointBehavior"/> class.</summary>
    /// <param name="configurationType">The connection type's configuration class.</param>
    public ConnectionEndpointBehavior(Type configurationType)
    {
        ConfigurationType = configurationType;
    }

    /// <inheritdoc />
    public Type ConfigurationType { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Aliases => _aliases;

    /// <summary>Accepts a deprecated flat field name for one of this type's configuration properties.</summary>
    /// <param name="field">The old request field (for example <c>baseUrl</c>).</param>
    /// <param name="property">The configuration property it sets (for example <c>Root</c>).</param>
    public void Alias(string field, string property) => _aliases[field] = property;

    /// <summary>
    /// Says which entries of a string dictionary property are secret. Without this, every entry of every
    /// string dictionary is masked, because an entry nobody declared is not safe to show.
    /// </summary>
    /// <param name="dictionaryProperty">The configuration property holding the dictionary.</param>
    /// <param name="mask">Returns the entries with the secret ones replaced by <see cref="ConnectionSecretMask.Value"/>.</param>
    public void MaskEntries(
        string dictionaryProperty,
        Func<IConnectionImplementationConfiguration, IReadOnlyDictionary<string, string?>, IDictionary<string, string?>> mask)
        => _entryMasks[dictionaryProperty] = mask;

    /// <summary>Adds a rule a bound or merged configuration must pass before it is saved.</summary>
    /// <param name="rule">Returns a failure, with its own logged message, to refuse the configuration.</param>
    public void Validate(Func<IConnectionImplementationConfiguration, ILogger, IGenericResult> rule) => _rules.Add(rule);

    /// <inheritdoc />
    public IGenericResult<IConnectionImplementationConfiguration> Bind(JsonElement configuration, ILogger logger)
    {
        var body = ReadObject(configuration, logger);
        if (body.IsFailure)
            return body.ToNewResult<IConnectionImplementationConfiguration>();

        // Why nulls are dropped on create: a member left out and a member sent as null both mean "not
        // supplied", so the configuration's own default applies -- the flat request read null the same way.
        var members = body.Value!;
        foreach (var key in members.Where(member => member.Value is null).Select(member => member.Key).ToList())
            members.Remove(key);

        return Read(members, logger);
    }

    /// <inheritdoc />
    public IGenericResult<IConnectionImplementationConfiguration> Merge(
        JsonElement patch, IConnectionImplementationConfiguration existing, ILogger logger)
    {
        var changes = ReadObject(patch, logger);
        if (changes.IsFailure)
            return changes.ToNewResult<IConnectionImplementationConfiguration>();

        var stored = Write(existing, logger);
        if (stored.IsFailure)
            return stored.ToNewResult<IConnectionImplementationConfiguration>();

        MergeInto(stored.Value!, changes.Value!);
        return Read(stored.Value!, logger);
    }

    /// <inheritdoc />
    public IGenericResult<JsonNode> Project(IConnectionImplementationConfiguration configuration, ILogger logger)
    {
        var shown = Write(configuration, logger);
        if (shown.IsFailure)
            return shown.ToNewResult<JsonNode>();

        var settings = shown.Value!;
        foreach (var shared in settings.Select(member => member.Key).Where(SharedMembers.Contains).ToList())
            settings.Remove(shared);

        MaskSecrets(configuration, settings);
        return GenericResult<JsonNode>.Success(settings);
    }

    private bool IsBindable =>
        !ConfigurationType.IsAbstract
        && !ConfigurationType.IsInterface
        && typeof(IConnectionImplementationConfiguration).IsAssignableFrom(ConfigurationType);

    private IGenericResult<JsonObject> ReadObject(JsonElement json, ILogger logger)
    {
        if (!IsBindable)
            return GenericResult<JsonObject>.Failure(
                ConnectionEndpointBehaviorLog.ConfigurationTypeNotBindable(logger, ConfigurationType.Name));

        if (json.ValueKind != JsonValueKind.Object)
            return GenericResult<JsonObject>.Failure(
                ConnectionEndpointBehaviorLog.ConfigurationInvalid(logger, ConfigurationType.Name, "it must be a JSON object"));

        var body = JsonNode.Parse(json.GetRawText())!.AsObject();
        ApplyAliases(body);
        return GenericResult<JsonObject>.Success(body);
    }

    private void ApplyAliases(JsonObject body)
    {
        foreach (var alias in _aliases)
        {
            var legacy = body.Select(member => member.Key)
                .FirstOrDefault(key => string.Equals(key, alias.Key, StringComparison.OrdinalIgnoreCase));
            if (legacy is null)
                continue;

            var value = body[legacy];
            body.Remove(legacy);

            // Why the configuration's own name wins: a body that sends both is sending the new field on purpose.
            var target = JsonNamingPolicy.CamelCase.ConvertName(alias.Value);
            if (MatchingKey(body, target) is null)
                body[target] = value;
        }
    }

    private IGenericResult<IConnectionImplementationConfiguration> Read(JsonObject body, ILogger logger)
    {
        IConnectionImplementationConfiguration? read;
        try
        {
            read = body.Deserialize(ConfigurationType, JsonOptions) as IConnectionImplementationConfiguration;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            return GenericResult<IConnectionImplementationConfiguration>.Failure(
                ConnectionEndpointBehaviorLog.ConfigurationInvalid(logger, ConfigurationType.Name, ex.Message));
        }

        if (read is null)
            return GenericResult<IConnectionImplementationConfiguration>.Failure(
                ConnectionEndpointBehaviorLog.ConfigurationInvalid(logger, ConfigurationType.Name, "the body produced no configuration"));

        foreach (var rule in _rules)
        {
            var checkedRule = rule(read, logger);
            if (checkedRule.IsFailure)
                return checkedRule.ToNewResult<IConnectionImplementationConfiguration>();
        }

        return GenericResult<IConnectionImplementationConfiguration>.Success(read);
    }

    private static IGenericResult<JsonObject> Write(IConnectionImplementationConfiguration configuration, ILogger logger)
    {
        var name = configuration.GetType().Name;
        try
        {
            return JsonSerializer.SerializeToNode(configuration, configuration.GetType(), JsonOptions) is JsonObject written
                ? GenericResult<JsonObject>.Success(written)
                : GenericResult<JsonObject>.Failure(ConnectionEndpointBehaviorLog.ProjectionFailed(logger, name, "it is not a JSON object"));
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            return GenericResult<JsonObject>.Failure(ConnectionEndpointBehaviorLog.ProjectionFailed(logger, name, ex.Message));
        }
    }

    private static void MergeInto(JsonObject stored, JsonObject changes)
    {
        foreach (var change in changes.ToList())
        {
            if (IsMask(change.Value))
                continue;

            var key = MatchingKey(stored, change.Key);
            if (key is not null && change.Value is JsonObject nested && stored[key] is JsonObject storedNested)
            {
                MergeInto(storedNested, nested);
                continue;
            }

            // Why remove first: a JSON node has one parent, so it leaves the change before joining the stored object.
            changes.Remove(change.Key);
            if (key is null)
                stored[change.Key] = change.Value;
            else
                stored[key] = change.Value;
        }
    }

    private void MaskSecrets(IConnectionImplementationConfiguration configuration, JsonObject shown)
    {
        foreach (var property in configuration.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var key = MatchingKey(shown, JsonNamingPolicy.CamelCase.ConvertName(property.Name));
            if (key is null)
                continue;

            if (property.GetCustomAttribute<ConnectionSecretAttribute>() is not null)
                MaskValue(shown, key);
            else if (shown[key] is JsonObject && property.GetValue(configuration) is IEnumerable<KeyValuePair<string, string?>> entries)
                shown[key] = MaskedEntries(configuration, property.Name, entries);
        }
    }

    private JsonObject MaskedEntries(
        IConnectionImplementationConfiguration configuration, string property, IEnumerable<KeyValuePair<string, string?>> entries)
    {
        var values = entries.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        IDictionary<string, string?> masked = _entryMasks.TryGetValue(property, out var mask)
            ? mask(configuration, values)
            : values.ToDictionary(
                entry => entry.Key,
                entry => string.IsNullOrEmpty(entry.Value) ? entry.Value : ConnectionSecretMask.Value,
                StringComparer.OrdinalIgnoreCase);

        var shown = new JsonObject();
        foreach (var entry in masked)
            shown[entry.Key] = entry.Value;
        return shown;
    }

    private static void MaskValue(JsonObject shown, string key)
    {
        if (shown[key] is JsonValue value && value.TryGetValue<string>(out var text) && !string.IsNullOrEmpty(text))
            shown[key] = ConnectionSecretMask.Value;
    }

    private static bool IsMask(JsonNode? value) =>
        value is JsonValue text
        && text.TryGetValue<string>(out var shown)
        && string.Equals(shown, ConnectionSecretMask.Value, StringComparison.Ordinal);

    private static string? MatchingKey(JsonObject members, string name) =>
        members.Select(member => member.Key)
            .FirstOrDefault(key => string.Equals(key, name, StringComparison.OrdinalIgnoreCase));
}
