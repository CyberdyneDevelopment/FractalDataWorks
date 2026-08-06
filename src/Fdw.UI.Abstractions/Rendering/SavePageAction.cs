using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>User saved the configuration.</summary>
[TypeOption(typeof(PageActions), "Save")]
[ExcludeFromCodeCoverage]
public sealed class SavePageAction : PageActionBase
{
    /// <summary>Initializes a new instance of <see cref="SavePageAction"/>.</summary>
    public SavePageAction() : base(1, "Save") { }
}
