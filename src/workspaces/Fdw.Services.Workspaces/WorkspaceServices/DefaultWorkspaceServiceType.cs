using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Registers the workspace configuration providers, the workspace permissions and the maker of a default workspace (FDW-4556).
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(WorkspaceServiceTypes), "Default")]
public sealed class DefaultWorkspaceServiceType : WorkspaceServiceTypeBase
{
    // The workspace endpoints' limits (FDW-7104), by the key a host states them under (Workspaces:Default): the lineage map's most nodes, deepest walk and
    // deepest path, its search page by default and at most, and the longest note (its column is varchar(MAX), so this is a product choice, made here as a
    // default). A host changes one with ServiceTypeDefaults.Override before Register.
    private static readonly IReadOnlyDictionary<string, object> Defaults = new Dictionary<string, object>(StringComparer.Ordinal)
    {
        [nameof(WorkspaceLimits.LineageMaxNodes)] = 500,
        [nameof(WorkspaceLimits.LineageMaxDepth)] = 5,
        [nameof(WorkspaceLimits.LineageMaxPathDepth)] = 8,
        [nameof(WorkspaceLimits.LineageSearchDefaultTake)] = 20,
        [nameof(WorkspaceLimits.LineageSearchMaxTake)] = 100,
        [nameof(WorkspaceLimits.NoteMaxBodyLength)] = 4000,
    };

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object> DeclaredDefaults => Defaults;

