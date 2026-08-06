using Fdw.ServiceTypes;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// Marker interface for multitenancy service types (the options of <see cref="MultitenancyTypes"/>).
/// </summary>
public interface IMultitenancyType : IServiceType
{
    /// <summary>
    /// Gets whether this option resolves a real tenant per request (enables tenant/org resolution
    /// middleware and the tenant switch/list endpoints). <c>false</c> for the single-tenant option;
    /// <c>true</c> for options backed by a real tenant store (e.g. Sql).
    /// </summary>
    /// <remarks>
    /// The host reads its configured <c>Multitenancy</c> row's <c>ServiceOptionType</c>, looks the
    /// option up via <see cref="MultitenancyTypes.ByName(string)"/>, and uses this property to derive
    /// the boolean <c>UseFrameworkApplicationPipeline</c> expects — never by sniffing whether a
    /// configuration section is present (NO FALLBACKS: an unrecognized ServiceOptionType is a
    /// startup failure, not a silent single-tenant default).
    /// </remarks>
    bool EnablesTenantResolution { get; }
}
