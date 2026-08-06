using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Mcp.Bus;

/// <summary>Canvas sink adds the result as a depth-2 ghost without advancing the tour.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ViewIntents), "Ghost")]
public sealed class Ghost : ViewIntentBase
{
    /// <summary>Initializes the Ghost view intent.</summary>
    public Ghost() : base(4, "Ghost") { }
}
