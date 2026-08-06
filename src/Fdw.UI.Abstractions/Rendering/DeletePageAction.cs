using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>User requested deletion.</summary>
[TypeOption(typeof(PageActions), "Delete")]
[ExcludeFromCodeCoverage]
public sealed class DeletePageAction : PageActionBase
{
    /// <summary>Initializes a new instance of <see cref="DeletePageAction"/>.</summary>
    public DeletePageAction() : base(3, "Delete") { }
}
