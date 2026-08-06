using Fdw.Collections;
using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpMethods;

/// <summary>
/// Source generator creates static HttpMethods class automatically.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(HttpMethodBase), typeof(IHttpMethod), typeof(HttpMethodCollection))]
public abstract partial class HttpMethodCollection : TypeCollectionBase<HttpMethodBase, IHttpMethod>
{
    // DO NOT IMPLEMENT BY HAND!
    // Source generator automatically creates static HttpMethods class with:
    // - HttpMethods.Get (returns IHttpMethod)
    // - HttpMethods.Post (returns IHttpMethod)
    // - HttpMethods.Put (returns IHttpMethod)
    // - HttpMethods.Delete (returns IHttpMethod)
    // - HttpMethods.Patch (returns IHttpMethod)
    // - HttpMethods.All (collection of IHttpMethod)
    // - HttpMethods.ById(int id) (returns IHttpMethod)
    // - HttpMethods.ByName(string name) (returns IHttpMethod)
}