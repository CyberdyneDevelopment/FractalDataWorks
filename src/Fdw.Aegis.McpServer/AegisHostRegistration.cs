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
    /// <c>IPlatformServiceProvider&lt;ISecretManager, SecretManagerConfiguration&gt;</c> the
    /// <see cref="AegisInjector"/> resolves secret managers through.
    /// </summary>
    public static IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
    {
        // Why ILogger<AegisHostRegistration>: SourceContext then names the type the line was written
        // in, so these phase lines are attributable to the host registration rather than to whichever
        // collection happens to be running underneath.
        var logger = Logger(loggerFactory);
        AegisLog.HostPhaseStarting(logger, nameof(Configure));

        // Why the result is returned rather than discarded: a phase that fails returns a coded
        // failure and logs it once. Swallowing it here left this host starting up as though its
        // secret managers had registered, so the first secret resolution failed instead - far from
        // the cause, and for a secrets host that is the worst place to discover it.
        var result = SecretManagerTypes.Configure(builder, loggerFactory);
        if (result.IsFailure)
        {
            AegisLog.HostPhaseFailed(logger, nameof(Configure), result.CurrentMessage);
            return result;
        }

        AegisLog.HostPhaseCompleted(logger, nameof(Configure));
        return result;
    }

    // Why CreateLogger(typeof(...)) and not ILogger<AegisHostRegistration>: this class is static, and
    // a static type cannot be a generic type argument (CS0718). The Type overload sets the same
    // category the generic form would, so SourceContext still names this type — which is the whole
    // reason for the typed logger: these phase lines are attributable to the host registration rather
    // than to whichever collection is running underneath.
    //
    // Why a helper rather than repeating the expression three times: every phase needs the same
    // logger, and loggerFactory is optional so each site would otherwise carry the same
    // null-coalesce. NullLogger keeps the phases working when no factory is supplied — the one
    // fallback the codebase allows.
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

        // Why: Scoped — IPlatformServiceProvider<ISecretManager, SecretManagerConfiguration> (registered by
        // SecretManagerTypes.Register above) is itself Scoped by default, so AegisInjector (which takes
        // it as a constructor dependency) must be Scoped too. PreApprovedPolicyEvaluator and
        // HttpHeaderInjectionTarget have no scoped dependencies of their own but are registered Scoped
        // for consistency with the one-scope-per-tool-call model ModelContextProtocol's WithTools<T>
        // uses (a fresh target is activated per call from the per-request scope).
        builder.Services.AddScoped<IApprovalPolicyEvaluator, PreApprovedPolicyEvaluator>();
        builder.Services.AddScoped<IAegisInjectionTarget, HttpHeaderInjectionTarget>();
        builder.Services.AddScoped<AegisInjector>();

        AegisLog.HostPhaseCompleted(logger, nameof(Register));
        return GenericResult<IHostApplicationBuilder>.Success(builder);
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
    public static IGenericResult<IHost> Initialize(IHost host, ConfigurationSchema schema, ILoggerFactory? loggerFactory = null)
    {
        var services = host.Services;

        var logger = Logger(loggerFactory);
        AegisLog.HostPhaseStarting(logger, nameof(Initialize));

        var secretManagersInitialized = SecretManagerTypes.Initialize(host, loggerFactory);
        if (secretManagersInitialized.IsFailure)
        {
            AegisLog.HostPhaseFailed(logger, nameof(Initialize), secretManagersInitialized.CurrentMessage);
            return secretManagersInitialized;
        }

        var domainResult = services
            .GetRequiredService<IPlatformServiceProvider<ISecretManager, SecretManagerConfiguration>>()
            .Register(new DeclaredSecretManagerConfigurationProvider([.. schema.SecretManagers]));

        // Why this returns rather than throws: an exception decides for the host that the process
        // ends. Registration failures arrive here as values from every other phase, and this one was
        // the odd path out - it aborted startup with a stack trace where its siblings returned a coded
        // failure the caller could log and act on.
        if (domainResult.IsFailure)
        {
            AegisLog.HostPhaseFailed(logger, nameof(Initialize), domainResult.CurrentMessage);
            return domainResult.ToNewResult<IHost>();
        }

        AegisLog.HostPhaseCompleted(logger, nameof(Initialize));
        return GenericResult<IHost>.Success(host);
    }
}
