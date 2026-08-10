using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Web.RestEndpoints.ApiServiceTypeOptions;

/// <summary>
/// The API domains a host serves.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(ApiServiceTypeBase),
    typeof(IApiServiceType),
    typeof(ApiServiceTypes),
    ServiceCategory = "ApiService")]
public partial class ApiServiceTypes : ServiceTypeCollectionBase<ApiServiceTypeBase, IApiServiceType>
{
}
