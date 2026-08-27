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
internal static partial class ConfigurationSchemaLoader
{
    // Why: Centralized JsonSerializerOptions for configurationSchema.json deserialization.
    // PropertyNameCaseInsensitive so JSON written with either casing round-trips cleanly.
    // The three custom converters dispatch ConnectionConfiguration, SecretManagerConfiguration, and
    // AegisCommandConfiguration to their concrete subtypes by reading the ServiceOptionType
    // discriminator field and looking up the resolved CLR type from the TypeCollection (populated by
    // module initializers).
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

    // Why: LoadSchema is called at service registration time (before Build()), not lazily.
    // Failing fast here ensures a clear error at startup rather than a cryptic NullReference
    // on the first Execute call. The schema is static (shipped with the app) so there is no
    // value in deferring the load. The InvalidOperationException propagates up to Program.cs
    // which is the appropriate failure boundary for a misconfigured app.
    internal static ConfigurationSchema Load(string jsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath))
            throw new InvalidOperationException("configurationSchema.json path must not be null or whitespace.");

        // Why: Resolve relative paths against AppContext.BaseDirectory (the app binary output dir
        // where CopyToOutputDirectory=PreserveNewest places the JSON file). Absolute paths are
        // used unchanged. This ensures the file is found at both development and deployment time.
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

    // Why: configurationSchema.json ships in source control and is published publicly, so
    // deployment-specific values — server addresses above all — cannot be committed literally.
    // A ${VAR} placeholder names the environment variable that supplies the value at startup,
    // which keeps the file identical across every environment and keeps infrastructure detail
    // out of the repository.
    //
    // An unset variable is a hard failure, never a blank. Substituting empty would produce a
    // schema that deserializes cleanly and then fails much later as an opaque connection error
    // pointing at the wrong layer; this reports the variable name at the point of absence.
    // Every missing name is collected before throwing so a misconfigured deployment is fixed in
    // one pass rather than one restart per variable.
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

            // Why: the placeholder sits inside a JSON string literal, so the substituted value is
            // escaped for that context — otherwise a value containing a quote or backslash would
            // produce invalid JSON, or let an environment variable inject schema structure.
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
