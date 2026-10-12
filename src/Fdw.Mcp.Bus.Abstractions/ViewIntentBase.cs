using Fdw.Collections;
using Fdw.Mcp.Bus.Abstractions;

namespace Fdw.Mcp.Bus;

/// <summary>Base class for <see cref="IViewIntent"/> TypeOptions.</summary>
public abstract class ViewIntentBase : TypeOptionBase<IViewIntent>, IViewIntent
{
    /// <summary>Required protected parameterless constructor for the TypeCollection Empty sentinel.</summary>
    protected ViewIntentBase() : base(0, "NotFound") { }

    /// <summary>Initializes a new view-intent option.</summary>
    protected ViewIntentBase(string name) : base(name) { }
}
