using Fdw.Collections;
using Fdw.Commands.Data.ValueSources;

namespace Fdw.Data.MsSql.ValueSources;

/// <summary>
/// Base class of the T-SQL value-source renderers. Each option renders one kind of
/// <see cref="ColumnValueSource"/> and is found by that kind's name.
/// </summary>
public abstract class MsSqlValueSourceRendererBase : TypeOptionBase<MsSqlValueSourceRendererBase>, IMsSqlValueSourceRenderer
{
    /// <summary>
    /// Initializes the NotFound sentinel.
    /// </summary>
    protected MsSqlValueSourceRendererBase()
        : base(0, "NotFound")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlValueSourceRendererBase"/> class.
    /// </summary>
    /// <param name="name">The value-source kind this option renders.</param>
    protected MsSqlValueSourceRendererBase(string name)
        : base(name, $"MsSqlValueSourceRenderers:{name}", $"{name} value source", $"Renders the {name} value source as T-SQL", "MsSqlValueSourceRenderer")
    {
    }

    /// <summary>
    /// Renders <paramref name="source"/> as a T-SQL expression, adding any parameters to the context's command.
    /// </summary>
    /// <param name="source">The value source.</param>
    /// <param name="context">The command, dialect and source alias.</param>
    /// <returns>The expression, or <see langword="null"/> when the source cannot be rendered (unsafe name, wrong kind).</returns>
    public virtual string? Render(ColumnValueSource source, MsSqlValueSourceContext context) => null;
}
