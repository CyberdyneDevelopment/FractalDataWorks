using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Settings;

/// <summary>
/// Base class for settings domain service type definitions.
/// </summary>
public abstract class SettingsServiceTypeBase : ServiceTypeBase<IGenericService, ISettingsServiceFactory, IServiceConfiguration>, ISettingsServiceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsServiceTypeBase"/> class.
    /// </summary>
    protected SettingsServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? "Settings")
    {
    }
}
