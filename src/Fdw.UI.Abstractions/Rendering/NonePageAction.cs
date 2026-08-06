using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>No action taken.</summary>
[TypeOption(typeof(PageActions), "None")]
[ExcludeFromCodeCoverage]
public sealed class NonePageAction : PageActionBase
{
    /// <summary>Initializes a new instance of <see cref="NonePageAction"/>.</summary>
    public NonePageAction() : base(0, "None") { }
}
