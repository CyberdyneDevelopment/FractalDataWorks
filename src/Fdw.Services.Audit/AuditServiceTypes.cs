using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Audit;

/// <summary>
/// ServiceTypeCollection for audit service types.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(AuditServiceTypeBase),
    typeof(IAuditServiceType),
    typeof(AuditServiceTypes),
    ServiceCategory = "Audit",
    RestrictToCurrentCompilation = true)]
public partial class AuditServiceTypes : ServiceTypeCollectionBase<AuditServiceTypeBase, IAuditServiceType>
{
}
