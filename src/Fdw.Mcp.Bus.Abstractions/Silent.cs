using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Mcp.Bus;

/// <summary>Canvas sink skips this event; stdio sink still delivers the RPC response.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ViewIntents), "Silent")]
public sealed class Silent : ViewIntentBase
{
    /// <summary>Initializes the Silent view intent.</summary>
    public Silent() : base(1, "Silent") { }
}
