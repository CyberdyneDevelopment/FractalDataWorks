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
using Microsoft.Extensions.Options;

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
            new SecretManagerConfigurationJsonConverter(),
            new AegisCommandConfigurationJsonConverter(),
        },
    };

    /// <summary>Reads the schema at <paramref name="jsonFilePath"/>.</summary>
    /// <param name="jsonFilePath">Path to the schema file; relative paths resolve against the app's base directory.</param>
    /// <returns>The declared connections, secret managers and data stores.</returns>
    public static ConfigurationSchema Load(string jsonFilePath)
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

        ConfigurationSchemaRoot? root;
        try
        {
            root = JsonSerializer.Deserialize<ConfigurationSchemaRoot>(
                ResolveEnvironmentPlaceholders(Encoding.UTF8.GetString(jsonBytes), jsonFilePath),
                _schemaJsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize configurationSchema.json from '{jsonFilePath}': {ex.Message}", ex);
        }

        if (root?.ConfigurationSchema is null)
            throw new InvalidOperationException(
                $"configurationSchema.json at '{jsonFilePath}' is missing the 'ConfigurationSchema' root object.");

        return root.ConfigurationSchema;
    }

    [GeneratedRegex(
        @"\$\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\}",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex EnvironmentPlaceholder();

    private static string ResolveEnvironmentPlaceholders(string json, string jsonFilePath)
    {
        var missing = new List<string>();

        var resolved = EnvironmentPlaceholder().Replace(json, match =>
        {
            var value = Environment.GetEnvironmentVariable(match.Groups["name"].Value);
            if (string.IsNullOrEmpty(value))
            {
                missing.Add(match.Groups["name"].Value);
                return match.Value;
            }

            return JsonEncodedText.Encode(value).ToString();
        });

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"configurationSchema.json at '{jsonFilePath}' references environment variable(s) that are not set: " +
                $"{string.Join(", ", missing.Distinct(StringComparer.Ordinal))}. " +
                "Set them in the host environment before starting the application.");

        return resolved;
    }
}
