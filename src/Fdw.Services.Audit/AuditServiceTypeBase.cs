using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Audit;

/// <summary>
/// Base class for audit service type definitions.
/// </summary>
public abstract class AuditServiceTypeBase : ServiceTypeBase<IGenericService, IAuditServiceFactory, IServiceConfiguration>, IAuditServiceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditServiceTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of the audit service type.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="category">The optional category.</param>
    protected AuditServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? "Audit")
    {
    }
}
