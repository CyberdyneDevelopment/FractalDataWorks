using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Mcp.Bus;

/// <summary>Canvas sink re-renders and advances the tour.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ViewIntents), "Update")]
public sealed class Update : ViewIntentBase
{
    /// <summary>Initializes the Update view intent.</summary>
    public Update() : base(2, "Update") { }
}
