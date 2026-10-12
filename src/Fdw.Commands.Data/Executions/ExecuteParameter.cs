using Fdw.Commands.Data.ValueSources;

namespace Fdw.Commands.Data.Executions;

/// <summary>
/// One parameter of an execute: the name of the executable container's field it supplies and the value source that
/// provides the value.
/// </summary>
public sealed class ExecuteParameter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteParameter"/> class.
    /// </summary>
    /// <param name="name">The name of the executable container's field that receives the value.</param>
    /// <param name="source">Where the value comes from: a constant or the current timestamp.</param>
    public ExecuteParameter(string name, ColumnValueSource source)
    {
        Name = name;
        Source = source;
    }

    /// <summary>Gets the name of the executable container's field that receives the value.</summary>
    public string Name { get; }

    /// <summary>Gets where the value comes from.</summary>
    public ColumnValueSource Source { get; }
}
