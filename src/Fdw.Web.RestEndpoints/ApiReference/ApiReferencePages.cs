using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.ApiReference;

/// <summary>
/// Every API reference page a host serves.
/// </summary>
/// <remarks>
/// A page joins by declaring itself a TypeOption of this collection, in the package that holds its
/// document endpoint, so referencing that package is what serves the page and nothing keeps a list.
/// The host maps every member, and the pages list offers the caller every member they may open.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ApiReferencePageBase), typeof(IApiReferencePage), typeof(ApiReferencePages))]
public abstract partial class ApiReferencePages : TypeCollectionBase<ApiReferencePageBase, IApiReferencePage>
{
}