    /// <summary>Initializes a new instance of the <see cref="DefaultWorkspaceServiceType"/> class.</summary>
    public DefaultWorkspaceServiceType()
        : base(
            "Default",
            "Workspaces:Default",
            "Default Workspace Services",
            "The workspace providers, the workspace permissions and the maker of a tenant's or organization's default workspace")
    {
        // The workspace provider writes each workspace's description with the descriptions writer, in the unit of the write (FDW-6208): a host that registered this
        // service type without the Descriptions service type is refused at Initialize with a coded message, never given a provider that fails on its first save.
        Requires<IDescriptionWriter>(WorkspaceRegistrationLog.DescriptionWriterNotRegistered);

        Registration((builder, loggerFactory) =>
        {
            var logger = loggerFactory?.CreateLogger<DefaultWorkspaceServiceType>() ?? NullLogger<DefaultWorkspaceServiceType>.Instance;
            var limits = LimitsOf(builder, logger);
            if (!limits.IsSuccess || limits.Value is null)
                return limits.ToNewResult<IHostApplicationBuilder>();

            builder.Services.TryAddSingleton(limits.Value);

            builder.Services.TryAddSingleton<IWorkspaceImplementationConfigurationProvider>(sp => new WorkspaceImplementationConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<WorkspaceImplementationConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                WorkspaceServiceTypes.ConfigurationConnection, "workspace", "WorkspaceConfiguration"));
            WorkspaceRegistrationLog.Registered(logger, nameof(IWorkspaceImplementationConfigurationProvider));

            builder.Services.TryAddSingleton<WorkspaceConfigurationProvider>(sp =>
            {
                var domain = new WorkspaceConfigurationProvider(
                    sp.GetRequiredService<ILogger<WorkspaceConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    WorkspaceServiceTypes.ConfigurationConnection, "workspace", "Workspace",
                    sp.GetRequiredService<IDescriptionWriter>(),
                    sp.GetRequiredService<IDescriptionReader>());
                domain.Register("Workspace", sp.GetRequiredService<IWorkspaceImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IWorkspaceConfigurationProvider>(sp => sp.GetRequiredService<WorkspaceConfigurationProvider>());
            WorkspaceRegistrationLog.Registered(logger, nameof(IWorkspaceConfigurationProvider));

            // Scoped: the providers are singletons, the work is per request (FDW-937).
            builder.Services.TryAddScoped<IWorkspacePermissions>(sp =>
                new WorkspacePermissions(
                    sp.GetRequiredService<IPermissionConfigurationProvider>(),
                    sp.GetRequiredService<IRolePermissionConfigurationProvider>(),
                    sp.GetService<ILogger<WorkspacePermissions>>()));
            WorkspaceRegistrationLog.Registered(logger, nameof(IWorkspacePermissions));

            // Scoped, like the permissions: it reads the ambient authentication context. Nothing registered it before (FDW-6346), so a service that took it, such as the data
            // configuration rule service, would have failed the host build; the workspace endpoints take it too.
            builder.Services.TryAddScoped<IWorkspaceAccessPolicy, WorkspaceAccessPolicy>();
            WorkspaceRegistrationLog.Registered(logger, nameof(IWorkspaceAccessPolicy));

            // Scoped, like the access policy: it records the caller as the one who made an owner, and raises a read of owners from the caller's context
            // (FDW-6967). The data package asks it who owns a path or a container of a shared store.
            builder.Services.TryAddScoped<IWorkspaceResourceOwners>(sp =>
                new WorkspaceResourceOwners(
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    sp.GetRequiredService<IWorkspaceConfigurationProvider>(),
                    sp.GetRequiredService<IAuthenticationContextAccessor>(),
                    WorkspaceServiceTypes.ConfigurationConnection,
                    "workspace",
                    sp.GetService<ILogger<WorkspaceResourceOwners>>()));
            WorkspaceRegistrationLog.Registered(logger, nameof(IWorkspaceResourceOwners));

            // Removes a member, a declared relationship or a resource by retiring its row (FDW-7024, FDW-7093): saving the workspace without it kept it current.
            builder.Services.TryAddScoped<IWorkspaceChildRetirement>(sp =>
                new WorkspaceChildRetirement(
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    WorkspaceServiceTypes.ConfigurationConnection,
                    "workspace",
                    sp.GetService<ILogger<WorkspaceChildRetirement>>()));
            WorkspaceRegistrationLog.Registered(logger, nameof(IWorkspaceChildRetirement));

            // AddScoped, not TryAdd, as the founding below: the data package registers an answer that knows only data views (FDW-5446), and the last registration wins, so this one
            // replaces it whichever of the two registers first. It adds a Scoped variation's owner: a caller who may change the workspace that owns it.
            builder.Services.AddScoped<IVariationChainAccess>(sp =>
                new VariationChainAccess(
                    sp.GetRequiredService<IWorkspaceConfigurationProvider>(),
                    sp.GetRequiredService<IWorkspaceAccessPolicy>(),
                    sp.GetRequiredService<IAuthenticationContextAccessor>()));
            WorkspaceRegistrationLog.Registered(logger, nameof(IVariationChainAccess));

            // AddScoped, not TryAdd: Fdw.Services.Users registers a refusing IDefaultWorkspaceFounding for a host without this package (FDW-4178), and the last
            // registration wins, so this one replaces it whichever of the two registers first. Scoped like the permissions it uses; it reads the ambient
            // authentication context, because a new tenant's workspace is written as that tenant.
            builder.Services.AddScoped<IDefaultWorkspaceFounding>(sp =>
                new DefaultWorkspaceFounding(
                    sp.GetRequiredService<IWorkspaceConfigurationProvider>(),
                    sp.GetRequiredService<IWorkspacePermissions>(),
                    sp.GetRequiredService<IAuthenticationContextAccessor>(),
                    sp.GetService<ILogger<DefaultWorkspaceFounding>>()));
            WorkspaceRegistrationLog.Registered(logger, nameof(IDefaultWorkspaceFounding));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }

    // The limits that apply: each the value the host's configuration states, else the host's override, else this option's declared default.
    private IGenericResult<WorkspaceLimits> LimitsOf(IHostApplicationBuilder builder, ILogger logger)
    {
        var nodes = DeclaredSetting.ConfiguredCount(builder, ConfigurationKey, this, nameof(WorkspaceLimits.LineageMaxNodes), logger);
        var depth = DeclaredSetting.ConfiguredCount(builder, ConfigurationKey, this, nameof(WorkspaceLimits.LineageMaxDepth), logger);
        var pathDepth = DeclaredSetting.ConfiguredCount(builder, ConfigurationKey, this, nameof(WorkspaceLimits.LineageMaxPathDepth), logger);
        var searchDefault = DeclaredSetting.ConfiguredCount(builder, ConfigurationKey, this, nameof(WorkspaceLimits.LineageSearchDefaultTake), logger);
        var searchMax = DeclaredSetting.ConfiguredCount(builder, ConfigurationKey, this, nameof(WorkspaceLimits.LineageSearchMaxTake), logger);
        var note = DeclaredSetting.ConfiguredCount(builder, ConfigurationKey, this, nameof(WorkspaceLimits.NoteMaxBodyLength), logger);

        // Why one check per result: each failure is passed up as it is, and FDW016 reads the check on the result it guards.
        if (!nodes.IsSuccess)
            return nodes.ToNewResult<WorkspaceLimits>();

        if (!depth.IsSuccess)
            return depth.ToNewResult<WorkspaceLimits>();

        if (!pathDepth.IsSuccess)
            return pathDepth.ToNewResult<WorkspaceLimits>();

        if (!searchDefault.IsSuccess)
            return searchDefault.ToNewResult<WorkspaceLimits>();

        if (!searchMax.IsSuccess)
            return searchMax.ToNewResult<WorkspaceLimits>();

        if (!note.IsSuccess)
            return note.ToNewResult<WorkspaceLimits>();

        return GenericResult<WorkspaceLimits>.Success(
            new WorkspaceLimits(nodes.Value, depth.Value, pathDepth.Value, searchDefault.Value, searchMax.Value, note.Value));
    }
}
