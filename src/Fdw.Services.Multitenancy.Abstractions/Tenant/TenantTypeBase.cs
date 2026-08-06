using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Base class for tenant type options.
/// Tenants can be defined statically or loaded from configuration at runtime.
/// </summary>
public abstract class TenantTypeBase : TypeOptionBase<Guid, TenantTypeBase>, ITenant
{
    /// <summary>
    /// Initializes a new instance from configuration.
    /// </summary>
    protected TenantTypeBase(TenantConfiguration configuration)
        : base(configuration.Id, configuration.Name)
    {
        Slug = configuration.Slug;
        OrgPrefix = configuration.OrgPrefix;
        IsActive = configuration.IsActive;
        IsGlobal = configuration.IsGlobal;
        DisplayName = configuration.Name;
        ConnectionName = configuration.ConnectionName;
        Theme = configuration.Theme.ToTheme();
        Options = configuration.Options.ToOptions();
        AvailableRoles = configuration.AvailableRoles;
        ConfigurationSection = $"Tenants:{configuration.Slug}";
    }

    /// <summary>
    /// Initializes a new instance with explicit values.
    /// </summary>
    protected TenantTypeBase(
        Guid id,
        string name,
        string slug,
        string? orgPrefix = null,
        string? connectionName = null,
        ITenantTheme? theme = null,
        ITenantOptions? options = null,
        IEnumerable<string>? availableRoles = null,
        bool isGlobal = false)
        : base(id, name)
    {
        Slug = slug;
        OrgPrefix = orgPrefix;
        IsActive = true;
        IsGlobal = isGlobal;
        DisplayName = name;
        ConnectionName = connectionName;
        Theme = theme ?? TenantTheme.Default;
        Options = options ?? TenantOptions.Default;
        AvailableRoles = availableRoles ?? new[] { "Admin", "User" };
        ConfigurationSection = $"Tenants:{slug}";
    }

    /// <inheritdoc />
    [TypeLookup("BySlug")]
    public string Slug { get; }

    /// <inheritdoc />
    public string? OrgPrefix { get; }

    /// <inheritdoc />
    public bool IsActive { get; protected set; }

    /// <inheritdoc />
    public bool IsGlobal { get; }

    /// <inheritdoc />
    public new string DisplayName { get; }

    /// <inheritdoc />
    public ITenantTheme Theme { get; }

    /// <inheritdoc />
    public ITenantOptions Options { get; }

    /// <inheritdoc />
    public IEnumerable<string> AvailableRoles { get; }

    /// <inheritdoc />
    public string? ConnectionName { get; }

    /// <inheritdoc />
    public string ConfigurationSection { get; }
}
