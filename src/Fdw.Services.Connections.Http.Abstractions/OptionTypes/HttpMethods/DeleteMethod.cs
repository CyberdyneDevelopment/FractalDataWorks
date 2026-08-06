using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpMethods;

/// <summary>
/// HTTP DELETE method - deletes a resource from the server.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpMethodCollection), "Delete", RestrictToCurrentCompilation = true)]
public sealed class DeleteMethod : HttpMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteMethod"/> class.
    /// </summary>
    public DeleteMethod() : base(4, "DELETE", "Deletes a resource from the server")
    {
    }
}