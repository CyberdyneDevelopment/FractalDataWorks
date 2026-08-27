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
public static partial class ConfigurationGatewayExtensions
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

    /// <summary>
    /// Reads <paramref name="jsonFilePath"/>, deserializes it directly via
    /// <see cref="System.Text.Json.JsonSerializer"/> (bypassing <c>IConfiguration</c> binding
    /// so that <c>[JsonPolymorphic]</c>-style dispatch works), and registers the resulting
    /// <see cref="ConfigurationSchema"/> as a singleton along with
    /// <see cref="IConfigurationGateway"/> without a secret manager.
    /// Use when the ConfigurationDb connection uses integrated auth or does not require secret
    /// resolution.
    /// </summary>
    /// <typeparam name="TConnectionFactory">
    /// Concrete connection factory type. Must implement <see cref="IConnectionFactory"/>.
    /// </typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionName">The configuration connection the gateway serves.</param>
    /// <param name="jsonFilePath">
    /// Absolute or relative path to the <c>configurationSchema.json</c> file.
    /// Relative paths are resolved from the current working directory (the application content root
    /// at startup).
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the file cannot be read or deserialization fails.
    /// </exception>
    public static IServiceCollection AddConfigurationGateway<TConnectionFactory>(
        this IServiceCollection services,
        string connectionName,
        string jsonFilePath)
        where TConnectionFactory : class, IConnectionFactory
    {
        var schema = LoadSchema(jsonFilePath);
        services.TryAddSingleton(schema);
        services.TryAddSingleton<IConnectionFactory, TConnectionFactory>();
        // Why: Explicit factory so DataGatewayResultCache and IOptions<DataGatewayOptions> are
        // resolved via GetService<T>() (returns null if not registered) rather than as required deps.
        // This allows ConfigurationGateway to degrade gracefully to cacheless operation when the
        // cache singleton is not yet registered (e.g., test hosts or apps that call
        // AddConfigurationGateway before DefaultDataGatewayServiceType.Register).
        services.TryAddSingleton<IConfigurationGateway>(sp =>
            new ConfigurationGateway(
                connectionName,
                sp.GetRequiredService<IConnectionFactory>(),
                sp.GetRequiredService<ConfigurationSchema>(),
                sp.GetService<ILogger<ConfigurationGateway>>(),
                sp.GetService<DataGatewayResultCache>(),
                sp.GetService<IOptions<DataGatewayOptions>>(),
                sp.GetService<IAuthenticationContextAccessor>()));
        return services;
    }

    /// <summary>
    /// Reads <paramref name="jsonFilePath"/>, deserializes it directly via
    /// <see cref="System.Text.Json.JsonSerializer"/>, and registers the resulting
    /// <see cref="ConfigurationSchema"/> as a singleton along with
    /// <see cref="IConfigurationGateway"/> with a secret manager for secret resolution
    /// at construction time.
    /// </summary>
    /// <typeparam name="TConnectionFactory">
    /// Concrete connection factory type. Must implement <see cref="IConnectionFactory"/>.
    /// </typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionName">The configuration connection the gateway serves.</param>
    /// <param name="jsonFilePath">
    /// Absolute or relative path to the <c>configurationSchema.json</c> file.
    /// </param>
    /// <param name="createSecretManager">
    /// Constructs the secret manager, given the service provider and the logical name the schema
    /// declares for it. The caller names the constructor, so the compiler checks the call.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="createSecretManager"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the file cannot be read, deserialization fails, or the schema does not declare
    /// exactly one secret manager.
    /// </exception>
    public static IServiceCollection AddConfigurationGateway<TConnectionFactory>(
        this IServiceCollection services,
        string connectionName,
        string jsonFilePath,
        Func<IServiceProvider, string, ISecretManager> createSecretManager)
        where TConnectionFactory : class, IConnectionFactory
    {
        if (createSecretManager is null)
            throw new ArgumentNullException(nameof(createSecretManager));

        var schema = LoadSchema(jsonFilePath);
        services.TryAddSingleton(schema);
        services.TryAddSingleton<IConnectionFactory, TConnectionFactory>();

        // Why: register both the SecretManagerConfiguration parent AND its typed Configuration
        // (e.g. EnvironmentVariableConfiguration) under their runtime types so the concrete
        // secret manager constructor (which takes the typed config) can resolve from DI.
        for (var i = 0; i < schema.SecretManagers.Count; i++)
        {
            var sm = schema.SecretManagers[i];
            services.TryAdd(ServiceDescriptor.Singleton(sm.GetType(), sm));
            if (sm.Configuration is not null)
                services.TryAdd(ServiceDescriptor.Singleton(sm.Configuration.GetType(), sm.Configuration));
        }

        // Why: same pattern for connections — typed ConnectionConfiguration (e.g.
        // MsSqlConnectionConfiguration) is what factories/providers consume via DI.
        for (var i = 0; i < schema.Connections.Count; i++)
        {
            var cn = schema.Connections[i];
            services.TryAdd(ServiceDescriptor.Singleton(cn.GetType(), cn));
            if (cn.Configuration is not null)
                services.TryAdd(ServiceDescriptor.Singleton(cn.Configuration.GetType(), cn.Configuration));
        }

        // Why: the connection factory refuses to read a secret out of a store the connection did not
        // name, so the manager handed to it must be able to state WHICH store it is, and that name lives
        // on the SecretManagerConfiguration header in the schema. NO FALLBACK on the name: a schema that
        // does not declare exactly one secret manager cannot say which one this single ISecretManager
        // registration is, so it fails at composition.
        if (schema.SecretManagers.Count != 1)
        {
            throw new InvalidOperationException(
                $"AddConfigurationGateway<{typeof(TConnectionFactory).Name}> registers ONE ISecretManager, "
                + $"but the schema declares {schema.SecretManagers.Count} SecretManagers. Declare exactly one, "
                + "or use the overload without a secret manager if the ConfigurationDb connection needs none.");
        }

        // Why: the caller constructs its own secret manager and the compiler checks the call. Selecting a
        // constructor reflectively (ActivatorUtilities) bound the name POSITIONALLY to whichever string
        // parameter could not be resolved from the container, which is unstateable in the type system —
        // it had to be described in an exception message and then verified after the fact by comparing
        // the constructed manager's Name. Needing to check that the object came out right is proof the
        // construction did not guarantee it. A delegate makes the constructor a compile-time choice, so
        // there is nothing to verify and nothing to mis-bind.
        var secretManagerName = schema.SecretManagers[0].Name;
        services.TryAddSingleton<ISecretManager>(sp => createSecretManager(sp, secretManagerName));
        // Why: Same explicit factory as the no-secret-manager overload — resolves cache and options
        // via GetService<T>() so the gateway degrades gracefully when they are not registered.
        services.TryAddSingleton<IConfigurationGateway>(sp =>
            new ConfigurationGateway(
                connectionName,
                sp.GetRequiredService<IConnectionFactory>(),
                sp.GetRequiredService<ISecretManager>(),
                sp.GetRequiredService<ConfigurationSchema>(),
                sp.GetService<ILogger<ConfigurationGateway>>(),
                sp.GetService<DataGatewayResultCache>(),
                sp.GetService<IOptions<DataGatewayOptions>>(),
                sp.GetService<IAuthenticationContextAccessor>()));
        return services;
    }

    // Why: LoadSchema is called at service registration time (before Build()), not lazily.
    // Failing fast here ensures a clear error at startup rather than a cryptic NullReference
    // on the first Execute call. The schema is static (shipped with the app) so there is no
    // value in deferring the load. The InvalidOperationException propagates up to Program.cs
    // which is the appropriate failure boundary for a misconfigured app.
    private static ConfigurationSchema LoadSchema(string jsonFilePath)
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
