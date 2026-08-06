using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpMethods;

/// <summary>
/// HTTP PUT method - uploads or replaces a resource on the server.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpMethodCollection), "Put", RestrictToCurrentCompilation = true)]
public sealed class PutMethod : HttpMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PutMethod"/> class.
    /// </summary>
    public PutMethod() : base(3, "PUT", "Uploads or replaces a resource on the server")
    {
    }
}