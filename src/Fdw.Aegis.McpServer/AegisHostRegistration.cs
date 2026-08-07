using System;
using System.IO;
using System.Text.Json;
using Fdw.Aegis;
using Fdw.Aegis.Abstractions;
using Fdw.Aegis.Configuration;
using Fdw.Aegis.Targets;
using Fdw.Services.Connections.Http;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Configuration;
using Fdw.ServiceTypes;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fdw.Aegis.McpServer;

/// <summary>
/// Shared DI wiring for the Aegis Gateway MCP host. Extracted from <see cref="Program"/> so the
/// non-exposure test suite can compose the identical, real registration graph against an
/// in-memory <see cref="ConfigurationSchema"/> (pointed at a test stub) instead of duplicating it.
/// </summary>
public static class AegisHostRegistration
{
    // Why: the same three discriminator-dispatch converters ConfigurationGatewayExtensions uses —
    // Aegis.McpServer deserializes its own aegisSchema.json directly via STJ (bypassing IConfiguration
    // binding) rather than calling AddConfigurationGateway, which would additionally register
    // IConnectionFactory/IConfigurationGateway against a real ConfigurationDb this host never touches.
    private static readonly JsonSerializerOptions SchemaJsonOptions = new(JsonSerializerDefaults.Web)
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
    /// Reads and deserializes <paramref name="jsonFilePath"/> into a <see cref="ConfigurationSchema"/>.
    /// Fails loud (no fallback schema) when the file is missing or unparseable.
    /// </summary>
    public static ConfigurationSchema LoadSchema(string jsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath))
            throw new InvalidOperationException("aegisSchema.json path must not be null or whitespace.");

        // Why: relative paths resolve against the published app's own output directory (where
        // CopyToOutputDirectory=PreserveNewest places the JSON file), not the process's current
        // working directory.
        var resolvedPath = Path.IsPathRooted(jsonFilePath)
            ? jsonFilePath
            : Path.Combine(AppContext.BaseDirectory, jsonFilePath);

        byte[] jsonBytes;
        try
        {
            jsonBytes = File.ReadAllBytes(resolvedPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to read aegisSchema.json from '{resolvedPath}': {ex.Message}", ex);
        }

        ConfigurationSchemaRoot? root;
        try
        {
            root = JsonSerializer.Deserialize<ConfigurationSchemaRoot>(jsonBytes, SchemaJsonOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize aegisSchema.json from '{resolvedPath}': {ex.Message}", ex);
        }

        if (root?.ConfigurationSchema is null)
            throw new InvalidOperationException(
                $"aegisSchema.json at '{resolvedPath}' is missing the 'ConfigurationSchema' root object.");

        return root.ConfigurationSchema;
    }

    /// <summary>
    /// Phase 1a (before Build): binds the one ServiceTypeCollection this ConfigurationDb-free host
    /// drives — <see cref="SecretManagerTypes"/>, which registers the
    /// <c>IFdwServiceProvider&lt;ISecretManager, SecretManagerConfiguration&gt;</c> the
    /// <see cref="AegisInjector"/> resolves secret managers through.
    /// </summary>
    public static void Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        SecretManagerTypes.Configure(builder, loggerFactory);
    }

    /// <summary>
    /// Phase 1b (before Build): registers <see cref="SecretManagerTypes"/>' required services, the
    /// declared schema as <see cref="IOptions{TOptions}"/>, one named <see cref="System.Net.Http.HttpClient"/>
    /// per declared HTTP connection, and the Aegis injector pipeline itself.
    /// </summary>
    public static void Register(IHostApplicationBuilder builder, ConfigurationSchema schema, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(schema);

        SecretManagerTypes.Register(builder, loggerFactory);

        // Why: every SecretManager-kind [ServiceTypeOption]'s Register (and the shared
        // SecretManagerConfigurationProvider it registers) constructs a DefaultConfigurationProvider
        // that takes a Lazy<IConfigurationGateway> constructor dependency — that dependency exists
        // purely to satisfy the shared FDW registration machinery; this host never resolves it.
        // AegisInjector resolves secret managers via ISecretManagerProvider.Get(name), whose parent
        // configuration provider is the in-memory DeclaredSecretManagerConfigurationProvider wired in
        // Initialize — so name resolution never touches this gateway either. Mirrors the exact
        // registration line ConfigurationGatewayServiceType uses (Fdw.Services.Data); IConfigurationGateway
        // itself is deliberately never registered here — any accidental use fails loud with a normal DI
        // resolution exception instead of silently reaching a real ConfigurationDb.
        builder.Services.TryAddSingleton(sp => new Lazy<IConfigurationGateway>(() => sp.GetRequiredService<IConfigurationGateway>()));

        // Why: HttpHeaderInjectionTarget picks up its downstream endpoint purely by ConnectionName —
        // register one named HttpClient per declared Http-typed-body connection so CreateClient(name)
        // resolves without this host ever building an IGenericConnection.
        foreach (var connection in schema.Connections)
        {
            if (connection.Configuration is HttpConnectionConfigurationBase http)
            {
                var baseUrl = http.BaseUrl;
                builder.Services.AddHttpClient(connection.Name, client => client.BaseAddress = new Uri(baseUrl));
            }
        }

        builder.Services.AddSingleton(Options.Create(new AegisCommandsOptions { Commands = [.. schema.Commands] }));

        // Why: Scoped — IFdwServiceProvider<ISecretManager, SecretManagerConfiguration> (registered by
        // SecretManagerTypes.Register above) is itself Scoped by default, so AegisInjector (which takes
        // it as a constructor dependency) must be Scoped too. PreApprovedPolicyEvaluator and
        // HttpHeaderInjectionTarget have no scoped dependencies of their own but are registered Scoped
        // for consistency with the one-scope-per-tool-call model ModelContextProtocol's WithTools<T>
        // uses (a fresh target is activated per call from the per-request scope).
        builder.Services.AddScoped<IApprovalPolicyEvaluator, PreApprovedPolicyEvaluator>();
        builder.Services.AddScoped<IAegisInjectionTarget, HttpHeaderInjectionTarget>();
        builder.Services.AddScoped<AegisInjector>();
    }

    /// <summary>
    /// Phase 2 (after Build): wires <see cref="SecretManagerTypes"/> factories into its provider, then
    /// gives that provider the declared schema as its PARENT configuration provider so
    /// <c>ISecretManagerProvider.Get(name)</c> resolves a logical name with zero ConfigurationDb access.
    /// </summary>
    /// <remarks>
    /// Why the parent provider rather than an Aegis-side directory: name-to-configuration resolution is
    /// the domain provider's job, not the injector's. Registering
    /// <see cref="DeclaredSecretManagerConfigurationProvider"/> here swaps only the SOURCE of that
    /// configuration (declared JSON instead of ConfigurationDb) and leaves the resolution path
    /// identical, so <c>AegisInjector</c> holds no directory and names no specific secret manager.
    /// </remarks>
    public static void Initialize(IHost host, ConfigurationSchema schema, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(schema);

        var services = host.Services;

        SecretManagerTypes.Initialize(host, loggerFactory);

        var parentResult = services
            .GetRequiredService<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>()
            .Register(new DeclaredSecretManagerConfigurationProvider([.. schema.SecretManagers]));

        if (!parentResult.IsSuccess)
            throw new InvalidOperationException(
                "Failed to register the declared secret-manager configuration provider: "
                + parentResult.CurrentMessage);
    }
}
