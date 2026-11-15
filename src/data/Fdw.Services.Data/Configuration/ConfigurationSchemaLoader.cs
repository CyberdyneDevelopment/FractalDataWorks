using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Configuration;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Fdw.Results;
using Fdw.Services.Data.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// Registration helpers for <see cref="IConfigurationGateway"/>. These extension methods are the
/// explicit exception to the no-extension-methods rule: the host must supply the concrete
/// <see cref="IConnectionFactory"/> implementation (and optionally an <see cref="ISecretManager"/>)
/// at registration time, which the ServiceTypeCollection 3-phase cannot do cleanly.
/// </summary>
/// <summary>
/// Loads <c>configurationSchema.json</c>, resolving environment placeholders.
/// </summary>
/// <remarks>
/// Public because a host that builds its own gateway - a test fixture, or an app that does not run
/// registration - still has to read the same schema registration would have read.
/// </remarks>
public static partial class ConfigurationSchemaLoader
{
    private static readonly JsonSerializerOptions _schemaJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new ConnectionConfigurationJsonConverter(),
        },
    };

    /// <summary>Reads the schema at <paramref name="jsonFilePath"/>.</summary>
    /// <param name="jsonFilePath">Path to the schema file; relative paths resolve against the app's base directory.</param>
    /// <returns>The declared connections, secret managers and data stores.</returns>
    /// <exception cref="InvalidOperationException">The file cannot be read or bound, or a placeholder has no value; for the last, the message is the coded failure's.</exception>
    public static ConfigurationSchema Load(string jsonFilePath)
    {
        var loaded = Load(jsonFilePath, NullLogger.Instance);
        return loaded.IsSuccess && loaded.Value is { } schema
            ? schema
            : throw new InvalidOperationException(string.Join(" | ", loaded.Messages.Select(message => message.Message)));
    }

    /// <summary>Reads the schema at <paramref name="jsonFilePath"/>, reporting a placeholder that has no value as a coded failure.</summary>
    /// <param name="jsonFilePath">Path to the schema file; relative paths resolve against the app's base directory.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The declared connections, secret managers and data stores, or why a placeholder cannot be resolved.</returns>
    /// <remarks>
    /// A <c>${NAME}</c> takes its value from an environment variable or, on a host that runs on an A/B slot
    /// (<see cref="SlotFile.EnvironmentVariableName"/> set), from the slot file's value of that name (<see cref="SlotFile"/>).
    /// A name stated by both is refused, and so is one stated by neither; nothing stands in for a value.
    /// </remarks>
    public static IGenericResult<ConfigurationSchema> Load(string jsonFilePath, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath))
            throw new InvalidOperationException("configurationSchema.json path must not be null or whitespace.");

        if (!Path.IsPathRooted(jsonFilePath))
            jsonFilePath = Path.Combine(AppContext.BaseDirectory, jsonFilePath);

        byte[] jsonBytes;
        try
        {
            jsonBytes = File.ReadAllBytes(jsonFilePath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to read configurationSchema.json from '{jsonFilePath}': {ex.Message}", ex);
        }

        var resolved = ResolvePlaceholders(Encoding.UTF8.GetString(jsonBytes), jsonFilePath, logger);
        if (resolved.IsFailure || resolved.Value is not { } resolvedJson)
            return resolved.ToNewResult<ConfigurationSchema>();

        ConfigurationSchemaRoot? root;
        try
        {
            root = JsonSerializer.Deserialize<ConfigurationSchemaRoot>(resolvedJson, _schemaJsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize configurationSchema.json from '{jsonFilePath}': {ex.Message}", ex);
        }

        if (root?.ConfigurationSchema is null)
            throw new InvalidOperationException(
                $"configurationSchema.json at '{jsonFilePath}' is missing the 'ConfigurationSchema' root object.");

        return GenericResult<ConfigurationSchema>.Success(root.ConfigurationSchema);
    }

    [GeneratedRegex(
        @"\$\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\}",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex EnvironmentPlaceholder();

    private static IGenericResult<string> ResolvePlaceholders(string json, string schemaPath, ILogger logger)
    {
        if (SlotFile.DeclaredPath() is not { } declared)
            return Substitute(json, schemaPath, null, logger);

        var read = SlotFile.Read(declared, logger);
        return read.IsFailure || read.Value is not { } file
            ? read.ToNewResult<string>()
            : Substitute(json, schemaPath, file, logger);
    }

    private static IGenericResult<string> Substitute(string json, string schemaPath, SlotFile? slot, ILogger logger)
    {
        var both = new List<string>();
        var missing = new List<string>();

        var resolved = EnvironmentPlaceholder().Replace(json, match => ValueOf(match, slot, both, missing, logger));

        if (both.Count > 0 && slot is not null)
            return GenericResult<string>.Failure(SlotFileLog.PlaceholderStatedByBothSources(logger, schemaPath, slot.FilePath, Names(both)));

        if (missing.Count > 0)
            return GenericResult<string>.Failure(slot is null
                ? SlotFileLog.PlaceholderNotStated(logger, schemaPath, Names(missing))
                : SlotFileLog.PlaceholderNotStatedOnSlot(logger, schemaPath, slot.FilePath, Names(missing)));

        return GenericResult<string>.Success(resolved);
    }

    /// <summary>The text one <c>${NAME}</c> becomes; a name with no value or two values stays as it is and is recorded for the failure.</summary>
    private static string ValueOf(Match match, SlotFile? slot, List<string> both, List<string> missing, ILogger logger)
    {
        var name = match.Groups["name"].Value;
        var fromEnvironment = Environment.GetEnvironmentVariable(name);
        var fromSlot = slot is not null && slot.Values.TryGetValue(name, out var stated) ? stated : null;

        if (string.IsNullOrEmpty(fromEnvironment))
        {
            if (fromSlot is null)
            {
                missing.Add(name);
                return match.Value;
            }

            SlotFileLog.PlaceholderResolved(logger, name, "the slot file");
            return JsonEncodedText.Encode(fromSlot).ToString();
        }

        if (fromSlot is not null)
        {
            both.Add(name);
            return match.Value;
        }

        SlotFileLog.PlaceholderResolved(logger, name, "the environment");
        return JsonEncodedText.Encode(fromEnvironment).ToString();
    }

    private static string Names(List<string> names) => string.Join(", ", names.Distinct(StringComparer.Ordinal));
}
