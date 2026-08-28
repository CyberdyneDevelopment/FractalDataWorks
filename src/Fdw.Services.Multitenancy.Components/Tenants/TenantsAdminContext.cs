using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Services.Multitenancy.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Services.Multitenancy.Components.Tenants;

/// <summary>
/// Immutable context provided by <see cref="TenantsAdminProvider"/> to its render template.
/// Exposes the list of tenants and operations for admin management (create, update, list).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TenantsAdminContext : ProviderContextBase
{
    /// <summary>Gets all tenants (active and inactive).</summary>
    public IReadOnlyList<TenantDetailPayload> Tenants { get; init; } = [];


    /// <summary>Gets whether a save operation is in progress.</summary>
    public bool IsSaving { get; init; }


    /// <summary>Gets the most recent success message, or null.</summary>
    public string? SuccessMessage { get; init; }

    /// <summary>Invoked to reload all tenants from the API.</summary>
    public Func<Task> OnLoad { get; init; } = () => Task.CompletedTask;

    /// <summary>Invoked to create a new tenant.</summary>
    public Func<CreateTenantRequest, Task> OnCreateTenant { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to update an existing tenant.</summary>
    public Func<Guid, UpdateTenantRequest, Task> OnUpdateTenant { get; init; } = (_, _) => Task.CompletedTask;
}
