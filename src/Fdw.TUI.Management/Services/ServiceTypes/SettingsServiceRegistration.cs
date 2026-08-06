using Fdw.Collections.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.TUI.Management.Services;

/// <summary>
/// Service registration for the settings service.
/// </summary>
[TypeOption(typeof(ManagementServiceRegistrations), "SettingsService", RestrictToCurrentCompilation = true)]
public sealed class SettingsServiceRegistration : ManagementServiceRegistrationBase
{
    /// <summary>
    /// Creates the settings service registration.
    /// </summary>
    public SettingsServiceRegistration() : base(
        id: 2,
        name: "SettingsService",
        serviceDescription: "Manages application settings and preferences",
        lifetime: ServiceLifetime.Singleton)
    {
    }

    /// <inheritdoc />
    public override void Register(IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, SettingsService>();
    }
}
