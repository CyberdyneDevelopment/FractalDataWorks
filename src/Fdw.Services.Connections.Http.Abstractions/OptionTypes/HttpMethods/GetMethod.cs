using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpMethods;

/// <summary>
/// HTTP GET method - retrieves data from the server.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpMethodCollection), "Get", RestrictToCurrentCompilation = true)]
public sealed class GetMethod : HttpMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetMethod"/> class.
    /// </summary>
    public GetMethod() : base(1, "GET", "Retrieves data from the server")
    {
    }
}