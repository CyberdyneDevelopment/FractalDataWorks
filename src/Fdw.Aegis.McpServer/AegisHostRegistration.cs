using System;
using System.IO;
using System.Text.Json;
using Fdw.Aegis;
using Fdw.Aegis.Abstractions;
using Fdw.Aegis.Configuration;
using Fdw.Aegis.Targets;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Aegis.Logging;
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
    /// <c>IPlatformServiceProvider&lt;ISecretManager, SecretManagerConfiguration&gt;</c> the
    /// <see cref="AegisInjector"/> resolves secret managers through.
    /// </summary>
    public static IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
    {
        var logger = Logger(loggerFactory);
        AegisLog.HostPhaseStarting(logger, nameof(Configure));

        var result = SecretManagerTypes.Configure(builder, loggerFactory);
        if (result.IsFailure)
        {
            AegisLog.HostPhaseFailed(logger, nameof(Configure), result.CurrentMessage);
            return result;
        }

        AegisLog.HostPhaseCompleted(logger, nameof(Configure));
        return result;
    }

    private static ILogger Logger(ILoggerFactory? loggerFactory) =>
        loggerFactory?.CreateLogger(typeof(AegisHostRegistration))
        ?? NullLogger.Instance;

    /// <summary>
    /// Phase 1b (before Build): registers <see cref="SecretManagerTypes"/>' required services, the
    /// declared schema as <see cref="IOptions{TOptions}"/>, one named <see cref="System.Net.Http.HttpClient"/>
    /// per declared HTTP connection, and the Aegis injector pipeline itself.
    /// </summary>
    public static IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ConfigurationSchema schema, ILoggerFactory? loggerFactory = null)
    {

        var logger = Logger(loggerFactory);
        AegisLog.HostPhaseStarting(logger, nameof(Register));

        var secretManagersRegistered = SecretManagerTypes.Register(builder, loggerFactory);
        if (secretManagersRegistered.IsFailure)
        {
            AegisLog.HostPhaseFailed(logger, nameof(Register), secretManagersRegistered.CurrentMessage);
            return secretManagersRegistered;
        }

        builder.Services.TryAddSingleton(sp => new Lazy<IConfigurationGateway>(() => sp.GetRequiredService<IConfigurationGateway>()));

        foreach (var connection in schema.Connections)
        {
            if (connection.Configuration is HttpConnectionConfigurationBase http)
            {
                var baseUrl = http.BaseUrl;
                builder.Services.AddHttpClient(connection.Name, client => client.BaseAddress = new Uri(baseUrl));
            }
        }

        builder.Services.AddSingleton(Options.Create(new AegisCommandsOptions { Commands = [.. schema.Commands] }));

        builder.Services.AddSingleton<ISecretManagerConfigurationProvider>(
            new DeclaredSecretManagerConfigurationProvider([.. schema.SecretManagers]));

        builder.Services.AddScoped<IApprovalPolicyEvaluator, PreApprovedPolicyEvaluator>();
        builder.Services.AddScoped<IAegisInjectionTarget, HttpHeaderInjectionTarget>();
        builder.Services.AddScoped<AegisInjector>();

        AegisLog.HostPhaseCompleted(logger, nameof(Register));
        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>
    /// Phase 2 (after Build): wires <see cref="SecretManagerTypes"/> factories into its provider, so
    /// <c>ISecretManagerProvider.Get(name)</c> resolves a logical name with zero ConfigurationDb access.
    /// </summary>
    /// <remarks>
    /// Why name resolution is not an Aegis-side directory: turning a name into a configuration is the
    /// domain provider's job, not the injector's. The
    /// <see cref="DeclaredSecretManagerConfigurationProvider"/> registered in <c>Register</c> swaps only
    /// the SOURCE of that configuration (declared JSON instead of ConfigurationDb) and leaves the
    /// resolution path identical, so <c>AegisInjector</c> holds no directory and names no specific
    /// secret manager.
    /// </remarks>
    public static IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null)
    {
        var logger = Logger(loggerFactory);
        AegisLog.HostPhaseStarting(logger, nameof(Initialize));

        var secretManagersInitialized = SecretManagerTypes.Initialize(host, loggerFactory);
        if (secretManagersInitialized.IsFailure)
        {
            AegisLog.HostPhaseFailed(logger, nameof(Initialize), secretManagersInitialized.CurrentMessage);
            return secretManagersInitialized;
        }

        AegisLog.HostPhaseCompleted(logger, nameof(Initialize));
        return GenericResult<IHost>.Success(host);
    }
}
