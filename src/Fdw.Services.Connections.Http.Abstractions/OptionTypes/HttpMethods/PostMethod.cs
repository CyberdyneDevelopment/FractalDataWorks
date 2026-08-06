using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpMethods;

/// <summary>
/// HTTP POST method - submits data to be processed by the server.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpMethodCollection), "Post", RestrictToCurrentCompilation = true)]
public sealed class PostMethod : HttpMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostMethod"/> class.
    /// </summary>
    public PostMethod() : base(2, "POST", "Submits data to be processed by the server")
    {
    }
}