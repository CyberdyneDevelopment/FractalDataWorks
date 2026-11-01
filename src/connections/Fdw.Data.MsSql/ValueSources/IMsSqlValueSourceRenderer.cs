using Fdw.Collections;
using Fdw.Commands.Data.ValueSources;

namespace Fdw.Data.MsSql.ValueSources;

/// <summary>Renders a value source as a T-SQL expression. The option's name is the value-source kind it handles.</summary>
public interface IMsSqlValueSourceRenderer : ITypeOption<int, MsSqlValueSourceRendererBase>
{
    /// <summary>
    /// Renders <paramref name="source"/> as a T-SQL expression, adding any parameters to the context's command.
    /// </summary>
    /// <param name="source">The value source.</param>
    /// <param name="context">The command, dialect and source alias.</param>
    /// <returns>The expression, or <see langword="null"/> when it cannot be rendered.</returns>
    string? Render(ColumnValueSource source, MsSqlValueSourceContext context);
}
