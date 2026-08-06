using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;

namespace Fdw.Services.Resiliency;

/// <summary>
/// ServiceTypeCollection for resiliency service types.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(ResiliencyServiceTypeBase),
    typeof(IResiliencyServiceType),
    typeof(ResiliencyServiceTypes),
    ServiceCategory = "Resiliency")]
public partial class ResiliencyServiceTypes : ServiceTypeCollectionBase<
    ResiliencyServiceTypeBase,
    IResiliencyServiceType>
{
}
