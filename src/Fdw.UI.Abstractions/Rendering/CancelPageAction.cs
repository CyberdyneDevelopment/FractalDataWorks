using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>User cancelled the operation.</summary>
[TypeOption(typeof(PageActions), "Cancel")]
[ExcludeFromCodeCoverage]
public sealed class CancelPageAction : PageActionBase
{
    /// <summary>Initializes a new instance of <see cref="CancelPageAction"/>.</summary>
    public CancelPageAction() : base(2, "Cancel") { }
}
