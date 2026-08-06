using Fdw.Collections;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpMethods;

/// <summary>
/// Interface defining the contract for HTTP method enum options.
/// </summary>
public interface IHttpMethod : ITypeOption<int, IHttpMethod>
{
    /// <summary>
    /// Gets the description of this HTTP method.
    /// </summary>
    string Description { get; }
}