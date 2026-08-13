using System;
using Fdw.Services.Results;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Connections.FileSystem;

/// <summary>
/// Connection type definition for FileSystem connections.
/// Registers <see cref="IFileSystemConnectionFactory"/> and the
/// <see cref="FileSystemConnectionConfiguration"/> typed-body provider. Connection configuration
/// lives in ConfigurationDb, not appsettings.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(ConnectionTypes), "FileSystem")]
public sealed class FileSystemConnectionType
    : ConnectionTypeBase<IGenericConnection, IFileSystemConnectionFactory, FileSystemConnectionConfiguration>
{

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemConnectionType"/> class.
    /// </summary>
    public FileSystemConnectionType() : base(
        name: "FileSystem",
        sectionName: "FileSystem",
        displayName: "File System",
        description: "Local or network file system connection",
        category: "Storage")
    {
        // Why Initialize and not Register: this wiring needs a LIVE container (it resolves the
        // domain provider and its typed-body providers), and Register runs while the container
        // is still being built. Initialize runs after Build() with a real IServiceProvider.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            // Why: Typed body providers are registered with the header provider (ConnectionConfigurationProvider)
            // via discriminator dispatch. FileSystemConnectionConfiguration no longer inherits
            // ConnectionConfiguration — it implements IConnectionConfiguration directly.
            var headerProvider = services.GetRequiredService<ConnectionConfigurationProvider>();
            var configProvider = services.GetRequiredService<FileSystemConnectionConfigurationProvider>();
            headerProvider.Register(Name, configProvider);

    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        // Why Append and not Registration: Registration REPLACES the phase body, and
        // ConnectionTypeBase's constructor has already prepended this option's factory registration
        // onto it. Replacing therefore silently discards the base's contribution — which is exactly
        // how every connection kind stopped being creatable while each option's own wiring kept
        // working and logging success. Appending composes onto what the base put there.
        AppendRegistration((builder, loggerFactory) =>
        {
            builder.Services.AddSingleton<IFileSystemConnectionFactory, FileSystemConnectionFactory>();
            // Why (FDW-403 slice 2 follow-up): mirror Http/MsSql — register a typed
            // FileSystemConnectionConfigurationProvider so DefaultConnectionProvider can resolve
            // child config rows (conn.FileSystemConnection) by parent ConnectionId.
            builder.Services.TryAddSingleton<FileSystemConnectionConfigurationProvider>(sp =>
                new FileSystemConnectionConfigurationProvider(
                    sp.GetService<ILogger<FileSystemConnectionConfigurationProvider>>() ?? NullLogger<FileSystemConnectionConfigurationProvider>.Instance,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    DataStore,
                    PathName,
                    new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<FileSystemConnectionConfiguration>>(
                sp => sp.GetRequiredService<FileSystemConnectionConfigurationProvider>());
            // Why: RegisterFactory (below) requires ConnectionConfigurationProvider (the shared header
            // provider for the whole Connections domain) to already be registered. TryAddSingleton makes
            // this idempotent — every connection-kind option calls it, harmlessly redundant after the first.
            ConnectionConfigurationProvider.RegisterDomainConfiguration(builder.Services);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

    /// <summary>
    /// FileSystem supports FileRead and FileWrite capabilities.
    /// </summary>
    // Why: ByName() returns the TypeCollection singleton — never instantiate capabilities inline.
    public override IReadOnlyList<ICommandCapabilityType> SupportedCommands =>
    [
        CommandCapabilityTypes.ByName("FileRead"),
        CommandCapabilityTypes.ByName("FileWrite"),
    ];

}
