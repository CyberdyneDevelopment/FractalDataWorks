using Fdw.Collections;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpMethods;

/// <summary>
/// Base class for HTTP method types in the TypeOption pattern.
/// </summary>
public abstract class HttpMethodBase : TypeOptionBase<int, IHttpMethod>, IHttpMethod
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpMethodBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the HTTP method.</param>
    /// <param name="name">The name of the HTTP method.</param>
    /// <param name="description">The description of the HTTP method.</param>
    protected HttpMethodBase(int id, string name, string description)
        : base(id, name)
    {
    }

}