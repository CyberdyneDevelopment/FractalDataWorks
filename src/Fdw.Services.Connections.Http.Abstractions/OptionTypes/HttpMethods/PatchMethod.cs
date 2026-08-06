using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpMethods;

/// <summary>
/// HTTP PATCH method - applies partial modifications to a resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpMethodCollection), "Patch", RestrictToCurrentCompilation = true)]
public sealed class PatchMethod : HttpMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PatchMethod"/> class.
    /// </summary>
    public PatchMethod() : base(5, "PATCH", "Applies partial modifications to a resource")
    {
    }
}