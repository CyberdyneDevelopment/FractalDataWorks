using System;
using System.IO;
using System.Text.Json.Nodes;
using Fdw.ServiceTypes;
using Fdw.Services.Connections.FileSystem;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.FileSystem.Tests.Integration;

/// <summary>
/// Builds ONE shared <see cref="ServiceProvider"/> for every test in
/// <see cref="FileSystemConnectionGatewayTests"/>. <see cref="SecretManagerTypes.Register"/> tracks
/// per-option registration in process-wide static state (<c>_registeredOptionNames</c>) — building
/// a fresh <see cref="ServiceCollection"/> per test would make every call after the first a silent
/// no-op against that new (empty) collection, leaving <see cref="SecretManagerConfigurationProvider"/>
/// unregistered. One shared, read-only-after-construction provider is the correct usage here, exactly
/// as a real host builds its container once.
/// </summary>
/// <remarks>
/// The store is rooted at an ISOLATED temp copy of the committed <c>config-data</c> seed (with a schema
/// clone whose <c>Root</c> repoints there), so the read tests still see the seed AND the end-to-end
/// write test (<see cref="SecretManagerConfigurationProvider.Save"/>) can persist a new record without
/// mutating the committed fixtures. The write test uses a DISTINCT record name so it can never interfere
/// with the seeded <c>EnvSecrets</c> the read tests assert on.
/// </remarks>
public sealed class FileSystemConnectionGatewayFixture : IDisposable
{
    public IServiceProvider Provider { get; }

    // Why the host is held: it owns the container now, so disposing it is what disposes the provider.
    private readonly IHost _host;

    public string Root { get; }

    public FileSystemConnectionGatewayFixture()
    {
        Root = Directory.CreateTempSubdirectory("fdw-fs-gateway-").FullName;
        CopySeed(Path.Combine(AppContext.BaseDirectory, "config-data"), Root);
        var schemaPath = WriteSchemaPointingAt(Root);

        var builder = Host.CreateApplicationBuilder();

        var services = builder.Services;
        services.AddLogging();
        services.AddConfigurationGateway<FileSystemConnectionFactory>(schemaPath);
        // Why: DefaultConfigurationProvider<TConfig,TCommand> consumes Lazy<IConfigurationGateway> so
        // the gateway resolves on first cfg query — mirrors ConfigurationGatewayServiceType.Register.
        services.AddSingleton(sp => new Lazy<IConfigurationGateway>(() => sp.GetRequiredService<IConfigurationGateway>()));

        // Why: registered explicitly because FDW no longer ships a SecretManager [ServiceTypeOption].
        // The concrete types (EnvironmentVariable, AzureKeyVault, MsSql, Sqlite, UserSecrets) moved to
        // reference-servicetypes, and each of them calls this as the first line of its
        // Register. With no option left in this repo, SecretManagerTypes.Register below
        // iterates an EMPTY collection and silently registers nothing — which is how the header provider
        // came to be missing and these tests started failing with "No service for type
        // SecretManagerConfigurationProvider". FDW's own tests cannot reference reference-servicetypes to
        // get it back: that package depends on FDW, so the dependency only runs one way.
        // TryAddSingleton inside makes this idempotent if an option is ever present again.
        SecretManagerConfigurationProvider.RegisterDomainConfiguration(services);

        // Why: the typed BODY provider, also lost with the option. The header provider composes an
        // aggregate by dispatching on the ServiceOptionType discriminator to a typed provider; with none
        // registered the header resolves but Configuration comes back null, which is the second failure
        // these tests hit after the header itself was restored.
        services.AddSingleton(sp => new DefaultConfigurationProvider<EnvironmentVariableConfiguration, EnvironmentVariableConfigurationCommand>(
            sp.GetRequiredService<ILoggerFactory>()
                .CreateLogger<DefaultConfigurationProvider<EnvironmentVariableConfiguration, EnvironmentVariableConfigurationCommand>>(),
            sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
            "ConfigurationDb",
            "sec",
            new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

        // Why: kept so that if a SecretManager option is reintroduced to this repo its factory and
        // typed-body provider still get wired; today it is a no-op over an empty collection.
        SecretManagerTypes.Register(builder, null);

        // Why the host and not services.BuildServiceProvider(): phase 3 takes the host now, and building
        // the container off the builder keeps this fixture on the same construction path a real host
        // uses — which is exactly what the resolution note below depends on.
        _host = builder.Build();
        Provider = _host.Services;
        SecretManagerTypes.Initialize(_host, null);

        // Why: SecretManagerTypes' generated provider is registered AddScoped, so
        // RegisterFactory (which calls EnvironmentVariableSecretManagerType.RegisterFactory ->
        // SecretManagerConfigurationProvider.Register) only runs the FIRST TIME
        // IFdwServiceProvider<ISecretManager, SecretManagerConfiguration> is actually resolved — it is
        // NOT run by Initialize() for the scoped path. A real host resolves this provider per-request
        // via an ISecretManager consumer; this test forces that same resolution once so the typed-body
        // provider is wired before any Get(...) call composes a SecretManagerConfiguration aggregate.
        Provider.GetRequiredService<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>();

        // Why: the discriminator -> typed-provider link that EnvironmentVariableSecretManagerType.
        // RegisterFactory used to make. Nothing in this repo makes it any more, so the fixture does it
        // directly; the name must match the ServiceOptionType stored in the seed data.
        Provider.GetRequiredService<SecretManagerConfigurationProvider>()
            .Register(
                "EnvironmentVariable",
                Provider.GetRequiredService<DefaultConfigurationProvider<EnvironmentVariableConfiguration, EnvironmentVariableConfigurationCommand>>());
    }

    // Why: clone the committed seed into the isolated temp root so writes never touch the bin-copied fixtures.
    private static void CopySeed(string source, string destination)
    {
        foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(source, destination, StringComparison.Ordinal));
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            File.Copy(file, file.Replace(source, destination, StringComparison.Ordinal), overwrite: true);
    }

    // Why: the committed schema pins Root to the relative "config-data" seed folder; clone it and point
    // Root at the isolated temp copy so the gateway reads/writes the throwaway store.
    private static string WriteSchemaPointingAt(string root)
    {
        var source = Path.Combine(AppContext.BaseDirectory, "configurationSchema.json");
        var node = JsonNode.Parse(File.ReadAllText(source))!;
        node["ConfigurationSchema"]!["Connections"]!.AsArray()[0]!["Configuration"]!["Root"] = root;
        var target = Path.Combine(root, "configurationSchema.json");
        File.WriteAllText(target, node.ToJsonString());
        return target;
    }

    public void Dispose()
    {
        _host.Dispose();
        Directory.Delete(Root, recursive: true);
    }
}
