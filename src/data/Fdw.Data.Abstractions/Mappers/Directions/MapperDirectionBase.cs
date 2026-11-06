using Fdw.Collections;

namespace Fdw.Data;

/// <summary>
/// Base class for the mapping directions a generated POCO mapper emits (CRTP pattern).
/// </summary>
public abstract class MapperDirectionBase : TypeOptionBase<MapperDirectionBase>, IMapperDirection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapperDirectionBase"/> class.
    /// </summary>
    /// <param name="name">The direction's name.</param>
    /// <param name="summary">A one-sentence description.</param>
    /// <param name="emitsInput">Whether the mapper emits the read side.</param>
    /// <param name="emitsOutput">Whether the mapper emits the write side.</param>
    protected MapperDirectionBase(string name, string summary, bool emitsInput, bool emitsOutput)
        : base(name)
    {
        Summary = summary;
        EmitsInput = emitsInput;
        EmitsOutput = emitsOutput;
    }

    /// <inheritdoc />
    public string Summary { get; }

    /// <inheritdoc />
    public bool EmitsInput { get; }

    /// <inheritdoc />
    public bool EmitsOutput { get; }
}
