using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Http.Operations;

/// <summary>
/// The HTTP methods an endpoint container maps to a data operation, found by the method in upper case (<c>ByMethod</c>). A method that
/// matches none (<c>HEAD</c>, <c>OPTIONS</c>) gets <c>NotFound</c> and stands for no operation.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(EndpointMethodOperationBase), typeof(IEndpointMethodOperation), typeof(EndpointMethodOperations))]
public abstract partial class EndpointMethodOperations : TypeCollectionBase<EndpointMethodOperationBase, IEndpointMethodOperation>
{
}
