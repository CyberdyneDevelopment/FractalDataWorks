using Fdw.Collections;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpMethods;

/// <summary>
/// Base class for HTTP method types in the TypeOption pattern.
/// </summary>
public abstract class HttpMethodBase : TypeOptionBase<IHttpMethod>, IHttpMethod
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpMethodBase"/> class.
    /// </summary>
    /// <param name="name">The name of the HTTP method.</param>
    /// <param name="description">The description of the HTTP method.</param>
    protected HttpMethodBase(string name, string description)
        : base(name)
    {
    }

}