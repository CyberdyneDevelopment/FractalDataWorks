using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Mcp.Bus;

/// <summary>Canvas sink places the result side-by-side with the current fovea.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ViewIntents), "Compare")]
public sealed class Compare : ViewIntentBase
{
    /// <summary>Initializes the Compare view intent.</summary>
    public Compare() : base(3, "Compare") { }
}
