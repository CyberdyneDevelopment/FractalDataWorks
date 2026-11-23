using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
/// Loads <c>configurationSchema.json</c>, resolving environment placeholders: the connections, the secret managers
/// those connections name for their secrets, the data stores and the roles (FDW-6360).
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
            new SecretManagerConfigurationJsonConverter(),
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
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            return GenericResult<ConfigurationSchema>.Failure(ConfigurationGatewayProviderLog.SchemaFileUnreadable(logger, jsonFilePath, ex.Message));
        }

        var resolved = ResolvePlaceholders(Encoding.UTF8.GetString(jsonBytes), jsonFilePath, logger);
        if (resolved.IsFailure || resolved.Value is not { } resolvedJson)
            return resolved.ToNewResult<ConfigurationSchema>();

        ConfigurationSchemaRoot? root;
        try
        {
            root = JsonSerializer.Deserialize<ConfigurationSchemaRoot>(resolvedJson, _schemaJsonOptions);
        }
        catch (SchemaRefusalException coded)
        {
            // A converter's coded refusal is the failure; one made without a way to say it is the file that did not bind, with its text.
            return GenericResult<ConfigurationSchema>.Failure(
                coded.Refusal is { } refusal
                    ? refusal(logger)
                    : ConfigurationGatewayProviderLog.SchemaNotBound(logger, jsonFilePath, coded.Message));
        }
        catch (JsonException ex)
        {
            // Any other binder error is the one message that says the file did not bind, with the binder's own text.
            return GenericResult<ConfigurationSchema>.Failure(ConfigurationGatewayProviderLog.SchemaNotBound(logger, jsonFilePath, ex.Message));
        }

        if (root?.ConfigurationSchema is null)
            return GenericResult<ConfigurationSchema>.Failure(ConfigurationGatewayProviderLog.SchemaRootMissing(logger, jsonFilePath));

        // The binder drops a key no class binds without a word; a second pass over the same document says each one (FDW-6584).
        using var document = JsonDocument.Parse(resolvedJson);
        var unbound = ConfigurationSchemaUnboundKeys.Judge(document.RootElement, jsonFilePath, _schemaJsonOptions, logger);

        return unbound.Count > 0
            ? GenericResult<ConfigurationSchema>.Failure(unbound)
            : GenericResult<ConfigurationSchema>.Success(root.ConfigurationSchema);
    }

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

        var resolved = SlotPlaceholders.Substitute(json, slot, static value => JsonEncodedText.Encode(value).ToString(), logger, both, missing);

        if (both.Count > 0 && slot is not null)
            return GenericResult<string>.Failure(SlotFileLog.PlaceholderStatedByBothSources(logger, schemaPath, slot.FilePath, SlotPlaceholders.Names(both)));

        if (missing.Count > 0)
            return GenericResult<string>.Failure(slot is null
                ? SlotFileLog.PlaceholderNotStated(logger, schemaPath, SlotPlaceholders.Names(missing))
                : SlotFileLog.PlaceholderNotStatedOnSlot(logger, schemaPath, slot.FilePath, SlotPlaceholders.Names(missing)));

        return GenericResult<string>.Success(resolved);
    }
}
